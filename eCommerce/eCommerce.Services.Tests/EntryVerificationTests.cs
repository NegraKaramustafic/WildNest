using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Messages;
using eCommerce.Model.Requests;
using eCommerce.Services.Database;
using eCommerce.Services.ReservationStateMachine;
using eCommerce.Services.Validators;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class EntryVerificationTests
    {
        [Fact]
        public async Task Verify_ConfirmedReservationForToday_CompletesWithoutRestoringSeats()
        {
            using var db = new SqliteDb();
            var clock = new ZooClock("Europe/Sarajevo");
            var activity = await db.Context.Activities.SingleAsync(item => item.Id == 1);
            var userId = await UserAsync(db.Context, "Mina", "Cole");
            var scheduleId = await ScheduleAsync(db.Context, clock.Today, capacityLeft: 7);
            var paymentId = await PaymentAsync(db.Context, userId);
            var reservationId = await ReservationAsync(
                db.Context, userId, scheduleId, paymentId, "WN-ENTRY1", ReservationStatus.Confirmed, guests: 2, total: 40m);

            var service = Tickets(db.Context, clock);
            var result = await service.VerifyAsync(new TicketVerifyRequest
            {
                Code = "wn-entry1",
                Method = TicketVerificationMethod.QrScan
            });

            Assert.Equal("Reservation", result.Kind);
            Assert.Null(result.Ticket);
            var check = result.Reservation;
            Assert.NotNull(check);
            Assert.Equal("WN-ENTRY1", check.Code);
            Assert.Equal("Mina Cole", check.VisitorName);
            Assert.Equal(activity.Name, check.ActivityName);
            Assert.Equal(clock.Today, check.ScheduleDate);
            Assert.Equal(2, check.GuestCount);
            Assert.Equal(40m, check.Total);

            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Reservations.SingleAsync(item => item.Id == reservationId);
            var capacity = await db.Context.Schedules.Where(item => item.Id == scheduleId).Select(item => item.CapacityLeft).SingleAsync();
            Assert.Equal(ReservationStatus.Completed, stored.Status);
            Assert.Equal(7, capacity);

            var again = await Assert.ThrowsAsync<ClientException>(() => service.VerifyAsync(new TicketVerifyRequest
            {
                Code = "WN-ENTRY1",
                Method = TicketVerificationMethod.ManualCode
            }));
            Assert.Equal("Reservation already used.", again.Message);
            db.Context.ChangeTracker.Clear();
            Assert.Equal(ReservationStatus.Completed, (await db.Context.Reservations.SingleAsync(item => item.Id == reservationId)).Status);
            Assert.Equal(7, await db.Context.Schedules.Where(item => item.Id == scheduleId).Select(item => item.CapacityLeft).SingleAsync());
        }

        [Fact]
        public async Task Verify_RejectedReservation_StaysUnchanged()
        {
            using var db = new SqliteDb();
            var clock = new ZooClock("Europe/Sarajevo");
            var userId = await UserAsync(db.Context, "Noah", "Ilic");
            var pendingSchedule = await ScheduleAsync(db.Context, clock.Today, 5);
            var cancelledSchedule = await ScheduleAsync(db.Context, clock.Today, 5);
            var unpaidSchedule = await ScheduleAsync(db.Context, clock.Today, 5);
            var yesterdayId = await ScheduleAsync(db.Context, clock.Today.AddDays(-1), 5);
            var tomorrowId = await ScheduleAsync(db.Context, clock.Today.AddDays(1), 5);
            var paid = await PaymentAsync(db.Context, userId);
            var unpaid = await PaymentAsync(db.Context, userId, PaymentStatus.Pending);

            var pendingId = await ReservationAsync(db.Context, userId, pendingSchedule, paid, "WN-PEND01", ReservationStatus.Pending, 1, 10m);
            var cancelledId = await ReservationAsync(db.Context, userId, cancelledSchedule, paid, "WN-CANC01", ReservationStatus.Cancelled, 1, 10m);
            var unpaidId = await ReservationAsync(db.Context, userId, unpaidSchedule, unpaid, "WN-UNPD01", ReservationStatus.Confirmed, 1, 10m);
            var expiredId = await ReservationAsync(db.Context, userId, yesterdayId, paid, "WN-EXPD01", ReservationStatus.Confirmed, 1, 10m);
            var futureId = await ReservationAsync(db.Context, userId, tomorrowId, paid, "WN-FUTR01", ReservationStatus.Confirmed, 1, 10m);
            var service = Tickets(db.Context, clock);

            var pending = await Assert.ThrowsAsync<ClientException>(() => Verify(service, "WN-PEND01"));
            var cancelled = await Assert.ThrowsAsync<ClientException>(() => Verify(service, "WN-CANC01"));
            var unpaidError = await Assert.ThrowsAsync<ClientException>(() => Verify(service, "WN-UNPD01"));
            var expired = await Assert.ThrowsAsync<ClientException>(() => Verify(service, "WN-EXPD01"));
            var future = await Assert.ThrowsAsync<ClientException>(() => Verify(service, "WN-FUTR01"));
            var missing = await Assert.ThrowsAsync<NotFoundException>(() => Verify(service, "WN-NONE01"));

            Assert.Equal("Reservation is not confirmed.", pending.Message);
            Assert.Equal("Reservation is cancelled.", cancelled.Message);
            Assert.Equal("Reservation is not confirmed.", unpaidError.Message);
            Assert.Equal($"Reservation expired. It was valid for {clock.Today.AddDays(-1):dd.MM.yyyy}.", expired.Message);
            Assert.Equal($"Reservation is valid for {clock.Today.AddDays(1):dd.MM.yyyy}, not today.", future.Message);
            Assert.Equal("A ticket with this code does not exist.", missing.Message);

            db.Context.ChangeTracker.Clear();
            Assert.Equal(ReservationStatus.Pending, (await db.Context.Reservations.FindAsync(pendingId))!.Status);
            Assert.Equal(ReservationStatus.Cancelled, (await db.Context.Reservations.FindAsync(cancelledId))!.Status);
            Assert.Equal(ReservationStatus.Confirmed, (await db.Context.Reservations.FindAsync(unpaidId))!.Status);
            Assert.Equal(ReservationStatus.Confirmed, (await db.Context.Reservations.FindAsync(expiredId))!.Status);
            Assert.Equal(ReservationStatus.Confirmed, (await db.Context.Reservations.FindAsync(futureId))!.Status);
            Assert.Equal(5, await db.Context.Schedules.Where(item => item.Id == pendingSchedule).Select(item => item.CapacityLeft).SingleAsync());
            Assert.Equal(5, await db.Context.Schedules.Where(item => item.Id == cancelledSchedule).Select(item => item.CapacityLeft).SingleAsync());
            Assert.Equal(5, await db.Context.Schedules.Where(item => item.Id == unpaidSchedule).Select(item => item.CapacityLeft).SingleAsync());
            Assert.Equal(5, await db.Context.Schedules.Where(item => item.Id == yesterdayId).Select(item => item.CapacityLeft).SingleAsync());
            Assert.Equal(5, await db.Context.Schedules.Where(item => item.Id == tomorrowId).Select(item => item.CapacityLeft).SingleAsync());
        }

        [Fact]
        public async Task Verify_Ticket_StillMarksItUsedOnce()
        {
            using var db = new SqliteDb();
            var clock = new ZooClock("Europe/Sarajevo");
            var paymentId = await PaymentAsync(db.Context, 4);
            db.Context.Tickets.Add(new Ticket
            {
                UserId = 4,
                VisitDate = clock.Today,
                VisitorCount = 1,
                PricePerPerson = 10m,
                TotalPrice = 10m,
                Code = "WN-TCKT01",
                QrCodeData = "WN-TCKT01",
                Status = TicketStatus.Active,
                PaymentId = paymentId,
                CreatedAt = DateTime.UtcNow
            });
            await db.Context.SaveChangesAsync();

            var service = Tickets(db.Context, clock);
            var result = await service.VerifyAsync(new TicketVerifyRequest
            {
                Code = "WN-TCKT01",
                Method = TicketVerificationMethod.ManualCode
            });
            var again = await Assert.ThrowsAsync<ClientException>(() => service.VerifyAsync(new TicketVerifyRequest
            {
                Code = "WN-TCKT01",
                Method = TicketVerificationMethod.ManualCode
            }));

            Assert.Equal("Ticket", result.Kind);
            Assert.Equal(TicketStatus.Used, result.Ticket!.Status);
            Assert.Equal("Ticket already used.", again.Message);
            db.Context.ChangeTracker.Clear();
            Assert.Equal(TicketStatus.Used, (await db.Context.Tickets.SingleAsync(item => item.Code == "WN-TCKT01")).Status);
        }

        private static Task Verify(TicketService service, string code)
        {
            return service.VerifyAsync(new TicketVerifyRequest
            {
                Code = code,
                Method = TicketVerificationMethod.QrScan
            });
        }

        private static TicketService Tickets(ECommerceDbContext context, ZooClock clock)
        {
            return new TicketService(
                context,
                new Mapper(),
                new TicketInsertValidator(clock),
                new TicketUpdateValidator(clock),
                new TestUserAccessor { UserId = 1, Admin = true },
                new NotificationEventService(new CapturingPublisher(), context),
                new RecordingPaymentService(),
                clock,
                Reservations(context),
                new SilentLoggerFactory());
        }

        private static ReservationService Reservations(ECommerceDbContext context)
        {
            var loggerFactory = new SilentLoggerFactory();
            var mapper = new Mapper();
            var accessor = new TestUserAccessor { UserId = 1, Admin = true };
            var provider = new StateBag();
            var clock = new ZooClock("Europe/Sarajevo");
            var pending = new PendingReservationState(context, mapper, provider, loggerFactory);
            var confirmed = new ConfirmedReservationState(context, mapper, provider, loggerFactory);
            var completed = new CompletedReservationState(context, mapper, provider, loggerFactory);
            var cancelled = new CancelledReservationState(context, mapper, provider, loggerFactory);
            var initial = new InitialReservationState(
                context,
                mapper,
                provider,
                loggerFactory,
                new ReservationInsertValidator(),
                accessor,
                clock);
            provider.Add(typeof(PendingReservationState), pending);
            provider.Add(typeof(ConfirmedReservationState), confirmed);
            provider.Add(typeof(CompletedReservationState), completed);
            provider.Add(typeof(CancelledReservationState), cancelled);
            provider.Add(typeof(InitialReservationState), initial);
            var root = new BaseReservationState(context, mapper, provider, loggerFactory);
            return new ReservationService(
                context,
                mapper,
                root,
                accessor,
                new NotificationEventService(new CapturingPublisher(), context),
                new RecordingPaymentService(),
                new ReservationCancelValidator(),
                loggerFactory);
        }

        private static async Task<int> UserAsync(ECommerceDbContext context, string first, string last)
        {
            var user = new User
            {
                FirstName = first,
                LastName = last,
                Email = $"{first}.{last}@example.com".ToLowerInvariant(),
                Username = $"{first}{last}".ToLowerInvariant(),
                PasswordHash = "hash",
                PasswordSalt = "salt",
                SecurityStamp = Guid.NewGuid().ToString("N"),
                IsActive = true
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user.Id;
        }

        private static async Task<int> ScheduleAsync(ECommerceDbContext context, DateOnly date, int capacityLeft)
        {
            var schedule = new Schedule
            {
                ActivityId = 1,
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

        private static async Task<int> PaymentAsync(
            ECommerceDbContext context,
            int userId,
            PaymentStatus status = PaymentStatus.Completed)
        {
            var payment = new Payment
            {
                Method = PaymentMethod.Card,
                Amount = 40m,
                Status = status,
                UserId = userId,
                RelatedEntityType = nameof(PaymentRelatedEntityType.Reservation),
                CreatedAt = DateTime.UtcNow
            };
            context.Payments.Add(payment);
            await context.SaveChangesAsync();
            return payment.Id;
        }

        private static async Task<int> ReservationAsync(
            ECommerceDbContext context,
            int userId,
            int scheduleId,
            int paymentId,
            string code,
            ReservationStatus status,
            int guests,
            decimal total)
        {
            var reservation = new Reservation
            {
                UserId = userId,
                ScheduleId = scheduleId,
                GuestCount = guests,
                TotalPrice = total,
                Status = status,
                Code = code,
                PaymentId = paymentId,
                CreatedAt = DateTime.UtcNow
            };
            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();
            return reservation.Id;
        }

        private sealed class CapturingPublisher : INotificationPublisher
        {
            public Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
