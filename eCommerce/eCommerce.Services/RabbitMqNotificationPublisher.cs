using System.Text;
using System.Text.Json;
using eCommerce.Model.Messages;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace eCommerce.Services
{
    /// <summary>
    /// Singleton publisher. Reuses one RabbitMQ connection; does not open a
    /// new connection per PublishAsync call.
    /// </summary>
    public sealed class RabbitMqNotificationPublisher : INotificationPublisher, IAsyncDisposable
    {
        private readonly ILogger<RabbitMqNotificationPublisher> _logger;
        private readonly RabbitMqSettings _settings;
        private readonly ConnectionFactory _factory;
        private readonly SemaphoreSlim _connectionGate = new(1, 1);
        private IConnection? _connection;

        public RabbitMqNotificationPublisher(ILogger<RabbitMqNotificationPublisher> logger)
        {
            _logger = logger;
            _settings = RabbitMqSettings.FromEnvironment();
            _factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password
            };
        }

        public async Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                var connection = await GetOrCreateConnectionAsync(cancellationToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

                await RabbitMqNotificationTopology.EnsureDeclaredAsync(channel, _settings, cancellationToken);

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, NotificationMessage.JsonOptions));
                await channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: _settings.QueueName,
                    body: body,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "Published {Type} notification for user {UserId} to {Queue}",
                    message.Type,
                    message.UserId,
                    _settings.QueueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish {Type} notification for user {UserId}. The business operation was not rolled back.",
                    message.Type,
                    message.UserId);
            }
        }

        private async Task<IConnection> GetOrCreateConnectionAsync(CancellationToken cancellationToken)
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            await _connectionGate.WaitAsync(cancellationToken);
            try
            {
                if (_connection is { IsOpen: true })
                {
                    return _connection;
                }

                if (_connection != null)
                {
                    await DisposeConnectionAsync(_connection);
                    _connection = null;
                }

                _connection = await _factory.CreateConnectionAsync(cancellationToken);
                _logger.LogInformation(
                    "Opened singleton RabbitMQ connection to {Host}:{Port} for queue {Queue}",
                    _settings.HostName,
                    _settings.Port,
                    _settings.QueueName);
                return _connection;
            }
            finally
            {
                _connectionGate.Release();
            }
        }

        private async Task DisposeConnectionAsync(IConnection connection)
        {
            try
            {
                if (connection.IsOpen)
                {
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while closing the RabbitMQ publisher connection.");
            }

            connection.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                await DisposeConnectionAsync(_connection);
                _connection = null;
            }

            _connectionGate.Dispose();
        }
    }
}
