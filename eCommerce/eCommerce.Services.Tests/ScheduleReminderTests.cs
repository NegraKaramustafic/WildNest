using eCommerce.Model.Enums;
using eCommerce.Model.Messages;
using eCommerce.Services;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class ScheduleReminderTests
    {
        private static readonly LocalReminderWindow Window = new(
            new DateTime(2026, 10, 18, 15, 0, 0),
            new DateTime(2026, 10, 19, 15, 0, 0));

        [Fact]
        public void Selector_IncludesAScheduleInsideTheWindow()
        {
            Assert.True(Due(new DateOnly(2026, 10, 19), new TimeOnly(10, 0)));
        }

        [Fact]
        public void Selector_SkipsAScheduleOutsideTheWindow()
        {
            Assert.False(Due(new DateOnly(2026, 10, 19), new TimeOnly(16, 0)));
        }

        [Fact]
        public void Selector_SkipsAScheduleThatAlreadyStarted()
        {
            Assert.False(Due(new DateOnly(2026, 10, 18), new TimeOnly(14, 0)));
        }

        [Fact]
        public void Selector_SkipsAnAlreadyRemindedReservation()
        {
            Assert.False(Due(new DateOnly(2026, 10, 19), new TimeOnly(10, 0), reminderSentAt: new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc)));
        }

        [Fact]
        public void Selector_SkipsCancelledReservationScheduleAndInactiveActivity()
        {
            Assert.False(Due(new DateOnly(2026, 10, 19), new TimeOnly(10, 0), reservationStatus: ReservationStatus.Cancelled));
            Assert.False(Due(new DateOnly(2026, 10, 19), new TimeOnly(10, 0), scheduleStatus: ScheduleStatus.Cancelled));
            Assert.False(Due(new DateOnly(2026, 10, 19), new TimeOnly(10, 0), activityStatus: ActivityStatus.Inactive));
            Assert.False(Due(new DateOnly(2026, 10, 19), new TimeOnly(10, 0), reservationStatus: ReservationStatus.Pending));
        }

        [Fact]
        public async Task Service_PublishesOnlyTheReservationInsideTheWindow_AndOnlyOnce()
        {
            using var db = new SqliteDb();
            var publisher = new CapturingPublisher();
            var clock = new ZooClock("UTC");
            var now = DateTime.UtcNow;
            var inside = await AddReservationAsync(db, now.AddHours(2), ReservationStatus.Confirmed, "Bird watch");
            await AddReservationAsync(db, now.AddHours(48), ReservationStatus.Confirmed, "Too far");
            await AddReservationAsync(db, now.AddHours(-2), ReservationStatus.Confirmed, "Already started");
            await AddReservationAsync(db, now.AddHours(3), ReservationStatus.Cancelled, "Cancelled booking");
            var reminded = await AddReservationAsync(db, now.AddHours(4), ReservationStatus.Confirmed, "Already reminded");
            var remindedRow = await db.Context.Reservations.SingleAsync(row => row.Id == reminded);
            remindedRow.ReminderSentAt = now;
            await AddReservationAsync(db, now.AddHours(3), ReservationStatus.Confirmed, "Cancelled schedule", ScheduleStatus.Cancelled);
            await db.Context.SaveChangesAsync();

            var service = new ScheduleReminderService(
                db.Context,
                publisher,
                clock,
                new ReminderSettings { HoursBefore = 24, CheckIntervalMinutes = 15 },
                NullLogger<ScheduleReminderService>.Instance);

            var sent = await service.SendDueRemindersAsync();
            Assert.Equal(1, sent);
            var message = Assert.Single(publisher.Messages);
            Assert.Equal(inside, message.RelatedEntityId);
            Assert.Equal(NotificationType.UpcomingReminder, message.Type);
            Assert.Contains("Bird watch", message.Message);
            Assert.Contains("at ", message.Message);
            Assert.NotNull(await db.Context.Reservations.Where(row => row.Id == inside).Select(row => row.ReminderSentAt).SingleAsync());

            var again = await service.SendDueRemindersAsync();
            Assert.Equal(0, again);
            Assert.Single(publisher.Messages);
        }

        [Fact]
        public async Task Service_ReleasesTheReservationClaim_WhenPublishFails_AndRetriesNextRun()
        {
            using var db = new SqliteDb();
            var publisher = new FailOncePublisher();
            var inside = await AddReservationAsync(db, DateTime.UtcNow.AddHours(2), ReservationStatus.Confirmed, "Bird watch");
            var service = ReminderService(db, publisher);

            var sent = await service.SendDueRemindersAsync();

            Assert.Equal(0, sent);
            Assert.Equal(1, publisher.Calls);
            Assert.Null(await db.Context.Reservations.Where(row => row.Id == inside).Select(row => row.ReminderSentAt).SingleAsync());

            var retried = await service.SendDueRemindersAsync();
            Assert.Equal(1, retried);
            Assert.Equal(2, publisher.Calls);
            Assert.NotNull(await db.Context.Reservations.Where(row => row.Id == inside).Select(row => row.ReminderSentAt).SingleAsync());
        }

        [Fact]
        public async Task Service_ReleasesTheTicketClaim_WhenPublishFails()
        {
            using var db = new SqliteDb();
            var publisher = new FailOncePublisher();
            var ticketId = await AddTicketAsync(db, DateOnly.FromDateTime(DateTime.UtcNow));
            var service = ReminderService(db, publisher);

            var sent = await service.SendDueRemindersAsync();

            Assert.Equal(0, sent);
            Assert.Null(await db.Context.Tickets.Where(row => row.Id == ticketId).Select(row => row.ReminderSentAt).SingleAsync());

            var retried = await service.SendDueRemindersAsync();
            Assert.Equal(1, retried);
            Assert.NotNull(await db.Context.Tickets.Where(row => row.Id == ticketId).Select(row => row.ReminderSentAt).SingleAsync());
        }

        [Fact]
        public void Publisher_MarksMessagesPersistent()
        {
            var properties = RabbitMqNotificationPublisher.CreateProperties(new NotificationMessage
            {
                MessageId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                Type = NotificationType.UpcomingReminder
            });

            Assert.True(properties.Persistent);
            Assert.Equal(DeliveryModes.Persistent, properties.DeliveryMode);
            Assert.Equal("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", properties.MessageId);
        }

        [Fact]
        public async Task Publisher_ReportsConnectionFailure_InsteadOfSwallowingIt()
        {
            await using var publisher = new RabbitMqNotificationPublisher(
                NullLogger<RabbitMqNotificationPublisher>.Instance,
                RabbitMqSettings.Create("127.0.0.1", 1, "guest", "guest", "wildnest.notifications"));

            var error = await Assert.ThrowsAnyAsync<Exception>(() => publisher.PublishAsync(new NotificationMessage
            {
                MessageId = Guid.NewGuid(),
                UserId = 4,
                Type = NotificationType.UpcomingReminder,
                Title = "Upcoming schedule",
                Message = "Your reservation starts soon."
            }));

            Assert.False(string.IsNullOrWhiteSpace(error.Message));
        }

        private static ScheduleReminderService ReminderService(SqliteDb db, INotificationPublisher publisher)
        {
            return new ScheduleReminderService(
                db.Context,
                publisher,
                new ZooClock("UTC"),
                new ReminderSettings { HoursBefore = 24, CheckIntervalMinutes = 15 },
                NullLogger<ScheduleReminderService>.Instance);
        }

        private static async Task<int> AddTicketAsync(SqliteDb db, DateOnly visitDate)
        {
            var ticket = new Ticket
            {
                UserId = 4,
                VisitDate = visitDate,
                VisitorCount = 1,
                PricePerPerson = 8,
                TotalPrice = 8,
                Code = "T" + Random.Shared.Next(100000, 999999),
                QrCodeData = "QR",
                Status = TicketStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            db.Context.Tickets.Add(ticket);
            await db.Context.SaveChangesAsync();
            return ticket.Id;
        }

        private static bool Due(
            DateOnly date,
            TimeOnly start,
            DateTime? reminderSentAt = null,
            ReservationStatus reservationStatus = ReservationStatus.Confirmed,
            ScheduleStatus scheduleStatus = ScheduleStatus.Active,
            ActivityStatus activityStatus = ActivityStatus.Active)
        {
            return ScheduleReminderSelector.IsReservationDue(
                reservationStatus,
                scheduleStatus,
                activityStatus,
                date,
                start,
                reminderSentAt,
                Window);
        }

        private static async Task<int> AddReservationAsync(
            SqliteDb db,
            DateTime startUtc,
            ReservationStatus status,
            string activityName,
            ScheduleStatus scheduleStatus = ScheduleStatus.Active)
        {
            var activity = new Activity
            {
                Name = activityName,
                ActivityTypeId = 1,
                Duration = TimeSpan.FromHours(1),
                Price = 10,
                Capacity = 10,
                Status = ActivityStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            db.Context.Activities.Add(activity);
            await db.Context.SaveChangesAsync();

            var schedule = new Schedule
            {
                ActivityId = activity.Id,
                Date = DateOnly.FromDateTime(startUtc),
                StartTime = TimeOnly.FromDateTime(startUtc),
                EndTime = TimeOnly.FromDateTime(startUtc.AddHours(1)),
                CapacityLeft = 8,
                Status = scheduleStatus
            };
            db.Context.Schedules.Add(schedule);
            await db.Context.SaveChangesAsync();

            var reservation = new Reservation
            {
                UserId = 4,
                ScheduleId = schedule.Id,
                GuestCount = 1,
                TotalPrice = 10,
                Status = status,
                Code = "R" + Random.Shared.Next(100000, 999999),
                CreatedAt = DateTime.UtcNow
            };
            db.Context.Reservations.Add(reservation);
            await db.Context.SaveChangesAsync();
            return reservation.Id;
        }

        private sealed class FailOncePublisher : INotificationPublisher
        {
            public int Calls { get; private set; }

            public Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default)
            {
                Calls++;
                if (Calls == 1)
                {
                    throw new InvalidOperationException("Broker unavailable.");
                }

                return Task.CompletedTask;
            }
        }
    }
}
