using eCommerce.Model.Enums;

namespace eCommerce.Model.SearchObjects
{
    public class NotificationSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Filter by user id (Admin may query any user; non-admins are scoped to themselves).
        /// </summary>
        public int? UserId { get; set; }

        public bool? IsRead { get; set; }

        public NotificationType? Type { get; set; }
    }
}
