using eCommerce.Model.Enums;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services.ActivityStateMachine
{
    public class ActiveActivityState : BaseActivityState
    {
        public ActiveActivityState(
            ECommerceDbContext dbContext,
            IMapper mapper,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory)
            : base(dbContext, mapper, serviceProvider, loggerFactory)
        {
        }

        public override async Task<ActivityResponse> DeactivateAsync(int id)
        {
            var entity = await DbContext.Activities.FindAsync(id);
            if (entity == null)
            {
                ThrowNotFound(id);
            }

            entity.Status = ActivityStatus.Inactive;
            await DbContext.SaveChangesAsync();
            return await MapResponseAsync(entity);
        }

        public override List<string> GetAllowedActions()
        {
            return new List<string> { nameof(DeactivateAsync) };
        }
    }
}
