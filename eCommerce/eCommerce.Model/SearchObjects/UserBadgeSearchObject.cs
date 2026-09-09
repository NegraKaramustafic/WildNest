namespace eCommerce.Model.SearchObjects
{
    public class UserBadgeSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Filter earned badges by user id (Admin may query any user; non-admins are scoped to themselves).
        /// </summary>
        public int? UserId { get; set; }
    }
}
