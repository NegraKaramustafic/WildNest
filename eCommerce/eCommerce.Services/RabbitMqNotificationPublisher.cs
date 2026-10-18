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

        public RabbitMqNotificationPublisher(ILogger<RabbitMqNotificationPublisher> logger, RabbitMqSettings settings)
        {
            _logger = logger;
            _settings = settings;
            _factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
            };
        }

        public async Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default)
        {
            var connection = await GetOrCreateConnectionAsync(cancellationToken);
            var channelOptions = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true);
            await using var channel = await connection.CreateChannelAsync(channelOptions, cancellationToken);

            await RabbitMqNotificationTopology.EnsureDeclaredAsync(channel, _settings, cancellationToken);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, NotificationMessage.JsonOptions));
            var properties = CreateProperties(message);
            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _settings.QueueName,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Published {Type} notification {MessageId} for user {UserId} to {Queue}. The broker confirmed the persistent message.",
                message.Type,
                message.MessageId,
                message.UserId,
                _settings.QueueName);
        }

        internal static BasicProperties CreateProperties(NotificationMessage message)
        {
            return new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = message.MessageId.ToString("D"),
                Type = message.Type.ToString()
            };
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
