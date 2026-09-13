using RabbitMQ.Client;

namespace eCommerce.Services
{
    /// <summary>
    /// Declares the notification queue, its dead-letter exchange, and
    /// wildnest.notifications.dead so a nack-without-requeue is retained.
    /// Publisher and subscriber must use the same topology.
    /// </summary>
    public static class RabbitMqNotificationTopology
    {
        public static async Task EnsureDeclaredAsync(
            IChannel channel,
            RabbitMqSettings settings,
            CancellationToken cancellationToken = default)
        {
            await channel.ExchangeDeclareAsync(
                exchange: settings.DeadLetterExchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: settings.DeadQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: settings.DeadQueueName,
                exchange: settings.DeadLetterExchange,
                routingKey: settings.DeadQueueName,
                cancellationToken: cancellationToken);

            var mainQueueArgs = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = settings.DeadLetterExchange,
                ["x-dead-letter-routing-key"] = settings.DeadQueueName
            };

            await channel.QueueDeclareAsync(
                queue: settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: mainQueueArgs,
                cancellationToken: cancellationToken);
        }
    }
}
