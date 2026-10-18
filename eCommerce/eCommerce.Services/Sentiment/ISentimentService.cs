namespace eCommerce.Services.Sentiment
{
    public interface ISentimentService
    {
        /// <summary>
        /// Loads the shipped binary model. A missing or invalid file is logged
        /// and reported as false so reviews can be saved without a label.
        /// </summary>
        Task<bool> EnsureReadyAsync(CancellationToken cancellationToken = default);

        SentimentClassification Classify(string? text);

        bool IsReady { get; }

        SentimentModelReport? LastReport { get; }
    }

    public sealed class SentimentClassification
    {
        public SentimentClassification(string? label, double? score)
        {
            Label = label;
            Score = score;
        }

        public string? Label { get; }

        public double? Score { get; }

        public static SentimentClassification Unlabeled { get; } = new(null, null);
    }

    /// <summary>
    /// Runtime load status. Hold-out accuracy is produced by SentimentModelTrainer,
    /// not while the API is starting.
    /// </summary>
    public sealed class SentimentModelReport
    {
        public bool LoadedFromFile { get; init; }

        public string ModelPath { get; init; } = string.Empty;
    }
}
