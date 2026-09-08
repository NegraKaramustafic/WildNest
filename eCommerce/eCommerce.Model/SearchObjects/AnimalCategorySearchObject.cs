namespace eCommerce.Model.SearchObjects
{
    public class AnimalCategorySearchObject : BaseSearchObject
    {
        /// <summary>
        /// Substring to match against the category name (case-insensitive).
        /// </summary>
        public string? Name { get; set; }
    }
}
