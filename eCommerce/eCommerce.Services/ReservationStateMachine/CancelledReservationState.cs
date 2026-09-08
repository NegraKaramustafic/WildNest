using eCommerce.Services.Database;
using MapsterMapper;

namespace eCommerce.Services.ReservationStateMachine
{
    public class CancelledReservationState : BaseReservationState
    {
        public CancelledReservationState(ECommerceDbContext dbContext, IMapper mapper, IServiceProvider serviceProvider)
            : base(dbContext, mapper, serviceProvider)
        {
        }
    }
}
