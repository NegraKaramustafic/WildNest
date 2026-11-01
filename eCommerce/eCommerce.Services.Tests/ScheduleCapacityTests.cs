using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using eCommerce.Services.Database;
using eCommerce.Services.Validators;
using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class ScheduleCapacityTests
    {
        [Fact]
        public async Task Update_KeepsEightReservedSeats_WhenTheSlotTotalStaysAtTen()
        {
            using var db = new SqliteDb();
            var activityId = await AddActivityAsync(db.Context, capacity: 10);
            var scheduleId = await AddScheduleAsync(db.Context, activityId, new DateOnly(2027, 6, 1), capacityLeft: 2);
            await AddReservationAsync(db.Context, scheduleId, userId: 4, guests: 8, ReservationStatus.Confirmed, "WN-HELD08");
            await AddReservationAsync(db.Context, scheduleId, userId: 5, guests: 5, ReservationStatus.Cancelled, "WN-GONE05");
            var service = Create(db.Context);

            var updated = await service.UpdateAsync(scheduleId, Request(activityId, new DateOnly(2027, 6, 1), capacity: 10));

            Assert.Equal(2, updated.CapacityLeft);
            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Schedules.SingleAsync(item => item.Id == scheduleId);
            Assert.Equal(2, stored.CapacityLeft);

            var tooSmall = await Assert.ThrowsAsync<ClientException>(() =>
                service.UpdateAsync(scheduleId, Request(activityId, new DateOnly(2027, 6, 1), capacity: 7)));
            Assert.Equal("Capacity cannot be less than the seats already reserved (8).", tooSmall.Message);

            var tooLarge = await Assert.ThrowsAsync<ValidationException>(() =>
                service.UpdateAsync(scheduleId, Request(activityId, new DateOnly(2027, 6, 1), capacity: 11)));
            Assert.Contains(tooLarge.Errors, failure => failure.ErrorMessage == "Capacity cannot exceed the activity capacity (10).");

            db.Context.ChangeTracker.Clear();
            Assert.Equal(2, await db.Context.Schedules.Where(item => item.Id == scheduleId).Select(item => item.CapacityLeft).SingleAsync());
        }

        [Fact]
        public async Task Update_RejectsAPastDate_AndALockedDateChange()
        {
            using var db = new SqliteDb();
            var activityId = await AddActivityAsync(db.Context, capacity: 10);
            var openId = await AddScheduleAsync(db.Context, activityId, new DateOnly(2027, 6, 2), capacityLeft: 10);
            var bookedId = await AddScheduleAsync(db.Context, activityId, new DateOnly(2027, 6, 3), capacityLeft: 2);
            await AddReservationAsync(db.Context, bookedId, userId: 4, guests: 8, ReservationStatus.Confirmed, "WN-LOCK08");
            var service = Create(db.Context);

            var past = await Assert.ThrowsAsync<ValidationException>(() =>
                service.UpdateAsync(openId, Request(activityId, new DateOnly(2020, 1, 1), capacity: 10)));
            Assert.Contains(past.Errors, failure => failure.ErrorMessage == "The date cannot be in the past.");

            var locked = await Assert.ThrowsAsync<ClientException>(() =>
                service.UpdateAsync(bookedId, Request(activityId, new DateOnly(2027, 6, 4), capacity: 10)));
            Assert.Equal("This schedule has reservations. The activity, date, and times cannot be changed.", locked.Message);
        }

        [Fact]
        public async Task Insert_UsesTheSubmittedTotal_AndDefaultsToTheActivityCapacity()
        {
            using var db = new SqliteDb();
            var activityId = await AddActivityAsync(db.Context, capacity: 10);
            var service = Create(db.Context);

            var specified = await service.InsertAsync(new ScheduleInsertRequest
            {
                ActivityId = activityId,
                Date = new DateOnly(2027, 7, 1),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                Capacity = 6,
                Status = ScheduleStatus.Active
            });
            Assert.Equal(6, specified.CapacityLeft);

            var omitted = await service.InsertAsync(new ScheduleInsertRequest
            {
                ActivityId = activityId,
                Date = new DateOnly(2027, 7, 2),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                Status = ScheduleStatus.Active
            });
            Assert.Equal(10, omitted.CapacityLeft);
        }

        [Fact]
        public async Task Update_LeavesFreeSeatsUnchanged_WhenCapacityIsOmitted()
        {
            using var db = new SqliteDb();
            var activityId = await AddActivityAsync(db.Context, capacity: 10);
            var scheduleId = await AddScheduleAsync(db.Context, activityId, new DateOnly(2027, 8, 1), capacityLeft: 2);
            await AddReservationAsync(db.Context, scheduleId, userId: 4, guests: 8, ReservationStatus.Pending, "WN-KEEP08");
            var service = Create(db.Context);

            var updated = await service.UpdateAsync(scheduleId, Request(activityId, new DateOnly(2027, 8, 1), capacity: null));

            Assert.Equal(2, updated.CapacityLeft);
        }

        private static ScheduleService Create(ECommerceDbContext context)
        {
            var clock = new ZooClock("Europe/Sarajevo");
            return new ScheduleService(
                context,
                new Mapper(),
                new ScheduleInsertValidator(context, clock),
                new ScheduleUpdateValidator(context, clock),
                new UnusedReservationService(),
                new NotificationEventService(new CapturingPublisher(), context),
                clock,
                new SilentLoggerFactory());
        }

        private static ScheduleUpdateRequest Request(int activityId, DateOnly date, int? capacity)
        {
            return new ScheduleUpdateRequest
            {
                ActivityId = activityId,
                Date = date,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                Capacity = capacity,
                Status = ScheduleStatus.Active
            };
        }

        private static async Task<int> AddActivityAsync(ECommerceDbContext context, int capacity)
        {
            var activity = new Activity
            {
                Name = "Keeper talk",
                ActivityTypeId = 1,
                Duration = TimeSpan.FromHours(1),
                Price = 10,
                Capacity = capacity,
                Status = ActivityStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            context.Activities.Add(activity);
            await context.SaveChangesAsync();
            return activity.Id;
        }

        private static async Task<int> AddScheduleAsync(ECommerceDbContext context, int activityId, DateOnly date, int capacityLeft)
        {
            var schedule = new Schedule
            {
                ActivityId = activityId,
                Date = date,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                CapacityLeft = capacityLeft,
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
            int guests,
            ReservationStatus status,
            string code)
        {
            context.Reservations.Add(new Reservation
            {
                UserId = userId,
                ScheduleId = scheduleId,
                GuestCount = guests,
                TotalPrice = guests * 10,
                Status = status,
                Code = code,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        private sealed class UnusedReservationService : IReservationService
        {
            public Task<ReservationResponse> CancelAsync(int id) => throw new NotImplementedException();
            public Task<ReservationResponse> CancelWithReasonAsync(int id, ReservationCancelRequest request) => throw new NotImplementedException();
            public Task<ReservationResponse> CompleteAsync(int id) => throw new NotImplementedException();
            public Task<ReservationResponse> ConfirmAsync(int id) => throw new NotImplementedException();
            public Task DeleteAsync(int id) => throw new NotImplementedException();
            public Task<PageResult<ReservationResponse>> GetAllAsync(ReservationSearchObject? search = null) => throw new NotImplementedException();
            public Task<List<string>> GetAllowedActionsAsync(int id) => throw new NotImplementedException();
            public Task<ReservationResponse> GetByIdAsync(int id) => throw new NotImplementedException();
            public Task<ReservationResponse> InsertAsync(ReservationInsertRequest request) => throw new NotImplementedException();
            public Task<ReservationResponse> UpdateAsync(int id, ReservationUpdateRequest request) => throw new NotImplementedException();
        }
    }
}
