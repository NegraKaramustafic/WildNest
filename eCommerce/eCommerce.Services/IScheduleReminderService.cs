namespace eCommerce.Services
{
    public interface IScheduleReminderService
    {
        Task<int> SendDueRemindersAsync(CancellationToken cancellationToken = default);
    }
}
