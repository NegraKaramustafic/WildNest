using System.Text;
using System.Text.Json;
using eCommerce.Model.Messages;
using eCommerce.Services;
using eCommerce.Services.Database;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace eCommerce.Subscriber
{
    /// <summary>
    /// Separate-process consumer. Lives in eCommerce.Subscriber, not WebAPI.
    /// Uses AsyncEventingBasicConsumer as required by the course appendix.
    /// </summary>
    public sealed class NotificationConsumer : BackgroundService
    {
        private static readonly int[] RetryDelaysMs = [1000, 2000, 4000, 8000];

        private readonly ILogger<NotificationConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly NotificationEmailSender _emailSender;
        private readonly RabbitMqSettings _settings;

        public NotificationConsumer(
            ILogger<NotificationConsumer> logger,
            IServiceScopeFactory scopeFactory,
            NotificationEmailSender emailSender,
            RabbitMqSettings settings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _emailSender = emailSender;
            _settings = settings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var reconnectDelay = TimeSpan.FromSeconds(1);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConsumeUntilDisconnectedAsync(stoppingToken);
                    reconnectDelay = TimeSpan.FromSeconds(1);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Notification consumer is stopping.");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Notification consumer lost its RabbitMQ connection or failed to start. Reconnecting in {DelaySeconds}s. The consumer is still running.",
                        reconnectDelay.TotalSeconds);

                    try
                    {
                        await Task.Delay(reconnectDelay, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Notification consumer is stopping during reconnect delay.");
                        return;
                    }

                    var nextSeconds = Math.Min(reconnectDelay.TotalSeconds * 2, 8);
                    reconnectDelay = TimeSpan.FromSeconds(nextSeconds);
                }
            }
        }

        private async Task ConsumeUntilDisconnectedAsync(CancellationToken stoppingToken)
        {
            var settings = _settings;
            var factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password
            };

            await using var connection = await factory.CreateConnectionAsync(stoppingToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await RabbitMqNotificationTopology.EnsureDeclaredAsync(channel, settings, stoppingToken);

            // Fair dispatch: one unacked message at a time so retries stay predictable.
            await channel.BasicQosAsync(0, 1, false, stoppingToken);

            var disconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            connection.ConnectionShutdownAsync += (_, args) =>
            {
                _logger.LogWarning(
                    "RabbitMQ connection shut down (reply {Code}: {Text}).",
                    args.ReplyCode,
                    args.ReplyText);
                disconnected.TrySetException(new InvalidOperationException(
                    $"RabbitMQ connection shut down: {args.ReplyText}"));
                return Task.CompletedTask;
            };

            channel.ChannelShutdownAsync += (_, args) =>
            {
                _logger.LogWarning(
                    "RabbitMQ channel shut down (reply {Code}: {Text}).",
                    args.ReplyCode,
                    args.ReplyText);
                disconnected.TrySetException(new InvalidOperationException(
                    $"RabbitMQ channel shut down: {args.ReplyText}"));
                return Task.CompletedTask;
            };

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                await HandleMessageWithRetryAsync(channel, ea, settings.DeadQueueName, stoppingToken);
            };

            await channel.BasicConsumeAsync(
                queue: settings.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "Notification consumer is listening on queue {Queue} at {Host}:{Port} using AsyncEventingBasicConsumer. Failed messages after retries go to {DeadQueue}.",
                settings.QueueName,
                settings.HostName,
                settings.Port,
                settings.DeadQueueName);

            using var registration = stoppingToken.Register(() => disconnected.TrySetCanceled(stoppingToken));
            await disconnected.Task;
        }

        private async Task HandleMessageWithRetryAsync(
            IChannel channel,
            BasicDeliverEventArgs ea,
            string deadQueueName,
            CancellationToken stoppingToken)
        {
            var maxAttempts = RetryDelaysMs.Length + 1;
            NotificationMessage? message = null;
            Exception? lastError = null;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    message ??= ReadMessage(ea.Body.ToArray());
                    await InsertNotificationIfNewAsync(message, stoppingToken);
                    lastError = null;
                    break;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Message processing cancelled while shutting down. Delivery tag {Tag} will be requeued.", ea.DeliveryTag);
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None);
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    _logger.LogError(
                        ex,
                        "Failed to store notification message (attempt {Attempt}/{MaxAttempts}). Delivery tag {Tag}.",
                        attempt + 1,
                        maxAttempts,
                        ea.DeliveryTag);

                    if (attempt < RetryDelaysMs.Length)
                    {
                        var delayMs = RetryDelaysMs[attempt];
                        _logger.LogWarning("Retrying notification message in {DelayMs}ms.", delayMs);
                        await Task.Delay(delayMs, stoppingToken);
                    }
                }
            }

            if (lastError != null || message == null)
            {
                _logger.LogError(
                    lastError,
                    "Giving up on notification message after {Attempts} attempts (backoff 1s → 2s → 4s → 8s). Nacking without requeue so RabbitMQ dead-letters it to {DeadQueue}. Delivery tag {Tag}.",
                    maxAttempts,
                    deadQueueName,
                    ea.DeliveryTag);

                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                return;
            }

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    await SendNotificationEmailAsync(message, stoppingToken);
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    _logger.LogInformation(
                        "Stored {Type} notification {MessageId} for user {UserId} (related {RelatedType} #{RelatedId}).",
                        message.Type,
                        message.MessageId,
                        message.UserId,
                        message.RelatedEntityType,
                        message.RelatedEntityId);
                    return;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Email sending cancelled while shutting down. Delivery tag {Tag} will be requeued.", ea.DeliveryTag);
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None);
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    _logger.LogError(
                        ex,
                        "Failed to send notification email (attempt {Attempt}/{MaxAttempts}). MessageId {MessageId}, user {UserId}.",
                        attempt + 1,
                        maxAttempts,
                        message.MessageId,
                        message.UserId);

                    if (attempt < RetryDelaysMs.Length)
                    {
                        var delayMs = RetryDelaysMs[attempt];
                        _logger.LogWarning("Retrying notification email in {DelayMs}ms.", delayMs);
                        await Task.Delay(delayMs, stoppingToken);
                    }
                }
            }

            _logger.LogError(
                lastError,
                "Notification {MessageId} for user {UserId} is already stored, but the email could not be sent after {Attempts} attempts. Acknowledging the message so it is not inserted again.",
                message.MessageId,
                message.UserId,
                maxAttempts);

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
        }

        private static NotificationMessage ReadMessage(byte[] body)
        {
            var json = Encoding.UTF8.GetString(body);
            var message = JsonSerializer.Deserialize<NotificationMessage>(json, NotificationMessage.JsonOptions);
            if (message == null || message.UserId <= 0)
            {
                throw new InvalidOperationException("Notification message is missing or has an invalid UserId.");
            }

            if (message.MessageId == Guid.Empty)
            {
                throw new InvalidOperationException("Notification message is missing MessageId.");
            }

            return message;
        }

        private async Task InsertNotificationIfNewAsync(NotificationMessage message, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();

            var alreadyStored = await dbContext.Notifications
                .AsNoTracking()
                .AnyAsync(n => n.MessageId == message.MessageId, cancellationToken);
            if (alreadyStored)
            {
                _logger.LogInformation(
                    "Notification {MessageId} for user {UserId} already exists; skipping insert.",
                    message.MessageId,
                    message.UserId);
                return;
            }

            dbContext.Notifications.Add(new Notification
            {
                MessageId = message.MessageId,
                UserId = message.UserId,
                Title = message.Title,
                Message = message.Message,
                Type = message.Type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedEntityType = message.RelatedEntityType,
                RelatedEntityId = message.RelatedEntityId
            });

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _logger.LogInformation(
                    ex,
                    "Notification {MessageId} for user {UserId} was inserted concurrently; treating it as already processed.",
                    message.MessageId,
                    message.UserId);
            }
        }

        private async Task SendNotificationEmailAsync(NotificationMessage message, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ECommerceDbContext>();
            var email = message.Email
                ?? await dbContext.Users
                    .Where(u => u.Id == message.UserId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync(cancellationToken);

            await _emailSender.SendAsync(message, email, cancellationToken);
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            for (Exception? current = exception; current != null; current = current.InnerException)
            {
                if (current is SqlException sql && sql.Number is 2601 or 2627)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
