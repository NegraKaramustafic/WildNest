using eCommerce.Services;

namespace eCommerce.WebAPI.Services
{
    public sealed class ScheduleReminderHostedService : BackgroundService
    {
        private readonly ILogger<ScheduleReminderHostedService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ReminderSettings _settings;

        public ScheduleReminderHostedService(
            ILogger<ScheduleReminderHostedService> logger,
            IServiceScopeFactory scopeFactory,
            ReminderSettings settings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _settings = settings;
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
                _logger.LogError(ex, "Upcoming schedule reminder run failed. The next run will try again.");
            }

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_settings.CheckIntervalMinutes));
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
                    _logger.LogError(ex, "Upcoming schedule reminder run failed. The next run will try again.");
                }
            }
        }

        private async Task RunOnceAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IScheduleReminderService>();
            var sent = await service.SendDueRemindersAsync(stoppingToken);
            if (sent > 0)
            {
                _logger.LogInformation("Published {Count} upcoming schedule reminders.", sent);
            }
        }
    }
}
