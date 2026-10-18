using eCommerce.Model.Enums;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
    }
}
