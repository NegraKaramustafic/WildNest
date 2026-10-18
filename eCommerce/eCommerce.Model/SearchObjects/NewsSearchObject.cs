namespace eCommerce.Model.SearchObjects
{
    public class NewsSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Substring matched against the title and the text.
        /// </summary>
        public string? FTS { get; set; }

        /// <summary>
        /// Admin list only. The published feed always forces this to true.
        /// </summary>
        public bool? IsPublished { get; set; }
    }
}
