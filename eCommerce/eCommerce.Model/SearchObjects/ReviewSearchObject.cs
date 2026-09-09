namespace eCommerce.Model.SearchObjects
{
    public class ReviewSearchObject : BaseSearchObject
    {
        /// <summary>
        /// Filter reviews by activity id (public; used on activity detail pages).
        /// </summary>
        public int? ActivityId { get; set; }

        /// <summary>
        /// Filter reviews by author user id.
        /// </summary>
        public int? UserId { get; set; }
    }
}
