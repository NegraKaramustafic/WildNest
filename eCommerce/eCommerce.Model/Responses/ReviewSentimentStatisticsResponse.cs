namespace eCommerce.Model.Responses
{
    public class ReviewSentimentStatisticsResponse
    {
        public int TotalReviews { get; set; }
        public int LabeledReviews { get; set; }
        public int UnlabeledReviews { get; set; }
        public double? OverallAverageRating { get; set; }
        public List<SentimentLabelStatisticResponse> Labels { get; set; } = new();
        public List<SentimentTrendPointResponse> MonthlyTrend { get; set; } = new();
        public List<SentimentActivityBreakdownResponse> Activities { get; set; } = new();
        public List<SentimentAnimalBreakdownResponse> Animals { get; set; } = new();
    }

    public class SentimentLabelStatisticResponse
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public double? AverageRating { get; set; }
    }

    public class SentimentTrendPointResponse
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int PositiveCount { get; set; }
        public int NeutralCount { get; set; }
        public int NegativeCount { get; set; }
    }

    public class SentimentActivityBreakdownResponse
    {
        public int ActivityId { get; set; }
        public string ActivityName { get; set; } = string.Empty;
        public int PositiveCount { get; set; }
        public int NeutralCount { get; set; }
        public int NegativeCount { get; set; }
        public double? AverageRating { get; set; }
    }

    public class SentimentAnimalBreakdownResponse
    {
        public int AnimalId { get; set; }
        public string AnimalName { get; set; } = string.Empty;
        public int PositiveCount { get; set; }
        public int NeutralCount { get; set; }
        public int NegativeCount { get; set; }
    }
}
