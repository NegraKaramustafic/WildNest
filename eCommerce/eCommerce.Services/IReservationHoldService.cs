namespace eCommerce.Services
{
    public interface IReservationHoldService
    {
        Task<int> ReleaseExpiredHoldsAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    }
}
