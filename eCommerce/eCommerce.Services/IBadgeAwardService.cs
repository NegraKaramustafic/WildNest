namespace eCommerce.Services
{
    public interface IBadgeAwardService
    {
        Task CheckAndAwardBadgesAsync(int userId);
    }
}
