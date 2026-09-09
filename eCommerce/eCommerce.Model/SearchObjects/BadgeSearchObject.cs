using eCommerce.Model.Enums;

namespace eCommerce.Model.SearchObjects
{
    public class BadgeSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Case-insensitive substring match against Name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Filter badges by award criteria.
        /// </summary>
        public BadgeCriteria? Criteria { get; set; }
    }
}
