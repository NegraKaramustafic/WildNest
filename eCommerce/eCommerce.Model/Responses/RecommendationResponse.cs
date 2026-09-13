using eCommerce.Model.Enums;

namespace eCommerce.Model.Responses
{
    public class RecommendationResponse
    {
        public DateTime GeneratedAt { get; set; }

        public DateTime? ModelTrainedAt { get; set; }

        public int TrainingRowCount { get; set; }

        public List<RecommendationItemResponse> Items { get; set; } = new();
    }

    public class RecommendationItemResponse
    {
        public int ActivityId { get; set; }

        public string Name { get; set; } = string.Empty;

        public ActivityType Type { get; set; }

        public int? AnimalId { get; set; }

        public string? AnimalName { get; set; }

        public string? ImageUrl { get; set; }

        public decimal Price { get; set; }

        public double Score { get; set; }

        public double AvgRating { get; set; }

        public int ReviewCount { get; set; }

        public List<RecommendationReasonResponse> Reasons { get; set; } = new();
    }

    public class RecommendationReasonResponse
    {
        public string Code { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;
    }

    public class RecommendationRetrainResponse
    {
        public DateTime TrainedAt { get; set; }

        public int TrainingRowCount { get; set; }
    }
}
