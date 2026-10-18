namespace eCommerce.Model.SearchObjects
{
    public class UserBadgeSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Filter earned badges by user id (Admin may query any user; non-admins are scoped to themselves).
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Filter holders of one badge.
        /// </summary>
        public int? BadgeId { get; set; }

        /// <summary>
        /// Substring matched against the holder's first name, last name, and username.
        /// </summary>
        public string? FTS { get; set; }
    }
}
