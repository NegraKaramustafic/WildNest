using eCommerce.Model.Messages;
using eCommerce.Services.Database;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services
{
    /// <summary>
    /// Stores one notification row per MessageId and records when its email was sent.
    /// A redelivered message skips SMTP once <see cref="Notification.EmailSentAt"/> is set.
    /// </summary>
    public sealed class NotificationDeliveryStore
    {
        private readonly ECommerceDbContext _dbContext;

        public NotificationDeliveryStore(ECommerceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> NeedsEmailAsync(NotificationMessage message, CancellationToken cancellationToken = default)
        {
            var existingSentAt = await EmailSentAtAsync(message.MessageId, cancellationToken);
            if (existingSentAt.Found)
            {
                return existingSentAt.EmailSentAt == null;
            }

            _dbContext.Notifications.Add(new Notification
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
                await _dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _dbContext.ChangeTracker.Clear();
                var stored = await EmailSentAtAsync(message.MessageId, cancellationToken);
                return !stored.Found || stored.EmailSentAt == null;
            }
        }

        public async Task MarkEmailSentAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var updated = await _dbContext.Notifications
                .Where(notification => notification.MessageId == messageId && notification.EmailSentAt == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(notification => notification.EmailSentAt, now),
                    cancellationToken);

            if (updated > 0)
            {
                return;
            }

            var stored = await EmailSentAtAsync(messageId, cancellationToken);
            if (stored.Found && stored.EmailSentAt != null)
            {
                return;
            }

            throw new InvalidOperationException($"Notification {messageId} could not be marked as emailed.");
        }

        private async Task<(bool Found, DateTime? EmailSentAt)> EmailSentAtAsync(Guid messageId, CancellationToken cancellationToken)
        {
            var row = await _dbContext.Notifications
                .AsNoTracking()
                .Where(notification => notification.MessageId == messageId)
                .Select(notification => new { notification.EmailSentAt })
                .FirstOrDefaultAsync(cancellationToken);

            return row == null
                ? (false, null)
                : (true, row.EmailSentAt);
        }

        internal static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            for (Exception? current = exception; current != null; current = current.InnerException)
            {
                if (current is SqlException sql && sql.Number is 2601 or 2627)
                {
                    return true;
                }

                if (current.GetType().Name == "SqliteException"
                    && current.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
