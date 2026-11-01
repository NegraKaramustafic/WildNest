using eCommerce.Model.Enums;
using eCommerce.Services;
using eCommerce.Services.Database;
using eCommerce.Services.ReservationStateMachine;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class ReservationHoldTests
    {
        private static readonly DateTime Now = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Release_CancelsUnpaidHoldsOlderThanThirtyMinutes_AndReturnsTheSeats()
        {
            using var db = new SqliteDb();
            var scheduleId = await AddScheduleAsync(db.Context, capacityLeft: 3);
            var visitorId = await AddUserAsync(db.Context);
            var expired = await AddReservationAsync(db.Context, scheduleId, userId: 4, Now.AddMinutes(-31), guests: 2, payment: null, code: "WN-HOLD31");
            var exact = await AddReservationAsync(db.Context, scheduleId, userId: 5, Now.AddMinutes(-30), guests: 1, payment: PaymentStatus.Failed, code: "WN-HOLD30");
            var pendingPayment = await AddReservationAsync(db.Context, scheduleId, userId: 1, Now.AddMinutes(-45), guests: 1, payment: PaymentStatus.Pending, code: "WN-HOLDPEND");
            await AddReservationAsync(db.Context, scheduleId, userId: 2, Now.AddMinutes(-29), guests: 1, payment: null, code: "WN-HOLD29");
            await AddReservationAsync(db.Context, scheduleId, userId: 3, Now.AddMinutes(-90), guests: 1, payment: PaymentStatus.Completed, code: "WN-HOLDPAID");
            var confirmed = await AddReservationAsync(db.Context, scheduleId, userId: visitorId, Now.AddMinutes(-90), guests: 1, payment: null, code: "WN-HOLDCONF");
            var confirmedRow = await db.Context.Reservations.SingleAsync(item => item.Id == confirmed);
            confirmedRow.Status = ReservationStatus.Confirmed;
            await db.Context.SaveChangesAsync();

            var publisher = new CapturingPublisher();
            var payments = new RecordingPaymentService();
            var service = Create(db.Context, publisher, payments);

            var released = await service.ReleaseExpiredHoldsAsync(Now);

            Assert.Equal(TimeSpan.FromMinutes(30), ReservationHoldService.HoldDuration);
            Assert.Equal(3, released);
            db.Context.ChangeTracker.Clear();

            var expiredRow = await db.Context.Reservations.Include(item => item.Payment).SingleAsync(item => item.Id == expired);
            Assert.Equal(ReservationStatus.Cancelled, expiredRow.Status);
            Assert.Null(expiredRow.CancelledByUserId);
            Assert.Equal("Payment was not completed in time.", expiredRow.CancellationReason);
            Assert.NotNull(expiredRow.CancelledAt);

            var exactRow = await db.Context.Reservations.SingleAsync(item => item.Id == exact);
            Assert.Equal(ReservationStatus.Cancelled, exactRow.Status);
            Assert.Null(exactRow.CancelledByUserId);
            Assert.Equal("Payment was not completed in time.", exactRow.CancellationReason);

            Assert.Equal(ReservationStatus.Cancelled, await db.Context.Reservations.Where(item => item.Id == pendingPayment).Select(item => item.Status).SingleAsync());
            Assert.Equal(ReservationStatus.Pending, await db.Context.Reservations.Where(item => item.Code == "WN-HOLD29").Select(item => item.Status).SingleAsync());
            Assert.Equal(ReservationStatus.Pending, await db.Context.Reservations.Where(item => item.Code == "WN-HOLDPAID").Select(item => item.Status).SingleAsync());
            Assert.Equal(ReservationStatus.Confirmed, await db.Context.Reservations.Where(item => item.Id == confirmed).Select(item => item.Status).SingleAsync());

            var schedule = await db.Context.Schedules.SingleAsync(item => item.Id == scheduleId);
            Assert.Equal(7, schedule.CapacityLeft);

            var cancelledPaymentIds = payments.Cancellations.Select(call => call.PaymentId).ToHashSet();
            Assert.Equal(2, cancelledPaymentIds.Count);
            Assert.All(payments.Cancellations, call => Assert.True(call.RefundIfCompleted));
            Assert.Contains(exactRow.PaymentId!.Value, cancelledPaymentIds);
            Assert.Contains(
                await db.Context.Reservations.Where(item => item.Id == pendingPayment).Select(item => item.PaymentId!.Value).SingleAsync(),
                cancelledPaymentIds);

            Assert.Equal(3, publisher.Messages.Count);
            Assert.All(publisher.Messages, message =>
            {
                Assert.Equal(NotificationType.ReservationCancelled, message.Type);
                Assert.Contains("Payment was not completed in time.", message.Message);
            });
            Assert.Contains(publisher.Messages, message => message.RelatedEntityId == expired && message.UserId == 4);

            var again = await service.ReleaseExpiredHoldsAsync(Now);
            Assert.Equal(0, again);
            Assert.Equal(3, publisher.Messages.Count);
        }

        private static ReservationHoldService Create(
            ECommerceDbContext context,
            CapturingPublisher publisher,
            RecordingPaymentService payments)
        {
            var loggerFactory = new SilentLoggerFactory();
            var mapper = new Mapper();
            var provider = new StateBag();
            var pending = new PendingReservationState(context, mapper, provider, loggerFactory);
            var confirmed = new ConfirmedReservationState(context, mapper, provider, loggerFactory);
            var completed = new CompletedReservationState(context, mapper, provider, loggerFactory);
            var cancelled = new CancelledReservationState(context, mapper, provider, loggerFactory);
            provider.Add(typeof(PendingReservationState), pending);
            provider.Add(typeof(ConfirmedReservationState), confirmed);
            provider.Add(typeof(CompletedReservationState), completed);
            provider.Add(typeof(CancelledReservationState), cancelled);
            var root = new BaseReservationState(context, mapper, provider, loggerFactory);
            return new ReservationHoldService(
                context,
                root,
                payments,
                new NotificationEventService(publisher, context));
        }

        private static async Task<int> AddScheduleAsync(ECommerceDbContext context, int capacityLeft)
        {
            var activity = new Activity
            {
                Name = "Night tour",
                ActivityTypeId = 1,
                Duration = TimeSpan.FromHours(1),
                Price = 12,
                Capacity = 10,
                Status = ActivityStatus.Active,
                CreatedAt = Now
            };
            context.Activities.Add(activity);
            await context.SaveChangesAsync();

            var schedule = new Schedule
            {
                ActivityId = activity.Id,
                Date = new DateOnly(2027, 6, 1),
                StartTime = new TimeOnly(20, 0),
                EndTime = new TimeOnly(21, 0),
                CapacityLeft = capacityLeft,
                Status = ScheduleStatus.Active
            };
            context.Schedules.Add(schedule);
            await context.SaveChangesAsync();
            return schedule.Id;
        }

        private static async Task<int> AddUserAsync(ECommerceDbContext context)
        {
            var user = new User
            {
                FirstName = "Pat",
                LastName = "Visitor",
                Email = "pat-hold@example.com",
                Username = "pat-hold",
                PasswordHash = "x",
                PasswordSalt = "y",
                IsActive = true,
                SecurityStamp = "hold-stamp",
                CreatedAt = Now
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user.Id;
        }

        private static async Task<int> AddReservationAsync(
            ECommerceDbContext context,
            int scheduleId,
            int userId,
            DateTime createdAt,
            int guests,
            PaymentStatus? payment,
            string code)
        {
            int? paymentId = null;
            if (payment.HasValue)
            {
                var row = new Payment
                {
                    Method = PaymentMethod.Card,
                    Amount = guests * 12,
                    Status = payment.Value,
                    UserId = userId,
                    CreatedAt = createdAt
                };
                context.Payments.Add(row);
                await context.SaveChangesAsync();
                paymentId = row.Id;
            }

            var reservation = new Reservation
            {
                UserId = userId,
                ScheduleId = scheduleId,
                GuestCount = guests,
                TotalPrice = guests * 12,
                Status = ReservationStatus.Pending,
                Code = code,
                PaymentId = paymentId,
                CreatedAt = createdAt
            };
            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();
            return reservation.Id;
        }
    }
}
