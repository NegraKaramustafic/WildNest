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

        /// <summary>
        /// Positive, Neutral, or Negative. Any other value is rejected.
        /// </summary>
        public string? SentimentLabel { get; set; }

        /// <summary>
        /// LIKE/FTS substring matched against the comment, activity name, and author name.
        /// </summary>
        public string? FTS { get; set; }

        /// <summary>
        /// Exact star rating from 1 to 5.
        /// </summary>
        public int? Rating { get; set; }

        /// <summary>
        /// Inclusive start of the created-at range (UTC date).
        /// </summary>
        public DateOnly? DateFrom { get; set; }

        /// <summary>
        /// Inclusive end of the created-at range (UTC date).
        /// </summary>
        public DateOnly? DateTo { get; set; }
    }
}
