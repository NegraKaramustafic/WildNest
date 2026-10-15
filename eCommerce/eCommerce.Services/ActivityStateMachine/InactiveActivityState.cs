using eCommerce.Model.Enums;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services.ActivityStateMachine
{
    public class InactiveActivityState : BaseActivityState
    {
        public InactiveActivityState(
            ECommerceDbContext dbContext,
            IMapper mapper,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory)
            : base(dbContext, mapper, serviceProvider, loggerFactory)
        {
        }

        public override async Task<ActivityResponse> ActivateAsync(int id)
        {
            var entity = await DbContext.Activities.FindAsync(id);
            if (entity == null)
            {
                ThrowNotFound(id);
            }

            entity.Status = ActivityStatus.Active;
            await DbContext.SaveChangesAsync();
            return await MapResponseAsync(entity);
        }

        public override List<string> GetAllowedActions()
        {
            return new List<string> { nameof(ActivateAsync) };
        }
    }
}
