using eCommerce.Common.Services;
using eCommerce.Model;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace eCommerce.Services.Sentiment
{
    /// <summary>
    /// Binary positive/negative classifier. Loads the model zip shipped with the
    /// repo. PredictionEngine is not thread-safe, so scoring is serialized.
    /// </summary>
    public sealed class SentimentService : ISentimentService, IDisposable
    {
        public const int MaxTextLength = 2000;

        private readonly ILogger<SentimentService> _logger;
        private readonly string _modelPath;
        private readonly object _sync = new();
        private readonly SemaphoreSlim _gate = new(1, 1);
        private PredictionEngine<SentimentDataRow, SentimentPrediction>? _engine;
        private int _state;

        public SentimentService(ILogger<SentimentService> logger, string? modelPath = null)
        {
            _logger = logger;
            _modelPath = ResolveModelPath(modelPath);
        }

        public bool IsReady
        {
            get
            {
                lock (_sync)
                {
                    return _engine != null;
                }
            }
        }

        public SentimentModelReport? LastReport { get; private set; }

        public async Task<bool> EnsureReadyAsync(CancellationToken cancellationToken = default)
        {
            if (Volatile.Read(ref _state) != 0)
            {
                return IsReady;
            }

            await _gate.WaitAsync(cancellationToken);
            try
            {
                if (_state != 0)
                {
                    return IsReady;
                }

                if (TryLoad())
                {
                    LastReport = new SentimentModelReport
                    {
                        LoadedFromFile = true,
                        ModelPath = _modelPath
                    };
                    _logger.LogInformation("Loaded sentiment model from {Path}.", _modelPath);
                    _state = 1;
                    return true;
                }

                _state = 2;
                return false;
            }
            finally
            {
                _gate.Release();
            }
        }

        public SentimentClassification Classify(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return SentimentClassification.Unlabeled;
            }

            var normalized = text.Trim();
            if (normalized.Length > MaxTextLength)
            {
                normalized = normalized[..MaxTextLength];
            }

            lock (_sync)
            {
                if (_engine == null)
                {
                    return SentimentClassification.Unlabeled;
                }

                var prediction = _engine.Predict(new SentimentDataRow
                {
                    SentimentText = normalized,
                    Label = false
                });

                var confidence = Confidence(prediction);
                if (confidence == null)
                {
                    _logger.LogWarning("Sentiment model returned a score that could not be used.");
                    return SentimentClassification.Unlabeled;
                }

                var label = prediction.PredictedLabel
                    ? SentimentLabels.Positive
                    : SentimentLabels.Negative;
                return new SentimentClassification(label, confidence.Value);
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _engine?.Dispose();
                _engine = null;
            }

            _gate.Dispose();
        }

        private bool TryLoad()
        {
            if (!File.Exists(_modelPath))
            {
                _logger.LogError(
                    "Sentiment model file was not found at {Path}. Reviews will be saved without a label until the file is available.",
                    _modelPath);
                return false;
            }

            try
            {
                var ml = new MLContext(seed: 1);
                var model = ml.Model.Load(_modelPath, out _);
                var engine = ml.Model.CreatePredictionEngine<SentimentDataRow, SentimentPrediction>(model);
                lock (_sync)
                {
                    _engine?.Dispose();
                    _engine = engine;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Sentiment model at {Path} could not be loaded. Reviews will be saved without a label until a valid model is available.",
                    _modelPath);
                return false;
            }
        }

        private static string ResolveModelPath(string? configured)
        {
            var path = string.IsNullOrWhiteSpace(configured)
                ? EnvVariables.Optional("SENTIMENT_MODEL_PATH")
                : configured.Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                return Path.Combine(AppContext.BaseDirectory, "ml-models", "review-sentiment.zip");
            }

            return Path.IsPathRooted(path)
                ? path
                : Path.Combine(AppContext.BaseDirectory, path);
        }

        private static double? Confidence(SentimentPrediction prediction)
        {
            var positiveProbability = prediction.Probability;
            if (float.IsNaN(positiveProbability) || float.IsInfinity(positiveProbability))
            {
                var raw = prediction.Score;
                if (float.IsNaN(raw) || float.IsInfinity(raw))
                {
                    return null;
                }

                positiveProbability = 1f / (1f + (float)Math.Exp(-raw));
            }

            var confidence = prediction.PredictedLabel
                ? positiveProbability
                : 1d - positiveProbability;
            if (double.IsNaN(confidence) || double.IsInfinity(confidence))
            {
                return null;
            }

            return Math.Clamp(confidence, 0d, 1d);
        }

        public sealed class SentimentDataRow
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
    }
}
