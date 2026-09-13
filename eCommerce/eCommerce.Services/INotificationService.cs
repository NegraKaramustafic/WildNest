using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface INotificationService : IBaseReadService<NotificationResponse, NotificationSearchObject>
    {
        Task<int> GetUnreadCountAsync(int? userId);
        Task<NotificationResponse> MarkReadAsync(int id);
        Task<int> MarkAllReadAsync();
    }
}
