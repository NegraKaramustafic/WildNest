using System.Linq.Expressions;
using eCommerce.Model.Enums;
using eCommerce.Services.Database;

namespace eCommerce.Services
{
    public interface IAdoptionTermService
    {
        bool IsCurrentlyActive(AdoptionStatus status, DateOnly? validUntil);

        AdoptionStatus StatusForList(AdoptionStatus status, DateOnly? validUntil);

        void ApplyCompletedPayment(Adoption adoption);

        Task<int> ExpireElapsedAsync(CancellationToken cancellationToken = default);
    }

    public static class AdoptionActivity
    {
        public static bool IsCurrent(AdoptionStatus status, DateOnly? validUntil, DateOnly today)
        {
            return status == AdoptionStatus.Active && !(validUntil < today);
        }

        public static Expression<Func<Adoption, bool>> CurrentlyActive(DateOnly today)
        {
            return adoption => adoption.Status == AdoptionStatus.Active
                && (adoption.ValidUntil == null || adoption.ValidUntil >= today);
        }
    }
}
