namespace eCommerce.Services.Recommendation
{
    /// <summary>
    /// Limits and the explanation texts the recommender is allowed to show.
    /// Scoring starts from the matrix-factorization score, or from reservation-count
    /// popularity when that model cannot be used. When the activity has at least one
    /// review, that base score is multiplied by the average rating divided by 5.
    /// </summary>
    public static class RecommendationWeights
    {
        public const int DefaultLimit = 5;
        public const int MaxLimit = 20;

        /// <summary>
        /// Distinct confirmed or completed activities required before a visitor
        /// who is already in the cached model gets a personalized list.
        /// </summary>
        public const int MinimumDistinctReservedActivities = 1;

        /// <summary>
        /// Matrix factorization needs more than one visitor and one activity.
        /// Below this, the cached model stays empty and popularity is used.
        /// </summary>
        public const int MinimumTrainingPairs = 4;

        public const float PositiveInteractionLabel = 1.0f;

        public const string SimilarReservations = "SimilarReservations";
        public const string Popular = "Popular";
        public const string NewActivity = "NewActivity";

        public const string SimilarReservationsText = "Visitors with similar reservations also booked this.";
        public const string PopularText = "Popular with visitors.";
        public const string NewActivityText = "New at the zoo.";
    }
}
