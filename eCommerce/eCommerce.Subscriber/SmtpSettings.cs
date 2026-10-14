using eCommerce.Common.Services;

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
            var host = EnvVariables.Optional("SMTP_HOST");
            if (host == null)
            {
                return new SmtpSettings(null, 0, null, null, null, true);
            }

            var portText = EnvVariables.Optional("SMTP_PORT");
            if (portText == null || !int.TryParse(portText, out var port) || port <= 0)
            {
                throw new InvalidOperationException("SMTP_PORT must be a positive integer when SMTP_HOST is set.");
            }

            var from = EnvVariables.Optional("SMTP_FROM");
            if (from == null)
            {
                throw new InvalidOperationException("SMTP_FROM must be set when SMTP_HOST is set.");
            }

            var enableSslText = EnvVariables.Optional("SMTP_ENABLE_SSL");
            var enableSsl = !string.Equals(enableSslText, "false", StringComparison.OrdinalIgnoreCase);

            return new SmtpSettings(
                host,
                port,
                EnvVariables.Optional("SMTP_USERNAME"),
                EnvVariables.Optional("SMTP_PASSWORD"),
                from,
                enableSsl);
        }
    }
}
