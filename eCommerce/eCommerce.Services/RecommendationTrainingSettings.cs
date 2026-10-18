using eCommerce.Common.Services;

namespace eCommerce.Services
{
    public sealed class RecommendationTrainingSettings
    {
        public int IntervalMinutes { get; init; } = 360;

        public int NewReservationsBeforeRetrain { get; init; } = 10;

        public int CheckIntervalMinutes { get; init; } = 5;

        public static RecommendationTrainingSettings FromEnvironment()
        {
            return new RecommendationTrainingSettings
            {
                IntervalMinutes = ReadPositive("RECOMMENDATION_RETRAIN_INTERVAL_MINUTES", 360),
                NewReservationsBeforeRetrain = ReadPositive("RECOMMENDATION_RETRAIN_AFTER_NEW_RESERVATIONS", 10),
                CheckIntervalMinutes = ReadPositive("RECOMMENDATION_TRAINING_CHECK_MINUTES", 5)
            };
        }

        private static int ReadPositive(string name, int fallback)
        {
            var text = EnvVariables.Optional(name);
            if (text == null)
            {
                return fallback;
            }

            if (!int.TryParse(text, out var value) || value <= 0)
            {
                throw new InvalidOperationException($"{name} must be a positive integer.");
            }

            return value;
        }
    }
}
