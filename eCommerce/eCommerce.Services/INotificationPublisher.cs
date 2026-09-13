using eCommerce.Model.Messages;

namespace eCommerce.Services
{
    public interface INotificationPublisher
    {
        Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default);
    }
}
