using System.IO.Compression;
using Microsoft.ML;
using SentimentModelTrainer;

// Trains the binary sentiment model shipped with the API.
// Pipeline matches the ML.NET binary sentiment tutorial:
// FeaturizeText + SdcaLogisticRegression, evaluated on a held-out 20% split.
// The saved zip is fit only on the training split.

const string DatasetPage = "https://archive.ics.uci.edu/dataset/331/sentiment+labelled+sentences";
const string DatasetZip = "https://archive.ics.uci.edu/static/public/331/sentiment+labelled+sentences.zip";
const string DatasetZipFallback = "https://archive.ics.uci.edu/ml/machine-learning-databases/00331/sentiment%20labelled%20sentences.zip";

var outputPath = ResolveOutputPath(args);
var zooPath = Path.Combine(AppContext.BaseDirectory, "Data", "zoo_reviews.tsv");
if (!File.Exists(zooPath))
{
    Console.Error.WriteLine($"Zoo review file was not found at {zooPath}.");
    return 1;
}

string dataDirectory;
try
{
    dataDirectory = await EnsureDatasetAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    PrintDownloadHelp();
    return 2;
}

var uciRows = LoadUciFiles(dataDirectory);
var zooRows = LoadZooFile(zooPath);
if (uciRows.Count < 1000)
{
    Console.Error.WriteLine($"Expected the UCI files to contain about 3,000 sentences, but loaded {uciRows.Count}.");
    PrintDownloadHelp();
    return 2;
}

var zooText = zooRows.Select(row => row.SentimentText).ToHashSet(StringComparer.Ordinal);
var all = uciRows.Concat(zooRows).ToList();
var split = Split(all, testFraction: 0.2, seed: 1);

var ml = new MLContext(seed: 1);
var pipeline = ml.Transforms.Text.FeaturizeText(
        outputColumnName: "Features",
        inputColumnName: nameof(SentimentRow.SentimentText))
    .Append(ml.BinaryClassification.Trainers.SdcaLogisticRegression(
        labelColumnName: nameof(SentimentRow.Label),
        featureColumnName: "Features"));

var model = pipeline.Fit(ml.Data.LoadFromEnumerable(split.Train));
var predictions = model.Transform(ml.Data.LoadFromEnumerable(split.Test));
var metrics = ml.BinaryClassification.Evaluate(predictions, labelColumnName: nameof(SentimentRow.Label));

var zooTest = split.Test.Where(row => zooText.Contains(row.SentimentText)).ToList();
var zooHits = CountHits(ml, model, zooTest);

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
ml.Model.Save(model, ml.Data.LoadFromEnumerable(split.Train).Schema, outputPath);
var size = new FileInfo(outputPath).Length;

var report = string.Join(Environment.NewLine, new[]
{
    "WildNest binary sentiment model",
    $"Saved: {outputPath}",
    $"Bytes: {size}",
    "Pipeline: FeaturizeText + SdcaLogisticRegression (ML.NET binary sentiment tutorial).",
    "Split: stratified 20% hold-out, seed 1. The saved model was not trained on the hold-out rows.",
    $"Train rows: {split.Train.Count} (positive {split.Train.Count(row => row.Label)}, negative {split.Train.Count(row => !row.Label)})",
    $"Test rows: {split.Test.Count} (positive {split.Test.Count(row => row.Label)}, negative {split.Test.Count(row => !row.Label)})",
    $"UCI rows: {uciRows.Count}",
    $"Zoo rows: {zooRows.Count} (English and Bosnian). Zoo rows in the hold-out: {zooTest.Count}.",
    $"Hold-out accuracy: {metrics.Accuracy:P2}",
    $"Hold-out AUC: {metrics.AreaUnderRocCurve:P2}",
    $"Hold-out F1: {metrics.F1Score:P2}",
    $"Positive precision: {metrics.PositivePrecision:P2}",
    $"Positive recall: {metrics.PositiveRecall:P2}",
    $"Negative precision: {metrics.NegativePrecision:P2}",
    $"Negative recall: {metrics.NegativeRecall:P2}",
    $"Zoo hold-out accuracy: {(zooTest.Count == 0 ? "n/a" : $"{(double)zooHits / zooTest.Count:P2} ({zooHits}/{zooTest.Count})")}",
    $"Source: UCI Sentiment Labelled Sentences, {DatasetPage}",
    "Citation: Dimitrios Kotzias, Misha Denil, Nando de Freitas, and Padhraic Smyth. 2015. From Group to Individual Labels using Deep Features. KDD 2015.",
    "Files: amazon_cells_labelled.txt, imdb_labelled.txt, yelp_labelled.txt (sentence, tab, 0 or 1).",
    "Licence: Creative Commons Attribution 4.0 International (CC BY 4.0), stated on the UCI dataset page. DOI 10.24432/C57604. Attribute Kotzias et al., KDD 2015."
});

var metricsPath = Path.ChangeExtension(outputPath, ".metrics.txt");
await File.WriteAllTextAsync(metricsPath, report + Environment.NewLine);
Console.WriteLine(report);
return 0;

static string ResolveOutputPath(string[] args)
{
    for (var index = 0; index < args.Length - 1; index++)
    {
        if (args[index] == "--output")
        {
            return Path.GetFullPath(args[index + 1]);
        }
    }

    return Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..",
        "eCommerce.Services", "Sentiment", "Models", "review-sentiment.zip"));
}

