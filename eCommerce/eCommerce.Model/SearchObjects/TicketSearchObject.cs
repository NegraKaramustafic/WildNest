using eCommerce.Model.Enums;

namespace eCommerce.Model.SearchObjects
{
    public class TicketSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Filter tickets by owner user id (Admin only; non-admins are always scoped to themselves).
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Filter tickets by status.
        /// </summary>
        public TicketStatus? Status { get; set; }

        /// <summary>
        /// Inclusive start of a visit-date range filter.
        /// </summary>
        public DateOnly? VisitDateFrom { get; set; }

        /// <summary>
        /// Inclusive end of a visit-date range filter.
        /// </summary>
        public DateOnly? VisitDateTo { get; set; }

        /// <summary>
        /// Exact ticket code match (e.g. WN-A3F8KL).
        /// </summary>
        public string? Code { get; set; }
    }
}
