using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Services;
using eCommerce.Services.Database;
using eCommerce.Services.Sentiment;
using eCommerce.Services.Validators;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class ReviewVisitTests
    {
        [Fact]
        public async Task Insert_RequiresACompletedVisit_ForTheSameUserAndActivity()
        {
            using var db = new SqliteDb();
            var activityId = await AddActivityAsync(db.Context, "Bird watch");
            var otherActivityId = await AddActivityAsync(db.Context, "Feeding");
            var pendingScheduleId = await AddScheduleAsync(db.Context, activityId, new DateOnly(2026, 10, 1));
            var confirmedScheduleId = await AddScheduleAsync(db.Context, activityId, new DateOnly(2026, 10, 2));
            var completedScheduleId = await AddScheduleAsync(db.Context, activityId, new DateOnly(2026, 10, 3));
            var otherScheduleId = await AddScheduleAsync(db.Context, otherActivityId, new DateOnly(2026, 10, 4));
            await AddReservationAsync(db.Context, pendingScheduleId, userId: 4, ReservationStatus.Pending, "WN-REV-P");
            await AddReservationAsync(db.Context, pendingScheduleId, userId: 5, ReservationStatus.Completed, "WN-REV-O");
            await AddReservationAsync(db.Context, otherScheduleId, userId: 4, ReservationStatus.Completed, "WN-REV-X");
            var service = Reviews(db.Context, userId: 4);

            var missing = await Assert.ThrowsAsync<ClientException>(() => service.InsertAsync(Review(activityId)));
            Assert.Equal("You can review this activity after a completed visit.", missing.Message);

            await AddReservationAsync(db.Context, confirmedScheduleId, userId: 4, ReservationStatus.Confirmed, "WN-REV-C");
            var confirmed = await Assert.ThrowsAsync<ClientException>(() => service.InsertAsync(Review(activityId)));
            Assert.Equal("You can review this activity after a completed visit.", confirmed.Message);

            await AddReservationAsync(db.Context, completedScheduleId, userId: 4, ReservationStatus.Completed, "WN-REV-D");
            var created = await service.InsertAsync(Review(activityId));
            Assert.Equal(activityId, created.ActivityId);
            Assert.Equal(4, created.UserId);

            var duplicate = await Assert.ThrowsAsync<ClientException>(() => service.InsertAsync(Review(activityId, "A second note.")));
            Assert.Equal("You have already reviewed this activity.", duplicate.Message);
            Assert.Equal(1, await db.Context.Reviews.CountAsync(item => item.UserId == 4 && item.ActivityId == activityId));
        }

        private static ReviewService Reviews(ECommerceDbContext context, int userId)
        {
            return new ReviewService(
                context,
                new Mapper(),
                new ReviewInsertValidator(),
                new ReviewUpdateValidator(),
                new TestUserAccessor { UserId = userId, Admin = false },
                new IdleSentiment(),
                new SilentLoggerFactory());
        }

        private static ReviewInsertRequest Review(int activityId, string comment = "The visit was worth it.")
        {
            return new ReviewInsertRequest
            {
                ActivityId = activityId,
                Rating = 5,
                Comment = comment
            };
        }

        private static async Task<int> AddActivityAsync(ECommerceDbContext context, string name)
        {
            var activity = new Activity
            {
                Name = name,
                ActivityTypeId = 1,
                Duration = TimeSpan.FromHours(1),
                Price = 10,
                Capacity = 10,
                Status = ActivityStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            context.Activities.Add(activity);
            await context.SaveChangesAsync();
            return activity.Id;
        }

        private static async Task<int> AddScheduleAsync(ECommerceDbContext context, int activityId, DateOnly date)
        {
            var schedule = new Schedule
            {
                ActivityId = activityId,
                Date = date,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                CapacityLeft = 8,
                Status = ScheduleStatus.Active
            };
            context.Schedules.Add(schedule);
            await context.SaveChangesAsync();
            return schedule.Id;
        }

        private static async Task AddReservationAsync(
            ECommerceDbContext context,
            int scheduleId,
            int userId,
            ReservationStatus status,
            string code)
        {
            context.Reservations.Add(new Reservation
            {
                UserId = userId,
                ScheduleId = scheduleId,
                GuestCount = 1,
                TotalPrice = 10,
                Status = status,
                Code = code,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        private sealed class IdleSentiment : ISentimentService
        {
            public bool IsReady => true;

            public SentimentModelReport? LastReport => null;

            public SentimentClassification Classify(string? text) => SentimentClassification.Unlabeled;

            public Task<bool> EnsureReadyAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        }
    }
}
