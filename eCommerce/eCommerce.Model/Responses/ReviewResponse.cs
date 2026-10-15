namespace eCommerce.Model.Responses
{
    public class ReviewResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public int ActivityId { get; set; }
        public string ActivityName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string? SentimentLabel { get; set; }
        public double? SentimentScore { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
