using eCommerce.Common.Services;

namespace eCommerce.Services
{
    /// <summary>
    /// RabbitMQ settings sourced only from environment variables (.env / compose).
    /// Bind once at startup via <see cref="FromEnvironment"/> and inject the instance.
    /// </summary>
    public sealed class RabbitMqSettings
    {
        public string HostName { get; }
        public int Port { get; }
        public string UserName { get; }
        public string Password { get; }
        public string QueueName { get; }
        public string DeadQueueName => $"{QueueName}.dead";
        public string DeadLetterExchange => $"{QueueName}.dlx";

        internal static RabbitMqSettings Create(string hostName, int port, string userName, string password, string queueName)
        {
            return new RabbitMqSettings(hostName, port, userName, password, queueName);
        }

        private RabbitMqSettings(string hostName, int port, string userName, string password, string queueName)
        {
            HostName = hostName;
            Port = port;
            UserName = userName;
            Password = password;
            QueueName = queueName;
        }

        public static RabbitMqSettings FromEnvironment()
        {
            var hostName = EnvVariables.Require("RABBITMQ_HOSTNAME");
            var portText = EnvVariables.Require("RABBITMQ_PORT");
            if (!int.TryParse(portText, out var port) || port <= 0)
            {
                throw new InvalidOperationException("RABBITMQ_PORT must be a positive integer from the .env file.");
            }

            return new RabbitMqSettings(
                hostName,
                port,
                EnvVariables.Require("RABBITMQ_USERNAME"),
                EnvVariables.Require("RABBITMQ_PASSWORD"),
                EnvVariables.Require("RABBITMQ_QUEUENAME"));
        }
    }
}
