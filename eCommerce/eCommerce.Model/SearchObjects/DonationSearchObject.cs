using eCommerce.Model.Enums;

namespace eCommerce.Model.SearchObjects
{
    public class DonationSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Filter donations by owner user id (Admin only; non-admins are always scoped to themselves).
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Filter donations by zoo partner id.
        /// </summary>
        public int? ZooPartnerId { get; set; }

        /// <summary>
        /// Filter by the linked payment status. Donations with no payment are excluded.
        /// </summary>
        public PaymentStatus? PaymentStatus { get; set; }

        /// <summary>
        /// When false, only donations that have no payment. When true, only donations that have one.
        /// </summary>
        public bool? HasPayment { get; set; }

        /// <summary>
        /// Inclusive start of the created-at range (UTC date).
        /// </summary>
        public DateOnly? DateFrom { get; set; }

        /// <summary>
        /// Inclusive end of the created-at range (UTC date).
        /// </summary>
        public DateOnly? DateTo { get; set; }

        /// <summary>
        /// Search customer name and zoo partner name.
        /// </summary>
        public string? FTS { get; set; }
    }
}
