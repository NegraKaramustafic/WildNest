namespace eCommerce.Services.Sentiment
{
    public interface ISentimentService
    {
        /// <summary>
        /// Loads a saved model or trains one from the synthetic CSV. Failures are logged
        /// and reported as false so API startup can continue.
        /// </summary>
        Task<bool> EnsureReadyAsync(CancellationToken cancellationToken = default);

        SentimentClassification Classify(string? text);

        bool IsReady { get; }

        SentimentTrainingReport? LastReport { get; }
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
    /// Metrics from the synthetic training set. Hold-out accuracy is measured on a
    /// stratified 20% split; the saved model is trained on every row.
    /// </summary>
    public sealed class SentimentTrainingReport
    {
        public int RowCount { get; init; }
        public int PositiveRows { get; init; }
        public int NeutralRows { get; init; }
        public int NegativeRows { get; init; }
        public int BosnianRows { get; init; }
        public int HoldoutCount { get; init; }
        public double? HoldoutAccuracy { get; init; }
        public double? PositiveAccuracy { get; init; }
        public double? NeutralAccuracy { get; init; }
        public double? NegativeAccuracy { get; init; }
        public bool LoadedFromFile { get; init; }
    }
}
