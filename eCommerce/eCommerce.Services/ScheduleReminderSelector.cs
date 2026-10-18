using eCommerce.Model.Enums;

namespace eCommerce.Services
{
    /// <summary>
    /// Zoo-local window used to decide which schedules and tickets are due for a reminder.
    /// Start is exclusive (already started) and the end is inclusive.
    /// </summary>
    public readonly record struct LocalReminderWindow(DateTime StartExclusive, DateTime EndInclusive)
    {
        public DateOnly Today => DateOnly.FromDateTime(StartExclusive);

        public DateOnly EndDate => DateOnly.FromDateTime(EndInclusive);

        public TimeOnly StartTime => TimeOnly.FromDateTime(StartExclusive);

        public TimeOnly EndTime => TimeOnly.FromDateTime(EndInclusive);
    }

    public static class ScheduleReminderSelector
    {
        public static bool IsReservationDue(
            ReservationStatus reservationStatus,
            ScheduleStatus scheduleStatus,
            ActivityStatus activityStatus,
            DateOnly date,
            TimeOnly startTime,
            DateTime? reminderSentAt,
            LocalReminderWindow window)
        {
            if (reminderSentAt != null)
            {
                return false;
            }

            if (reservationStatus != ReservationStatus.Confirmed)
            {
                return false;
            }

            if (scheduleStatus != ScheduleStatus.Active)
            {
                return false;
            }

            if (activityStatus != ActivityStatus.Active)
            {
                return false;
            }

            var start = date.ToDateTime(startTime);
            return start > window.StartExclusive && start <= window.EndInclusive;
        }

        public static bool IsTicketDue(
            TicketStatus status,
            DateOnly visitDate,
            DateTime? reminderSentAt,
            LocalReminderWindow window)
        {
            if (reminderSentAt != null)
            {
                return false;
            }

            if (status != TicketStatus.Active)
            {
                return false;
            }

            return visitDate >= window.Today && visitDate <= window.EndDate;
        }
    }
}
