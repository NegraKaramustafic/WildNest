using eCommerce.Model.Messages;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace eCommerce.Subscriber
{
    public sealed class NotificationEmailSender
    {
        private readonly ILogger<NotificationEmailSender> _logger;
        private readonly SmtpSettings _settings;

        public NotificationEmailSender(ILogger<NotificationEmailSender> logger, SmtpSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public async Task SendAsync(NotificationMessage message, string? email, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning(
                    "No email address for user {UserId}; skipping SMTP for {Type}.",
                    message.UserId,
                    message.Type);
                return;
            }

            if (!_settings.IsConfigured)
            {
                _logger.LogInformation(
                    "SMTP_HOST is not set. Would email {Email} [{Type}]: {Title} — {Message}",
                    email,
                    message.Type,
                    message.Title,
                    message.Message);
                return;
            }

            var mime = new MimeMessage();
            mime.From.Add(MailboxAddress.Parse(_settings.From!));
            mime.To.Add(MailboxAddress.Parse(email));
            mime.Subject = message.Title;
            mime.Body = new TextPart("plain") { Text = message.Message };

            using var client = new SmtpClient();
            var socketOptions = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await client.ConnectAsync(_settings.Host!, _settings.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.UserName))
            {
                await client.AuthenticateAsync(_settings.UserName, _settings.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Sent notification email to {Email} for {Type}.", email, message.Type);
        }
    }
}
