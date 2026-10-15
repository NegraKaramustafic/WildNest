using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services.ReservationStateMachine
{
    public class PendingReservationState : BaseReservationState
    {
        public PendingReservationState(
            ECommerceDbContext dbContext,
            IMapper mapper,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory)
            : base(dbContext, mapper, serviceProvider, loggerFactory)
        {
        }

        public override async Task<ReservationResponse> ConfirmAsync(int id)
        {
            var entity = await DbContext.Reservations.FindAsync(id);
            if (entity == null)
            {
                ThrowNotFound(id);
            }

            if (!entity.PaymentId.HasValue
                || !await DbContext.Payments.AnyAsync(p => p.Id == entity.PaymentId.Value && p.Status == PaymentStatus.Completed))
            {
                throw new ClientException("Reservation cannot be confirmed until payment is completed.");
            }

            entity.Status = ReservationStatus.Confirmed;
            await DbContext.SaveChangesAsync();
            return await MapResponseAsync(entity);
        }

        public override async Task<ReservationResponse> CancelAsync(int id)
        {
            var entity = await DbContext.Reservations.FindAsync(id);
            if (entity == null)
            {
                ThrowNotFound(id);
            }

            entity.Status = ReservationStatus.Cancelled;
            await RestoreCapacityAsync(entity);
            await DbContext.SaveChangesAsync();
            return await MapResponseAsync(entity);
        }

        public override List<string> GetAllowedActions()
        {
            return new List<string> { nameof(ConfirmAsync), nameof(CancelAsync) };
        }
    }
}
