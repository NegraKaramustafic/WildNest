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
    }
}
