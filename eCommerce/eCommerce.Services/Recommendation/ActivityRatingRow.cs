namespace eCommerce.Services.Recommendation
{
    public class ActivityRatingRow
    {
        public float UserId { get; set; }

        public float ActivityId { get; set; }

        public float Label { get; set; }
    }

    public class ActivityRatingPrediction
    {
        public float Score { get; set; }
    }
}
