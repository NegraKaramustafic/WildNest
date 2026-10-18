using eCommerce.Model.Enums;

namespace eCommerce.Model.SearchObjects
{
    public class ReservationSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Filter reservations by owner user id (Admin only; non-admins are always scoped to themselves).
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Filter reservations by schedule id.
        /// </summary>
        public int? ScheduleId { get; set; }

        /// <summary>
        /// Filter reservations by status.
        /// </summary>
        public ReservationStatus? Status { get; set; }

        /// <summary>
        /// Filter reservations by the schedule's activity.
        /// </summary>
        public int? ActivityId { get; set; }

        /// <summary>
        /// Inclusive start of the schedule date range.
        /// </summary>
        public DateOnly? DateFrom { get; set; }

        /// <summary>
        /// Inclusive end of the schedule date range.
        /// </summary>
        public DateOnly? DateTo { get; set; }

        /// <summary>
        /// Search code, customer name, and activity name.
        /// </summary>
        public string? FTS { get; set; }
    }
}
