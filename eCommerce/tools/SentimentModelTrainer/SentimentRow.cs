using Microsoft.ML.Data;

namespace SentimentModelTrainer;

public sealed class SentimentRow
{
    public string SentimentText { get; set; } = string.Empty;

    public bool Label { get; set; }
}

public sealed class SentimentPrediction
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    public float Probability { get; set; }

    public float Score { get; set; }
}
