using eCommerce.Model.Enums;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services.ReservationStateMachine
{
    public class ConfirmedReservationState : BaseReservationState
    {
        public ConfirmedReservationState(
            ECommerceDbContext dbContext,
            IMapper mapper,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory)
            : base(dbContext, mapper, serviceProvider, loggerFactory)
        {
        }

        public override async Task<ReservationResponse> CompleteAsync(int id)
        {
            var entity = await DbContext.Reservations.FindAsync(id);
            if (entity == null)
            {
                ThrowNotFound(id);
            }

            entity.Status = ReservationStatus.Completed;
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
            return new List<string> { nameof(CompleteAsync), nameof(CancelAsync) };
        }
    }
}
