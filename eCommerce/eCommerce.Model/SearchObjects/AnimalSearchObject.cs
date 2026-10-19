using eCommerce.Model.Enums;

namespace eCommerce.Model.SearchObjects
{
    public class AnimalSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Filter animals by category id.
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// Filter animals by tag.
        /// </summary>
        public AnimalTag? Tag { get; set; }

        /// <summary>
        /// Substring matched against Name only.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// LIKE/FTS substring matched against Name and Species (case-insensitive).
        /// </summary>
        public string? FTS { get; set; }
    }
}
