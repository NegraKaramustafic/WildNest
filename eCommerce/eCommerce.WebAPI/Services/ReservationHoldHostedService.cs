using eCommerce.Services;

namespace eCommerce.WebAPI.Services
{
    public sealed class ReservationHoldHostedService : BackgroundService
    {
        private readonly ILogger<ReservationHoldHostedService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ReservationHoldHostedService(
            ILogger<ReservationHoldHostedService> logger,
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
                _logger.LogError(ex, "Unpaid reservation hold release failed. The next run will try again.");
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
                    _logger.LogError(ex, "Unpaid reservation hold release failed. The next run will try again.");
                }
            }
        }

        private async Task RunOnceAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IReservationHoldService>();
            var released = await service.ReleaseExpiredHoldsAsync(DateTime.UtcNow, stoppingToken);
            if (released > 0)
            {
                _logger.LogInformation("Cancelled {Count} unpaid reservation holds.", released);
            }
        }
    }
}
