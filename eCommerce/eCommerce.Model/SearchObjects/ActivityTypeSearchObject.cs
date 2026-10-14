namespace eCommerce.Model.SearchObjects
{
    public class ActivityTypeSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Substring to match against the activity type name.
        /// </summary>
        public string? Name { get; set; }
    }
}
