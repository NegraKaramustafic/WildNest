using eCommerce.Model.Enums;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services
{
    public class BadgeAwardService : IBadgeAwardService
    {
        private readonly ECommerceDbContext _dbContext;
        private readonly INotificationEventService _notificationEvents;

        public BadgeAwardService(ECommerceDbContext dbContext, INotificationEventService notificationEvents)
        {
            _dbContext = dbContext;
            _notificationEvents = notificationEvents;
        }

        public async Task<IReadOnlyList<AwardedBadge>> CheckAndAwardBadgesAsync(int userId)
        {
            var badges = await _dbContext.Badges.ToListAsync();
            if (badges.Count == 0)
            {
                return Array.Empty<AwardedBadge>();
            }

            var earnedBadgeIds = await _dbContext.UserBadges
                .Where(ub => ub.UserId == userId)
                .Select(ub => ub.BadgeId)
                .ToListAsync();

            var adoptionCount = await _dbContext.Adoptions.CountAsync(a => a.UserId == userId);
            var reservationCount = await _dbContext.Reservations.CountAsync(r => r.UserId == userId);
            var donationCount = await _dbContext.Donations.CountAsync(d => d.UserId == userId);

            var awarded = new List<AwardedBadge>();
            foreach (var badge in badges)
            {
                if (earnedBadgeIds.Contains(badge.Id))
                {
                    continue;
                }

                var earned = badge.Criteria switch
                {
                    BadgeCriteria.FirstAdoption => adoptionCount >= 1,
                    BadgeCriteria.FirstBooking => reservationCount >= 1,
                    BadgeCriteria.ThreeDonations => donationCount >= 3,
                    BadgeCriteria.FiveAdoptions => adoptionCount >= 5,
                    BadgeCriteria.TenAnimalVisits => false,
                    _ => false
                };

                if (!earned)
                {
                    continue;
                }

                _dbContext.UserBadges.Add(new UserBadge
                {
                    UserId = userId,
                    BadgeId = badge.Id,
                    EarnedAt = DateTime.UtcNow
                });
                awarded.Add(new AwardedBadge { BadgeId = badge.Id, Name = badge.Name });
            }

            if (awarded.Count == 0)
            {
                return awarded;
            }

            await _dbContext.SaveChangesAsync();

            foreach (var badge in awarded)
            {
                await _notificationEvents.NotifyBadgeEarnedAsync(userId, badge.BadgeId, badge.Name);
            }

            return awarded;
        }
    }
}
