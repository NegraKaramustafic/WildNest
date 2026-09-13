namespace eCommerce.Services
{
    public interface IBadgeAwardService
    {
        Task<IReadOnlyList<AwardedBadge>> CheckAndAwardBadgesAsync(int userId);
    }
}
