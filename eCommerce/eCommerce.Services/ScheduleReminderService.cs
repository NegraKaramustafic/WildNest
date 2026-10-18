using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using eCommerce.Model.Enums;
using eCommerce.Model.Messages;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services
{
    public class ScheduleReminderService : IScheduleReminderService
    {
        private readonly ECommerceDbContext _dbContext;
        private readonly INotificationPublisher _publisher;
        private readonly ZooClock _clock;
        private readonly ReminderSettings _settings;
        private readonly ILogger<ScheduleReminderService> _logger;

        public ScheduleReminderService(
            ECommerceDbContext dbContext,
            INotificationPublisher publisher,
            ZooClock clock,
            ReminderSettings settings,
            ILogger<ScheduleReminderService> logger)
        {
            _dbContext = dbContext;
            _publisher = publisher;
            _clock = clock;
            _settings = settings;
            _logger = logger;
        }

        public async Task<int> SendDueRemindersAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var window = _clock.UpcomingWindow(now, TimeSpan.FromHours(_settings.HoursBefore));
            var sent = 0;

            var reservations = await DueReservations(window).ToListAsync(cancellationToken);
            foreach (var item in reservations)
            {
                var claimed = await _dbContext.Reservations
                    .Where(reservation => reservation.Id == item.Id && reservation.ReminderSentAt == null)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(reservation => reservation.ReminderSentAt, now),
                        cancellationToken);
                if (claimed != 1)
                {
                    continue;
                }

                var when = FormatStart(item.Date, item.StartTime);
                try
                {
                    await _publisher.PublishAsync(new NotificationMessage
                    {
                        MessageId = MessageIdFor("reservation", item.Id),
                        UserId = item.UserId,
                        Type = NotificationType.UpcomingReminder,
                        Title = "Upcoming schedule",
                        Message = $"Your reservation for {item.ActivityName} starts on {when}.",
                        RelatedEntityType = "Reservation",
                        RelatedEntityId = item.Id
                    }, cancellationToken);
                    sent++;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to publish the reminder for reservation {ReservationId}. The claim will be released so the next run can retry.",
                        item.Id);
                    await _dbContext.Reservations
                        .Where(reservation => reservation.Id == item.Id && reservation.ReminderSentAt == now)
                        .ExecuteUpdateAsync(
                            setters => setters.SetProperty(reservation => reservation.ReminderSentAt, (DateTime?)null),
                            cancellationToken);
                }
            }

            var tickets = await DueTickets(window).ToListAsync(cancellationToken);
            foreach (var item in tickets)
            {
                var claimed = await _dbContext.Tickets
                    .Where(ticket => ticket.Id == item.Id && ticket.ReminderSentAt == null)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(ticket => ticket.ReminderSentAt, now),
                        cancellationToken);
                if (claimed != 1)
                {
                    continue;
                }

                var when = FormatDate(item.VisitDate);
                try
                {
                    await _publisher.PublishAsync(new NotificationMessage
                    {
                        MessageId = MessageIdFor("ticket", item.Id),
                        UserId = item.UserId,
                        Type = NotificationType.UpcomingReminder,
                        Title = "Upcoming visit",
                        Message = $"Your zoo entry ticket {item.Code} is for {when}.",
                        RelatedEntityType = "Ticket",
                        RelatedEntityId = item.Id
                    }, cancellationToken);
                    sent++;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to publish the reminder for ticket {TicketId}. The claim will be released so the next run can retry.",
                        item.Id);
                    await _dbContext.Tickets
                        .Where(ticket => ticket.Id == item.Id && ticket.ReminderSentAt == now)
                        .ExecuteUpdateAsync(
                            setters => setters.SetProperty(ticket => ticket.ReminderSentAt, (DateTime?)null),
                            cancellationToken);
                }
            }

            return sent;
        }

        private IQueryable<DueReservation> DueReservations(LocalReminderWindow window)
        {
            var startDate = window.Today;
            var startTime = window.StartTime;
            var endDate = window.EndDate;
            var endTime = window.EndTime;

            return _dbContext.Reservations
                .AsNoTracking()
                .Where(reservation =>
                    reservation.ReminderSentAt == null
                    && reservation.Status == ReservationStatus.Confirmed
                    && reservation.Schedule.Status == ScheduleStatus.Active
                    && reservation.Schedule.Activity.Status == ActivityStatus.Active)
                .Where(reservation =>
                    reservation.Schedule.Date > startDate
                    || (reservation.Schedule.Date == startDate && reservation.Schedule.StartTime > startTime))
                .Where(reservation =>
                    reservation.Schedule.Date < endDate
                    || (reservation.Schedule.Date == endDate && reservation.Schedule.StartTime <= endTime))
                .Select(reservation => new DueReservation(
                    reservation.Id,
                    reservation.UserId,
                    reservation.Schedule.Activity.Name,
                    reservation.Schedule.Date,
                    reservation.Schedule.StartTime));
        }

        private IQueryable<DueTicket> DueTickets(LocalReminderWindow window)
        {
            var today = window.Today;
            var endDate = window.EndDate;

            return _dbContext.Tickets
                .AsNoTracking()
                .Where(ticket =>
                    ticket.ReminderSentAt == null
                    && ticket.Status == TicketStatus.Active
                    && ticket.VisitDate >= today
                    && ticket.VisitDate <= endDate)
                .Select(ticket => new DueTicket(ticket.Id, ticket.UserId, ticket.Code, ticket.VisitDate));
        }

        private string FormatStart(DateOnly date, TimeOnly time)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{date:dd MMMM yyyy} at {time:HH:mm} ({_clock.TimeZoneId})");
        }

        private string FormatDate(DateOnly date)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{date:dd MMMM yyyy} ({_clock.TimeZoneId})");
        }

        private static Guid MessageIdFor(string kind, int id)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"wildnest-reminder:{kind}:{id}"));
            return new Guid(hash.AsSpan(0, 16));
        }

        private sealed record DueReservation(int Id, int UserId, string ActivityName, DateOnly Date, TimeOnly StartTime);

        private sealed record DueTicket(int Id, int UserId, string Code, DateOnly VisitDate);
    }
}
