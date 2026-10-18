using eCommerce.Services;
using eCommerce.Services.Database;
using eCommerce.Services.Recommendation;

namespace eCommerce.WebAPI.Services
{
    /// <summary>
    /// Trains the reservation matrix-factorization model off the request path.
    /// Retrains on a timer and after enough new confirmed or completed reservations.
    /// </summary>
    public sealed class RecommendationTrainingHostedService : BackgroundService
    {
        private readonly ILogger<RecommendationTrainingHostedService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RecommendationTrainingSettings _settings;

        public RecommendationTrainingHostedService(
            ILogger<RecommendationTrainingHostedService> logger,
            IServiceScopeFactory scopeFactory,
            RecommendationTrainingSettings settings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _settings = settings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await TrainAsync(force: true, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recommendation model training failed. Suggestions will use popularity until the next attempt.");
            }

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_settings.CheckIntervalMinutes));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await TrainAsync(force: false, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Recommendation model training failed. Suggestions will use popularity until the next attempt.");
                }
            }
        }

        private async Task TrainAsync(bool force, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
            var trainer = scope.ServiceProvider.GetRequiredService<RecommendationTrainer>();
            if (!force && !await trainer.IsDueAsync(db, _settings, cancellationToken))
            {
                return;
            }

            var result = await trainer.TrainAsync(db, cancellationToken);
            _logger.LogInformation(
                "Recommendation training finished. Pairs: {Rows}. Model ready: {Ready}.",
                result.TrainingRowCount,
                result.ModelReady);
        }
    }
}
