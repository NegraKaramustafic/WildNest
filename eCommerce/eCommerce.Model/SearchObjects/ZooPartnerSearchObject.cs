namespace eCommerce.Model.SearchObjects
{
    public class ZooPartnerSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Filter partners by active flag.
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Case-insensitive substring match against Name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Substring matched against name, location, and description.
        /// </summary>
        public string? FTS { get; set; }
    }
}
