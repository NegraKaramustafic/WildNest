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
    }
}
