using eCommerce.Model;
using eCommerce.Services.Sentiment;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace eCommerce.Services.Tests
{
    public sealed class SentimentServiceFixture : IDisposable
    {
        public SentimentServiceFixture()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), "wildnest-sentiment", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
            Service = new SentimentService(
                NullLogger<SentimentService>.Instance,
                Path.Combine(DirectoryPath, "review-sentiment.zip"));
            Ready = Service.EnsureReadyAsync().GetAwaiter().GetResult();
        }

        public string DirectoryPath { get; }

        public SentimentService Service { get; }

        public bool Ready { get; }

        public void Dispose()
        {
            Service.Dispose();
            try
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    public class SentimentServiceTests : IClassFixture<SentimentServiceFixture>
    {
        private const string EnglishPositive = "This was an amazing and wonderful experience and I would recommend it.";
        private const string EnglishNegative = "The guide was rude and the tour was terrible and disappointing.";
        private const string EnglishSeed = "Amazing experience! The kids were thrilled, staff were very kind.";
        private const string BosnianPositive = "Predivno iskustvo, osoblje je bilo ljubazno i djeca su bila oduševljena.";

        private readonly SentimentServiceFixture _fixture;
        private readonly ITestOutputHelper _output;

        public SentimentServiceTests(SentimentServiceFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            var report = fixture.Service.LastReport;
            _output.WriteLine(
                "rows={0} positive={1} neutral={2} negative={3} bosnian={4} holdout={5} accuracy={6:P1} pos={7:P0} neu={8:P0} neg={9:P0}",
                report?.RowCount,
                report?.PositiveRows,
                report?.NeutralRows,
                report?.NegativeRows,
                report?.BosnianRows,
                report?.HoldoutCount,
                report?.HoldoutAccuracy,
                report?.PositiveAccuracy,
                report?.NeutralAccuracy,
                report?.NegativeAccuracy);
        }

        [Fact]
        public void TrainingSet_IsBalancedSyntheticData()
        {
            var report = _fixture.Service.LastReport;
            Assert.NotNull(report);
            Assert.InRange(report.RowCount, 150, 300);
            Assert.Equal(report.PositiveRows, report.NeutralRows);
            Assert.Equal(report.NeutralRows, report.NegativeRows);
            Assert.InRange(report.BosnianRows, 100, 160);
        }

        [Fact]
        public void HoldoutAccuracy_IsReported()
        {
            var report = _fixture.Service.LastReport;
            Assert.NotNull(report);
            Assert.NotNull(report.HoldoutAccuracy);
            Assert.True(
                report.HoldoutAccuracy >= 0.75,
                $"Hold-out accuracy was {report.HoldoutAccuracy:P1}.");
        }

        [Fact]
        public void Classify_ClearlyPositiveEnglish_IsPositive()
        {
            var result = Classify(EnglishPositive);
            Assert.Equal(SentimentLabels.Positive, result.Label);
            Assert.InRange(result.Score ?? -1, 0, 1);
        }

        [Fact]
        public void Classify_ClearlyNegativeEnglish_IsNegative()
        {
            var result = Classify(EnglishNegative);
            Assert.Equal(SentimentLabels.Negative, result.Label);
        }

        [Fact]
        public void Classify_EnglishSample_IsPositive()
        {
            var result = Classify(EnglishSeed);
            Assert.Equal(SentimentLabels.Positive, result.Label);
        }

        [Fact]
        public void Classify_BosnianSample_IsPositive()
        {
            var result = Classify(BosnianPositive);
            Assert.Equal(SentimentLabels.Positive, result.Label);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Classify_EmptyText_IsUnlabeled(string? text)
        {
            var result = _fixture.Service.Classify(text);
            Assert.Null(result.Label);
            Assert.Null(result.Score);
        }

        [Fact]
        public void Classify_VeryLongText_DoesNotThrow()
        {
            var text = string.Concat(Enumerable.Repeat(EnglishPositive + " ", 80));
            Assert.True(text.Length > SentimentService.MaxTextLength);
            var result = Classify(text);
            Assert.Equal(SentimentLabels.Positive, result.Label);
        }

        private SentimentClassification Classify(string text)
        {
            Assert.True(_fixture.Ready);
            Assert.True(_fixture.Service.IsReady);
            return _fixture.Service.Classify(text);
        }
    }
}
