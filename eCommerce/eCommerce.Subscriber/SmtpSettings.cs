namespace eCommerce.Subscriber
{
    public sealed class SmtpSettings
    {
        public string? Host { get; }
        public int Port { get; }
        public string? UserName { get; }
        public string? Password { get; }
        public string? From { get; }
        public bool EnableSsl { get; }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);

        private SmtpSettings(string? host, int port, string? userName, string? password, string? from, bool enableSsl)
        {
            Host = host;
            Port = port;
            UserName = userName;
            Password = password;
            From = from;
            EnableSsl = enableSsl;
        }

        public static SmtpSettings FromEnvironment()
        {
            var host = Environment.GetEnvironmentVariable("SMTP_HOST");
            if (string.IsNullOrWhiteSpace(host))
            {
                return new SmtpSettings(null, 0, null, null, null, true);
            }

            var portText = Environment.GetEnvironmentVariable("SMTP_PORT");
            if (string.IsNullOrWhiteSpace(portText) || !int.TryParse(portText, out var port) || port <= 0)
            {
                throw new InvalidOperationException("SMTP_PORT must be a positive integer when SMTP_HOST is set.");
            }

            var from = Environment.GetEnvironmentVariable("SMTP_FROM");
            if (string.IsNullOrWhiteSpace(from))
            {
                throw new InvalidOperationException("SMTP_FROM must be set when SMTP_HOST is set.");
            }

            var enableSslText = Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL");
            var enableSsl = !string.Equals(enableSslText, "false", StringComparison.OrdinalIgnoreCase);

            return new SmtpSettings(
                host.Trim(),
                port,
                Environment.GetEnvironmentVariable("SMTP_USERNAME"),
                Environment.GetEnvironmentVariable("SMTP_PASSWORD"),
                from.Trim(),
                enableSsl);
        }
    }
}
