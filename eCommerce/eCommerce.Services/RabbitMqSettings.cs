namespace eCommerce.Services
{
    /// <summary>
    /// RabbitMQ settings sourced only from environment variables (.env / compose).
    /// Values are not read from appsettings and have no hardcoded defaults.
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
            var hostName = Require("RABBITMQ_HOSTNAME");
            var portText = Require("RABBITMQ_PORT");
            if (!int.TryParse(portText, out var port) || port <= 0)
            {
                throw new InvalidOperationException("RABBITMQ_PORT must be a positive integer from the .env file.");
            }

            return new RabbitMqSettings(
                hostName,
                port,
                Require("RABBITMQ_USERNAME"),
                Require("RABBITMQ_PASSWORD"),
                Require("RABBITMQ_QUEUENAME"));
        }

        private static string Require(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"{name} is not set. Add it to the .env file (see .env.example).");
            }

            return value.Trim();
        }
    }
}
