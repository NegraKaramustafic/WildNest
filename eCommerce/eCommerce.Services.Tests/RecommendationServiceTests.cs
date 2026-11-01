using eCommerce.Model.Enums;
using eCommerce.Services.Database;
using eCommerce.Services.Recommendation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class RecommendationServiceTests
    {
        [Fact]
        public async Task PositiveHistory_RanksTheActivitySimilarVisitorsBooked()
        {
            using var db = new SqliteDb();
            var world = await World.CreateAsync(db.Context);
            var cache = new RecommendationModelCache();
            var trainer = Trainer(cache);
            var trained = await trainer.TrainAsync(db.Context);

            Assert.True(trained.ModelReady);
            var state = cache.Capture();
            Assert.Contains(world.Target.Id, state.UserIds);
            Assert.Contains(world.Canopy.Id, state.ActivityIds);
            var result = await Service(db.Context, cache, trainer, world.Target.Id)
                .GetRecommendationsAsync(10);

            Assert.DoesNotContain(result.Items, item => item.ActivityId == world.Alpha.Id);
            Assert.DoesNotContain(result.Items, item => item.ActivityId == world.Bravo.Id);
            Assert.DoesNotContain(result.Items, item => item.ActivityId == world.PendingOnly.Id);
            var canopy = Assert.Single(result.Items, item => item.ActivityId == world.Canopy.Id);
            var popular = Assert.Single(result.Items, item => item.ActivityId == world.Popular.Id);
            Assert.True(
                canopy.Score > popular.Score,
                $"Expected the shared activity to outrank the disjoint popular one. Canopy {canopy.Score}, popular {popular.Score}.");
            Assert.Equal(RecommendationWeights.SimilarReservations, canopy.Reasons.Single().Code);
            Assert.Equal(RecommendationWeights.SimilarReservationsText, canopy.Reasons.Single().Text);
        }

        [Fact]
        public async Task ColdStart_ReturnsMostPopularActivities()
        {
            using var db = new SqliteDb();
            var world = await World.CreateAsync(db.Context);
            var cache = new RecommendationModelCache();
            var trainer = Trainer(cache);
            await trainer.TrainAsync(db.Context);

            var result = await Service(db.Context, cache, trainer, world.NewVisitor.Id)
                .GetRecommendationsAsync(10);

            Assert.Equal(
                new[] { world.Popular.Id, world.Alpha.Id, world.Bravo.Id, world.Canopy.Id, world.PendingOnly.Id },
                result.Items.Select(item => item.ActivityId).ToArray());
            Assert.All(result.Items.Take(4), item =>
            {
                Assert.Equal(RecommendationWeights.Popular, item.Reasons.Single().Code);
                Assert.Equal(RecommendationWeights.PopularText, item.Reasons.Single().Text);
            });
            var fresh = result.Items[^1];
            Assert.Equal(world.PendingOnly.Id, fresh.ActivityId);
            Assert.Equal(RecommendationWeights.NewActivity, fresh.Reasons.Single().Code);
            Assert.Equal(RecommendationWeights.NewActivityText, fresh.Reasons.Single().Text);
            Assert.Equal(12, result.Items[0].Score);
        }

        [Fact]
        public async Task ExcludesActivitiesTheVisitorAlreadyReserved_AndInactiveOrPastOnes()
        {
            using var db = new SqliteDb();
            var world = await World.CreateAsync(db.Context);
            var cache = new RecommendationModelCache();
            var trainer = Trainer(cache);
            await trainer.TrainAsync(db.Context);

            var result = await Service(db.Context, cache, trainer, world.Target.Id)
                .GetRecommendationsAsync(10);

            var ids = result.Items.Select(item => item.ActivityId).ToHashSet();
            Assert.DoesNotContain(world.Alpha.Id, ids);
            Assert.DoesNotContain(world.Bravo.Id, ids);
            Assert.DoesNotContain(world.PendingOnly.Id, ids);
            Assert.DoesNotContain(world.Inactive.Id, ids);
            Assert.DoesNotContain(world.Past.Id, ids);
        }

        [Fact]
        public async Task ModelNotReady_UsesPopularityWithoutTraining()
        {
            using var db = new SqliteDb();
            var world = await World.CreateAsync(db.Context);
            var cache = new RecommendationModelCache();
            var trainer = Trainer(cache);

            var result = await Service(db.Context, cache, trainer, world.Target.Id)
                .GetRecommendationsAsync(10);

            Assert.Null(result.ModelTrainedAt);
            Assert.Equal(0, result.TrainingRowCount);
            Assert.Null(cache.Capture().Model);
            Assert.Null(cache.Capture().LastAttemptUtc);
            Assert.Equal(world.Popular.Id, result.Items[0].ActivityId);
            Assert.Equal(RecommendationWeights.PopularText, result.Items[0].Reasons.Single().Text);
            Assert.DoesNotContain(result.Items, item => item.ActivityId == world.Alpha.Id);
            Assert.DoesNotContain(result.Items, item => item.ActivityId == world.Inactive.Id);
            Assert.DoesNotContain(result.Items, item => item.ActivityId == world.Past.Id);
        }

        [Fact]
        public async Task FullFutureSchedule_IsNotACandidate()
        {
            using var db = new SqliteDb();
            var world = await World.CreateAsync(db.Context);
            var future = new ZooClock("Europe/Sarajevo").Today.AddDays(5);
            var soldOut = Bookable("Sold Out Walk");
            var mixed = Bookable("One Seat Open");
            db.Context.Activities.AddRange(soldOut, mixed);
            await db.Context.SaveChangesAsync();

            var full = OpenSchedule(soldOut, future, capacityLeft: 0);
            var closed = OpenSchedule(mixed, future, capacityLeft: 0);
            var open = OpenSchedule(mixed, future, capacityLeft: 4);
            open.StartTime = new TimeOnly(14, 0);
            open.EndTime = new TimeOnly(15, 0);
            db.Context.Schedules.AddRange(full, closed, open);
            var guests = Enumerable.Range(0, 15).Select(index => Visitor($"fullguest{index}")).ToList();
            var mixedGuest = Visitor("mixedguest");
            db.Context.Users.AddRange(guests);
            db.Context.Users.Add(mixedGuest);
            await db.Context.SaveChangesAsync();

            var code = 0;
            foreach (var guest in guests)
            {
                Book(db.Context, guest, full, ref code);
            }

            Book(db.Context, mixedGuest, open, ref code);
            await db.Context.SaveChangesAsync();

            var result = await Service(db.Context, new RecommendationModelCache(), Trainer(new RecommendationModelCache()), world.NewVisitor.Id)
                .GetRecommendationsAsync(20);

            Assert.DoesNotContain(result.Items, item => item.ActivityId == soldOut.Id);
            Assert.Contains(result.Items, item => item.ActivityId == mixed.Id);
            Assert.Equal(world.Popular.Id, result.Items[0].ActivityId);
        }

        [Fact]
        public async Task NewActivity_FillsAShortList()
        {
            using var db = new SqliteDb();
            var world = await World.CreateAsync(db.Context);
            var older = Bookable("Aaron Fresh");
            older.CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newer = Bookable("Zebra Hatch");
            newer.CreatedAt = DateTime.UtcNow.AddDays(30);
            db.Context.Activities.AddRange(older, newer);
            await db.Context.SaveChangesAsync();

            var future = new ZooClock("Europe/Sarajevo").Today.AddDays(5);
            db.Context.Schedules.AddRange(
                OpenSchedule(older, future, capacityLeft: 10),
                OpenSchedule(newer, future, capacityLeft: 10));
            await db.Context.SaveChangesAsync();

            var result = await Service(db.Context, new RecommendationModelCache(), Trainer(new RecommendationModelCache()), world.NewVisitor.Id)
                .GetRecommendationsAsync(5);

            Assert.Equal(
                new[] { world.Popular.Id, world.Alpha.Id, world.Bravo.Id, world.Canopy.Id, newer.Id },
                result.Items.Select(item => item.ActivityId).ToArray());
            Assert.All(result.Items.Take(4), item =>
            {
                Assert.Equal(RecommendationWeights.Popular, item.Reasons.Single().Code);
                Assert.DoesNotContain(item.Reasons, reason => reason.Code == RecommendationWeights.NewActivity);
            });
            var added = result.Items[4];
            Assert.Equal(RecommendationWeights.NewActivity, added.Reasons.Single().Code);
            Assert.Equal(RecommendationWeights.NewActivityText, added.Reasons.Single().Text);
            Assert.Equal(0, added.Score);
            Assert.DoesNotContain(result.Items, item => item.ActivityId == older.Id);
        }

        [Fact]
        public async Task EqualBaseScores_LowerAverageRanksLower()
        {
            using var db = new SqliteDb();
            var world = await World.CreateAsync(db.Context);
            var low = Bookable("Alpha Low");
            var high = Bookable("Zulu High");
            db.Context.Activities.AddRange(low, high);
            await db.Context.SaveChangesAsync();

            var future = new ZooClock("Europe/Sarajevo").Today.AddDays(6);
            var lowSchedule = OpenSchedule(low, future, capacityLeft: 20);
            var highSchedule = OpenSchedule(high, future, capacityLeft: 20);
            db.Context.Schedules.AddRange(lowSchedule, highSchedule);
            var guests = Enumerable.Range(0, 6).Select(index => Visitor($"rater{index}")).ToList();
            db.Context.Users.AddRange(guests);
            await db.Context.SaveChangesAsync();

            var code = 0;
            foreach (var guest in guests)
            {
                Book(db.Context, guest, lowSchedule, ref code);
                Book(db.Context, guest, highSchedule, ref code);
            }

            db.Context.Reviews.AddRange(
                new Review
                {
                    UserId = world.NewVisitor.Id,
                    ActivityId = low.Id,
                    Rating = 2,
                    Comment = "Quiet",
                    CreatedAt = DateTime.UtcNow
                },
                new Review
                {
                    UserId = world.Target.Id,
                    ActivityId = high.Id,
                    Rating = 5,
                    Comment = "Wonderful",
                    CreatedAt = DateTime.UtcNow
                });
            await db.Context.SaveChangesAsync();

            var result = await Service(db.Context, new RecommendationModelCache(), Trainer(new RecommendationModelCache()), world.NewVisitor.Id)
                .GetRecommendationsAsync(20);

            var lowItem = Assert.Single(result.Items, item => item.ActivityId == low.Id);
            var highItem = Assert.Single(result.Items, item => item.ActivityId == high.Id);
            Assert.Equal(6d, highItem.Score, precision: 4);
            Assert.Equal(6d * (2d / 5d), lowItem.Score, precision: 4);
            Assert.True(
                result.Items.FindIndex(item => item.ActivityId == high.Id)
                < result.Items.FindIndex(item => item.ActivityId == low.Id));
            Assert.Equal(5, highItem.AvgRating);
            Assert.Equal(2, lowItem.AvgRating);
            Assert.Equal(1, highItem.ReviewCount);
            Assert.Equal(1, lowItem.ReviewCount);
            Assert.Equal(RecommendationWeights.Popular, highItem.Reasons.Single().Code);
            Assert.Equal(RecommendationWeights.Popular, lowItem.Reasons.Single().Code);
            Assert.Equal(12, result.Items.Single(item => item.ActivityId == world.Popular.Id).Score);
        }

        private static Activity Bookable(string name)
        {
            return new Activity
            {
                Name = name,
                ActivityTypeId = ActivityTypeSeedIds.Feeding,
                Duration = TimeSpan.FromHours(1),
                Price = 12.50m,
                Capacity = 40,
                Status = ActivityStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
        }

        private static Schedule OpenSchedule(Activity activity, DateOnly date, int capacityLeft)
        {
            return new Schedule
            {
                ActivityId = activity.Id,
                Date = date,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(11, 0),
                CapacityLeft = capacityLeft,
                Status = ScheduleStatus.Active
            };
        }

        private static User Visitor(string name)
        {
            return new User
            {
                FirstName = "Ada",
                LastName = name,
                Email = $"{name}@example.com",
                Username = name,
                PasswordHash = "x",
                PasswordSalt = "y",
                SecurityStamp = "stamp",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        private static void Book(ECommerceDbContext db, User user, Schedule schedule, ref int code)
        {
            code++;
            db.Reservations.Add(new Reservation
            {
                UserId = user.Id,
                ScheduleId = schedule.Id,
                GuestCount = 1,
                TotalPrice = 12.50m,
                Status = ReservationStatus.Confirmed,
                Code = $"EX{code:000000}",
                CreatedAt = DateTime.UtcNow
            });
        }

        private static RecommendationTrainer Trainer(RecommendationModelCache cache)
        {
            return new RecommendationTrainer(cache, NullLogger<RecommendationTrainer>.Instance);
        }

        private static RecommendationService Service(
            ECommerceDbContext db,
            RecommendationModelCache cache,
            RecommendationTrainer trainer,
            int userId)
        {
            return new RecommendationService(
                db,
                new TestUserAccessor { UserId = userId },
                cache,
                trainer,
                new ZooClock("Europe/Sarajevo"),
                NullLogger<RecommendationService>.Instance);
        }

        private sealed class World
        {
            public required User Target { get; init; }
            public required User NewVisitor { get; init; }
            public required Activity Alpha { get; init; }
            public required Activity Bravo { get; init; }
            public required Activity Canopy { get; init; }
            public required Activity Popular { get; init; }
            public required Activity PendingOnly { get; init; }
            public required Activity Inactive { get; init; }
            public required Activity Past { get; init; }

            public static async Task<World> CreateAsync(ECommerceDbContext db)
            {
                foreach (var reservation in db.Reservations)
                {
                    reservation.Status = ReservationStatus.Cancelled;
                }

                foreach (var activity in db.Activities)
                {
                    activity.Status = ActivityStatus.Inactive;
                }

                await db.SaveChangesAsync();

                var clock = new ZooClock("Europe/Sarajevo");
                var future = clock.Today.AddDays(5);
                var past = clock.Today.AddDays(-3);
                var similar = new List<User>();
                for (var index = 0; index < 8; index++)
                {
                    similar.Add(UserRow($"similar{index}"));
                }

                var outsiders = new List<User>();
                for (var index = 0; index < 12; index++)
                {
                    outsiders.Add(UserRow($"outsider{index}"));
                }

                var target = UserRow("targetvisitor");
                var newbie = UserRow("newvisitor");
                db.Users.AddRange(similar);
                db.Users.AddRange(outsiders);
                db.Users.Add(target);
                db.Users.Add(newbie);

                var alpha = ActivityRow("Alpha Meadow");
                var bravo = ActivityRow("Bravo River");
                var canopy = ActivityRow("Canopy Tour");
                var popular = ActivityRow("Delta Night");
                var pendingOnly = ActivityRow("Echo Picnic");
                var inactive = ActivityRow("Foxtrot Closed");
                inactive.Status = ActivityStatus.Inactive;
                var pastActivity = ActivityRow("Golf Yesterday");
                db.Activities.AddRange(alpha, bravo, canopy, popular, pendingOnly, inactive, pastActivity);
                await db.SaveChangesAsync();

                ScheduleRow(db, alpha, future);
                ScheduleRow(db, bravo, future);
                ScheduleRow(db, canopy, future);
                ScheduleRow(db, popular, future);
                ScheduleRow(db, pendingOnly, future);
                ScheduleRow(db, inactive, future);
                ScheduleRow(db, pastActivity, past);
                await db.SaveChangesAsync();

                var code = 0;
                foreach (var user in similar)
                {
                    Reserve(db, user, alpha, ref code);
                    Reserve(db, user, bravo, ref code);
                    Reserve(db, user, canopy, ref code);
                }

                Reserve(db, target, alpha, ref code);
                Reserve(db, target, bravo, ref code);
                Reserve(db, target, pendingOnly, ref code, ReservationStatus.Pending);

                foreach (var user in outsiders)
                {
                    Reserve(db, user, popular, ref code);
                }

                for (var extra = 0; extra < 6; extra++)
                {
                    var filler = UserRow($"filler{extra}");
                    db.Users.Add(filler);
                    await db.SaveChangesAsync();
                    Reserve(db, filler, inactive, ref code);
                    Reserve(db, filler, pastActivity, ref code);
                }

                await db.SaveChangesAsync();
                return new World
                {
                    Target = target,
                    NewVisitor = newbie,
                    Alpha = alpha,
                    Bravo = bravo,
                    Canopy = canopy,
                    Popular = popular,
                    PendingOnly = pendingOnly,
                    Inactive = inactive,
                    Past = pastActivity
                };
            }

            private static User UserRow(string name)
            {
                return new User
                {
                    FirstName = "Ada",
                    LastName = name,
                    Email = $"{name}@example.com",
                    Username = name,
                    PasswordHash = "x",
                    PasswordSalt = "y",
                    SecurityStamp = "stamp",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
            }

            private static Activity ActivityRow(string name)
            {
                return new Activity
                {
                    Name = name,
                    ActivityTypeId = ActivityTypeSeedIds.Feeding,
                    Duration = TimeSpan.FromHours(1),
                    Price = 12.50m,
                    Capacity = 40,
                    Status = ActivityStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
            }

            private static void ScheduleRow(ECommerceDbContext db, Activity activity, DateOnly date)
            {
                db.Schedules.Add(new Schedule
                {
                    ActivityId = activity.Id,
                    Date = date,
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(11, 0),
                    CapacityLeft = 40,
                    Status = ScheduleStatus.Active
                });
            }

            private static void Reserve(
                ECommerceDbContext db,
                User user,
                Activity activity,
                ref int code,
                ReservationStatus status = ReservationStatus.Confirmed)
            {
                var schedule = db.Schedules.Local.Single(item => item.ActivityId == activity.Id);
                code++;
                db.Reservations.Add(new Reservation
                {
                    UserId = user.Id,
                    ScheduleId = schedule.Id,
                    GuestCount = 1,
                    TotalPrice = 12.50m,
                    Status = status,
                    Code = $"WN{code:000000}",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    }
}
