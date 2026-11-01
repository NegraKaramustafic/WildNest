using eCommerce.Model.Enums;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services
{
    public class AdoptionTermService : IAdoptionTermService
    {
        private readonly ECommerceDbContext _dbContext;
        private readonly ZooClock _zooClock;

        public AdoptionTermService(ECommerceDbContext dbContext, ZooClock zooClock)
        {
            _dbContext = dbContext;
            _zooClock = zooClock;
        }

        public bool IsCurrentlyActive(AdoptionStatus status, DateOnly? validUntil)
        {
            return AdoptionActivity.IsCurrent(status, validUntil, _zooClock.Today);
        }

        public AdoptionStatus StatusForList(AdoptionStatus status, DateOnly? validUntil)
        {
            if (status == AdoptionStatus.Active && validUntil < _zooClock.Today)
            {
                return AdoptionStatus.Expired;
            }

            return status;
        }

        public void ApplyCompletedPayment(Adoption adoption)
        {
            if (adoption.Status == AdoptionStatus.Cancelled)
            {
                return;
            }

            var today = _zooClock.Today;
            if (adoption.ValidUntil is null)
            {
                adoption.ValidUntil = today.AddMonths(1);
            }
            else if (adoption.ValidUntil.Value >= today)
            {
                adoption.ValidUntil = adoption.ValidUntil.Value.AddMonths(1);
            }
            else
            {
                adoption.ValidUntil = today.AddMonths(1);
            }

            adoption.Status = AdoptionStatus.Active;
        }

        public async Task<int> ExpireElapsedAsync(CancellationToken cancellationToken = default)
        {
            var today = _zooClock.Today;
            return await _dbContext.Adoptions
                .Where(adoption => adoption.Status == AdoptionStatus.Active
                    && adoption.ValidUntil != null
                    && adoption.ValidUntil < today)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(adoption => adoption.Status, AdoptionStatus.Expired),
                    cancellationToken);
        }
    }
}
