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
                new[] { world.Popular.Id, world.Alpha.Id, world.Bravo.Id, world.Canopy.Id },
                result.Items.Select(item => item.ActivityId).ToArray());
            Assert.All(result.Items, item =>
            {
                Assert.Equal(RecommendationWeights.Popular, item.Reasons.Single().Code);
                Assert.Equal(RecommendationWeights.PopularText, item.Reasons.Single().Text);
            });
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
