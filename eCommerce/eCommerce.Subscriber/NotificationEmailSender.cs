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
            var subject = string.IsNullOrWhiteSpace(message.EmailSubject) ? message.Title : message.EmailSubject;
            var body = string.IsNullOrWhiteSpace(message.EmailBody) ? message.Message : message.EmailBody;
            var sensitive = !string.IsNullOrWhiteSpace(message.EmailBody);

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
                if (sensitive)
                {
                    _logger.LogInformation(
                        "SMTP_HOST is not set. Would email {Email} [{Type}] with subject {Subject}. The message body was not logged.",
                        email,
                        message.Type,
                        subject);
                }
                else
                {
                    _logger.LogInformation(
                        "SMTP_HOST is not set. Would email {Email} [{Type}]: {Title} — {Message}",
                        email,
                        message.Type,
                        subject,
                        body);
                }

                return;
            }

            var mime = new MimeMessage();
            mime.From.Add(MailboxAddress.Parse(_settings.From!));
            mime.To.Add(MailboxAddress.Parse(email));
            mime.Subject = subject;
            mime.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            var socketOptions = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
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
