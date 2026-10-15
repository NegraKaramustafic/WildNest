using System.Reflection;
using System.Text;
using eCommerce.Common.Services;
using eCommerce.Model;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace eCommerce.Services.Sentiment
{
    /// <summary>
    /// Multiclass text classifier (FeaturizeText + SdcaMaximumEntropy).
    /// The committed CSV is a synthetic labeled set of English and Bosnian review sentences.
    /// PredictionEngine is not thread-safe, so scoring is serialized with a lock.
    /// </summary>
    public sealed class SentimentService : ISentimentService, IDisposable
    {
        public const string EmbeddedResourceName = "eCommerce.Services.Sentiment.review_sentiment.csv";
        public const int MaxTextLength = 2000;
        private const int HoldoutSeed = 42;
        private const double HoldoutFraction = 0.2;

        private readonly ILogger<SentimentService> _logger;
        private readonly string _modelPath;
        private readonly object _sync = new();
        private readonly SemaphoreSlim _gate = new(1, 1);
        private MLContext? _mlContext;
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

        public SentimentTrainingReport? LastReport { get; private set; }

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

                try
                {
                    var rows = LoadTrainingRows();
                    if (TryLoadSavedModel())
                    {
                        LastReport = BuildReport(rows, null, loadedFromFile: true);
                        _logger.LogInformation("Loaded sentiment model from {Path}.", _modelPath);
                        _state = 1;
                        return true;
                    }

                    var holdout = EvaluateHoldout(rows);
                    TrainAndSave(rows);
                    LastReport = BuildReport(rows, holdout, loadedFromFile: false);
                    _logger.LogInformation(
                        "Trained sentiment model on {Rows} synthetic rows ({Bosnian} with Bosnian diacritics). Hold-out accuracy {Accuracy:P1} on {Holdout} rows (positive {Positive:P0}, neutral {Neutral:P0}, negative {Negative:P0}). Saved to {Path}.",
                        rows.Count,
                        LastReport.BosnianRows,
                        holdout.Accuracy,
                        holdout.Count,
                        holdout.PositiveAccuracy,
                        holdout.NeutralAccuracy,
                        holdout.NegativeAccuracy,
                        _modelPath);
                    _state = 1;
                    return true;
                }
                catch (Exception ex)
                {
                    _state = 2;
                    _logger.LogError(
                        ex,
                        "Sentiment model training failed. Reviews will be saved without a label.");
                    return false;
                }
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
                if (_engine == null || _mlContext == null)
                {
                    return SentimentClassification.Unlabeled;
                }

                var prediction = _engine.Predict(new SentimentDataRow
                {
                    // The training pipeline keeps Label as an input column. Scoring uses Features only.
                    Label = SentimentLabels.Positive,
                    Text = normalized
                });

                var label = SentimentLabels.Canonical(prediction.PredictedLabel);
                if (label == null)
                {
                    _logger.LogWarning(
                        "Sentiment model returned an unexpected label {Label}.",
                        prediction.PredictedLabel);
                    return SentimentClassification.Unlabeled;
                }

                return new SentimentClassification(label, Confidence(prediction.Score));
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _engine?.Dispose();
                _engine = null;
                _mlContext = null;
            }

            _gate.Dispose();
        }

        private bool TryLoadSavedModel()
        {
            if (!File.Exists(_modelPath))
            {
                return false;
            }

            try
            {
                var ml = new MLContext(HoldoutSeed);
                var model = ml.Model.Load(_modelPath, out _);
                ReplaceEngine(ml, model);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Saved sentiment model at {Path} could not be loaded. A new model will be trained.",
                    _modelPath);
                return false;
            }
        }

        private void TrainAndSave(IReadOnlyList<SentimentDataRow> rows)
        {
            var ml = new MLContext(HoldoutSeed);
            var model = Fit(ml, Shuffle(rows, HoldoutSeed));
            var directory = Path.GetDirectoryName(_modelPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temporary = _modelPath + ".tmp";
            ml.Model.Save(model, ml.Data.LoadFromEnumerable(rows).Schema, temporary);
            File.Move(temporary, _modelPath, overwrite: true);
            ReplaceEngine(ml, model);
        }

        private HoldoutResult EvaluateHoldout(IReadOnlyList<SentimentDataRow> rows)
        {
            var train = new List<SentimentDataRow>();
            var test = new List<SentimentDataRow>();
            foreach (var label in SentimentLabels.All)
            {
                var group = Shuffle(rows.Where(row => row.Label == label), HoldoutSeed);
                var testCount = (int)Math.Round(group.Count * HoldoutFraction, MidpointRounding.AwayFromZero);
                if (testCount < 1)
                {
                    testCount = 1;
                }

                if (testCount >= group.Count)
                {
                    testCount = Math.Max(1, group.Count / 5);
                }

                test.AddRange(group.Take(testCount));
                train.AddRange(group.Skip(testCount));
            }

            var ml = new MLContext(HoldoutSeed);
            var model = Fit(ml, train);
            var engine = ml.Model.CreatePredictionEngine<SentimentDataRow, SentimentPrediction>(model);
            try
            {
                var totals = SentimentLabels.All.ToDictionary(label => label, _ => 0);
                var correct = SentimentLabels.All.ToDictionary(label => label, _ => 0);
                foreach (var row in test)
                {
                    var predicted = engine.Predict(new SentimentDataRow
                    {
                        Label = SentimentLabels.Positive,
                        Text = row.Text
                    });
                    totals[row.Label]++;
                    if (string.Equals(SentimentLabels.Canonical(predicted.PredictedLabel), row.Label, StringComparison.Ordinal))
                    {
                        correct[row.Label]++;
                    }
                }

                var total = totals.Values.Sum();
                var hits = correct.Values.Sum();
                return new HoldoutResult(
                    total,
                    total == 0 ? 0 : (double)hits / total,
                    Rate(correct[SentimentLabels.Positive], totals[SentimentLabels.Positive]),
                    Rate(correct[SentimentLabels.Neutral], totals[SentimentLabels.Neutral]),
                    Rate(correct[SentimentLabels.Negative], totals[SentimentLabels.Negative]));
            }
            finally
            {
                engine.Dispose();
            }
        }

        private static ITransformer Fit(MLContext ml, IReadOnlyList<SentimentDataRow> rows)
        {
            var data = ml.Data.LoadFromEnumerable(rows);
            var pipeline = ml.Transforms.Conversion.MapValueToKey("Label", nameof(SentimentDataRow.Label))
                .Append(ml.Transforms.Text.FeaturizeText("Features", nameof(SentimentDataRow.Text)))
                .Append(ml.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                    labelColumnName: "Label",
                    featureColumnName: "Features"))
                .Append(ml.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
            return pipeline.Fit(data);
        }

        private void ReplaceEngine(MLContext ml, ITransformer model)
        {
            var engine = ml.Model.CreatePredictionEngine<SentimentDataRow, SentimentPrediction>(model);
            lock (_sync)
            {
                _engine?.Dispose();
                _engine = engine;
                _mlContext = ml;
            }
        }

        private static SentimentTrainingReport BuildReport(
            IReadOnlyList<SentimentDataRow> rows,
            HoldoutResult? holdout,
            bool loadedFromFile)
        {
            return new SentimentTrainingReport
            {
                RowCount = rows.Count,
                PositiveRows = rows.Count(row => row.Label == SentimentLabels.Positive),
                NeutralRows = rows.Count(row => row.Label == SentimentLabels.Neutral),
                NegativeRows = rows.Count(row => row.Label == SentimentLabels.Negative),
                BosnianRows = rows.Count(row => ContainsBosnianDiacritic(row.Text)),
                HoldoutCount = holdout?.Count ?? 0,
                HoldoutAccuracy = holdout?.Accuracy,
                PositiveAccuracy = holdout?.PositiveAccuracy,
                NeutralAccuracy = holdout?.NeutralAccuracy,
                NegativeAccuracy = holdout?.NegativeAccuracy,
                LoadedFromFile = loadedFromFile
            };
        }

        private static List<SentimentDataRow> LoadTrainingRows()
        {
            var assembly = typeof(SentimentService).Assembly;
            using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName)
                ?? throw new InvalidOperationException(
                    "The sentiment training CSV is not embedded in the services assembly.");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var header = reader.ReadLine();
            if (header == null || !header.Trim().Equals("Label,Text", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The sentiment training CSV must start with the header Label,Text.");
            }

            var rows = new List<SentimentDataRow>();
            var lineNumber = 1;
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parsed = ParseLine(line, lineNumber);
                rows.Add(parsed);
            }

            if (rows.Count < 150)
            {
                throw new InvalidOperationException("The sentiment training CSV does not contain enough labeled rows.");
            }

            return rows;
        }

        private static SentimentDataRow ParseLine(string line, int lineNumber)
        {
            var comma = line.IndexOf(',');
            if (comma <= 0)
            {
                throw new InvalidOperationException($"Sentiment training row {lineNumber} is not a Label,Text pair.");
            }

            var label = SentimentLabels.Canonical(line[..comma]);
            if (label == null)
            {
                throw new InvalidOperationException($"Sentiment training row {lineNumber} has an unknown label.");
            }

            var text = line[(comma + 1)..].Trim();
            if (text.Length >= 2 && text[0] == '"' && text[^1] == '"')
            {
                text = text[1..^1].Replace("\"\"", "\"", StringComparison.Ordinal);
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException($"Sentiment training row {lineNumber} has no text.");
            }

            return new SentimentDataRow { Label = label, Text = text };
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

        private static List<SentimentDataRow> Shuffle(IEnumerable<SentimentDataRow> rows, int seed)
        {
            var list = rows.ToList();
            var random = new Random(seed);
            for (var index = list.Count - 1; index > 0; index--)
            {
                var swap = random.Next(index + 1);
                (list[index], list[swap]) = (list[swap], list[index]);
            }

            return list;
        }

        private static double Confidence(float[]? scores)
        {
            if (scores == null || scores.Length == 0)
            {
                return 0;
            }

            double sum = 0;
            var nonNegative = true;
            foreach (var score in scores)
            {
                if (score < 0)
                {
                    nonNegative = false;
                }

                sum += score;
            }

            double peak;
            if (nonNegative && sum > 0.98 && sum < 1.02)
            {
                peak = scores.Max();
            }
            else
            {
                var max = scores.Max();
                double expSum = 0;
                double expPeak = 0;
                foreach (var score in scores)
                {
                    var exp = Math.Exp(score - max);
                    expSum += exp;
                    if (exp > expPeak)
                    {
                        expPeak = exp;
                    }
                }

                peak = expSum <= 0 ? 0 : expPeak / expSum;
            }

            if (double.IsNaN(peak) || double.IsInfinity(peak))
            {
                return 0;
            }

            return Math.Clamp(peak, 0, 1);
        }

        private static double Rate(int hits, int total)
        {
            return total == 0 ? 0 : (double)hits / total;
        }

        private static bool ContainsBosnianDiacritic(string text)
        {
            foreach (var character in text)
            {
                if ("čćšžđČĆŠŽĐ".Contains(character))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class SentimentDataRow
        {
            public string Label { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
        }

        private sealed class SentimentPrediction
        {
            [ColumnName("PredictedLabel")]
            public string PredictedLabel { get; set; } = string.Empty;

            public float[]? Score { get; set; }
        }

        private sealed record HoldoutResult(
            int Count,
            double Accuracy,
            double PositiveAccuracy,
            double NeutralAccuracy,
            double NegativeAccuracy);
    }
}
