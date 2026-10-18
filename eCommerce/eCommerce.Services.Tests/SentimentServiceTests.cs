using eCommerce.Model;
using eCommerce.Services.Sentiment;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace eCommerce.Services.Tests
{
    public sealed class SentimentServiceFixture : IDisposable
    {
        public SentimentServiceFixture()
        {
            ModelPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "eCommerce.Services", "Sentiment", "Models", "review-sentiment.zip"));
            Service = new SentimentService(NullLogger<SentimentService>.Instance, ModelPath);
            Ready = Service.EnsureReadyAsync().GetAwaiter().GetResult();
        }

        public string ModelPath { get; }

        public SentimentService Service { get; }

        public bool Ready { get; }

        public void Dispose()
        {
            Service.Dispose();
        }
    }

    public class SentimentServiceTests : IClassFixture<SentimentServiceFixture>
    {
        private const string EnglishPositive = "This was an amazing and wonderful experience and I would recommend it.";
        private const string EnglishNegative = "The guide was rude and the tour was terrible and disappointing.";
        private const string BosnianPositive = "Predivno iskustvo, osoblje je bilo ljubazno i djeca su bila oduševljena.";

        private readonly SentimentServiceFixture _fixture;

        public SentimentServiceTests(SentimentServiceFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void ShippedModel_LoadsWithoutTraining()
        {
            Assert.True(File.Exists(_fixture.ModelPath), $"Missing shipped model at {_fixture.ModelPath}.");
            Assert.True(_fixture.Ready);
            Assert.True(_fixture.Service.IsReady);
            Assert.True(_fixture.Service.LastReport?.LoadedFromFile);
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
            Assert.InRange(result.Score ?? -1, 0, 1);
        }

        [Fact]
        public void Classify_BosnianSample_IsPositive()
        {
            var result = Classify(BosnianPositive);
            Assert.Equal(SentimentLabels.Positive, result.Label);
        }

        [Fact]
        public void Classify_DoesNotReturnNeutral()
        {
            var result = Classify(EnglishPositive);
            Assert.NotEqual("Neutral", result.Label);
            Assert.Contains(result.Label, SentimentLabels.All);
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

        [Fact]
        public async Task MissingModel_DoesNotTrainAndLeavesTextUnlabeled()
        {
            var directory = Path.Combine(Path.GetTempPath(), "wildnest-sentiment", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "review-sentiment.zip");
                using var service = new SentimentService(NullLogger<SentimentService>.Instance, path);
                var ready = await service.EnsureReadyAsync();
                Assert.False(ready);
                Assert.False(service.IsReady);
                Assert.False(File.Exists(path));
                var result = service.Classify("This was wonderful and I loved it.");
                Assert.Null(result.Label);
                Assert.Null(result.Score);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public async Task InvalidModel_DoesNotTrainAndLeavesTextUnlabeled()
        {
            var directory = Path.Combine(Path.GetTempPath(), "wildnest-sentiment", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var path = Path.Combine(directory, "review-sentiment.zip");
                await File.WriteAllTextAsync(path, "this is not a model");
                using var service = new SentimentService(NullLogger<SentimentService>.Instance, path);
                var ready = await service.EnsureReadyAsync();
                Assert.False(ready);
                Assert.False(service.IsReady);
                var result = service.Classify("This was terrible and rude.");
                Assert.Null(result.Label);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private SentimentClassification Classify(string text)
        {
            Assert.True(_fixture.Ready);
            return _fixture.Service.Classify(text);
        }
    }
}
