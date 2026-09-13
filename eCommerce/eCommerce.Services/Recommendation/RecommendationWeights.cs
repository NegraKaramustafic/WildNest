namespace eCommerce.Services.Recommendation
{
    /// <summary>
    /// Fixed score terms. Every weight is added into the rank — nothing is collected and then ignored.
    /// </summary>
    public static class RecommendationWeights
    {
        public const int DefaultLimit = 5;
        public const int MaxLimit = 20;
        public const int MinTrainingRows = 5;

        public const double MatrixFactorization = 0.35;
        public const double AvgRating = 0.25;
        public const double TypeAffinity = 0.15;
        public const double AnimalAffinity = 0.10;
        public const double CategoryAffinity = 0.05;
        public const double AdoptionBoost = 0.05;
        public const double Popularity = 0.05;

        public const double HighRatingThreshold = 4.0;
        public const double PopularityCap = 10.0;
        public const float ImplicitReservationLabel = 4.0f;

        public const string Collaborative = "Collaborative";
        public const string HighRating = "HighRating";
        public const string SameType = "SameType";
        public const string SameAnimal = "SameAnimal";
        public const string SameCategory = "SameCategory";
        public const string YouAdopted = "YouAdopted";
        public const string Popular = "Popular";
        public const string Available = "Available";
    }
}
