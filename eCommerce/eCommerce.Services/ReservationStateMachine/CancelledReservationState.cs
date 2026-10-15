using eCommerce.Services.Database;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services.ReservationStateMachine
{
    public class CancelledReservationState : BaseReservationState
    {
        public CancelledReservationState(
            ECommerceDbContext dbContext,
            IMapper mapper,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory)
            : base(dbContext, mapper, serviceProvider, loggerFactory)
        {
        }
    }
}
