using eCommerce.Model.Enums;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using MapsterMapper;

namespace eCommerce.Services.ReservationStateMachine
{
    public class PendingReservationState : BaseReservationState
    {
        public PendingReservationState(ECommerceDbContext dbContext, IMapper mapper, IServiceProvider serviceProvider)
            : base(dbContext, mapper, serviceProvider)
        {
        }

        public override async Task<ReservationResponse> ConfirmAsync(int id)
        {
            var entity = await DbContext.Reservations.FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Reservation with id {id} not found.");
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
                throw new KeyNotFoundException($"Reservation with id {id} not found.");
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
