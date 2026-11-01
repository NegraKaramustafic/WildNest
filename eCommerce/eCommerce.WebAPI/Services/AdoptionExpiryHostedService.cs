using eCommerce.Services;

namespace eCommerce.WebAPI.Services
{
    public sealed class AdoptionExpiryHostedService : BackgroundService
    {
        private readonly ILogger<AdoptionExpiryHostedService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public AdoptionExpiryHostedService(
            ILogger<AdoptionExpiryHostedService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Adoption expiry failed. The next run will try again.");
            }

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Adoption expiry failed. The next run will try again.");
                }
            }
        }

        private async Task RunOnceAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IAdoptionTermService>();
            var expired = await service.ExpireElapsedAsync(stoppingToken);
            if (expired > 0)
            {
                _logger.LogInformation("Marked {Count} adoptions expired.", expired);
            }
        }
    }
}