static async Task<string> EnsureDatasetAsync()
{
    var dataRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data"));
    var existing = FindLabelFile(dataRoot);
    if (existing != null)
    {
        return Path.GetDirectoryName(existing)!;
    }

    Directory.CreateDirectory(dataRoot);
    var zipPath = Path.Combine(dataRoot, "sentiment_labelled_sentences.zip");
    using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
    Exception? last = null;
    foreach (var url in new[] { DatasetZip, DatasetZipFallback })
    {
        try
        {
            Console.WriteLine($"Downloading {url}");
            await using var stream = await http.GetStreamAsync(url);
            await using var file = File.Create(zipPath);
            await stream.CopyToAsync(file);
            last = null;
            break;
        }
        catch (Exception ex)
        {
            last = ex;
            Console.WriteLine($"Download failed: {ex.Message}");
        }
    }

    if (last != null || !File.Exists(zipPath))
    {
        throw new InvalidOperationException("The UCI sentiment dataset could not be downloaded.");
    }

    ZipFile.ExtractToDirectory(zipPath, dataRoot, overwriteFiles: true);
    var found = FindLabelFile(dataRoot);
    if (found == null)
    {
        throw new InvalidOperationException("The downloaded archive did not contain yelp_labelled.txt.");
    }

    return Path.GetDirectoryName(found)!;
}

static string? FindLabelFile(string root)
{
    if (!Directory.Exists(root))
    {
        return null;
    }

    return Directory.EnumerateFiles(root, "yelp_labelled.txt", SearchOption.AllDirectories).FirstOrDefault();
}

static List<SentimentRow> LoadUciFiles(string directory)
{
    var names = new[] { "amazon_cells_labelled.txt", "imdb_labelled.txt", "yelp_labelled.txt" };
    var rows = new List<SentimentRow>();
    foreach (var name in names)
    {
        var path = Path.Combine(directory, name);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Missing {path}.");
        }

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var tab = line.LastIndexOf('\t');
            if (tab <= 0 || tab == line.Length - 1)
            {
                continue;
            }

            var labelText = line[(tab + 1)..].Trim();
            if (labelText != "0" && labelText != "1")
            {
                continue;
            }

            rows.Add(new SentimentRow
            {
                SentimentText = line[..tab].Trim(),
                Label = labelText == "1"
            });
        }
    }

    return rows;
}

static List<SentimentRow> LoadZooFile(string path)
{
    var rows = new List<SentimentRow>();
    foreach (var line in File.ReadLines(path))
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            continue;
        }

        var tab = line.LastIndexOf('\t');
        if (tab <= 0)
        {
            throw new InvalidOperationException($"Zoo review row is not 'text<tab>0|1': {line}");
        }

        var labelText = line[(tab + 1)..].Trim();
        if (labelText != "0" && labelText != "1")
        {
            throw new InvalidOperationException($"Zoo review label must be 0 or 1: {line}");
        }

        rows.Add(new SentimentRow
        {
            SentimentText = line[..tab].Trim(),
            Label = labelText == "1"
        });
    }

    if (rows.Count < 40)
    {
        throw new InvalidOperationException("The zoo review file needs at least a few dozen labeled sentences.");
    }

    return rows;
}

static SplitRows Split(List<SentimentRow> rows, double testFraction, int seed)
{
    var random = new Random(seed);
    var train = new List<SentimentRow>();
    var test = new List<SentimentRow>();
    foreach (var group in rows.GroupBy(row => row.Label))
    {
        var items = group.ToList();
        for (var index = items.Count - 1; index > 0; index--)
        {
            var swap = random.Next(index + 1);
            (items[index], items[swap]) = (items[swap], items[index]);
        }

        var testCount = (int)Math.Round(items.Count * testFraction, MidpointRounding.AwayFromZero);
        if (testCount < 1)
        {
            testCount = 1;
        }

        if (testCount >= items.Count)
        {
            testCount = Math.Max(1, items.Count / 5);
        }

        test.AddRange(items.Take(testCount));
        train.AddRange(items.Skip(testCount));
    }

    return new SplitRows(train, test);
}

static int CountHits(MLContext ml, ITransformer model, List<SentimentRow> rows)
{
    if (rows.Count == 0)
    {
        return 0;
    }

    var engine = ml.Model.CreatePredictionEngine<SentimentRow, SentimentPrediction>(model);
    var hits = 0;
    foreach (var row in rows)
    {
        var predicted = engine.Predict(row);
        if (predicted.PredictedLabel == row.Label)
        {
            hits++;
        }
    }

    engine.Dispose();
    return hits;
}

static void PrintDownloadHelp()
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("Download the UCI Sentiment Labelled Sentences archive:");
    Console.Error.WriteLine($"  {DatasetZip}");
    Console.Error.WriteLine("Extract it so these three files sit in one folder:");
    Console.Error.WriteLine("  eCommerce/tools/SentimentModelTrainer/data/sentiment labelled sentences/yelp_labelled.txt");
    Console.Error.WriteLine("  eCommerce/tools/SentimentModelTrainer/data/sentiment labelled sentences/amazon_cells_labelled.txt");
    Console.Error.WriteLine("  eCommerce/tools/SentimentModelTrainer/data/sentiment labelled sentences/imdb_labelled.txt");
    Console.Error.WriteLine("Then run:");
    Console.Error.WriteLine("  dotnet run --project eCommerce/tools/SentimentModelTrainer");
    Console.Error.WriteLine($"Dataset page: {DatasetPage}");
}

sealed record SplitRows(List<SentimentRow> Train, List<SentimentRow> Test);
