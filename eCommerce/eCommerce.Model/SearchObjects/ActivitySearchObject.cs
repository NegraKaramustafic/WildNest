using eCommerce.Model.Enums;

namespace eCommerce.Model.SearchObjects
{
    public class ActivitySearchObject : BaseSearchObject
    {
        /// <summary>
        /// Filter activities by activity type id.
        /// </summary>
        public int? ActivityTypeId { get; set; }

        /// <summary>
        /// Filter activities by status.
        /// </summary>
        public ActivityStatus? Status { get; set; }

        /// <summary>
        /// Substring to match against activity name (case-insensitive).
        /// </summary>
        public string? Name { get; set; }
    }
}
