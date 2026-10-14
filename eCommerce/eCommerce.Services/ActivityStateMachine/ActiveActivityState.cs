using eCommerce.Model.Enums;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using MapsterMapper;
using eCommerce.Model.Exceptions;

namespace eCommerce.Services.ActivityStateMachine
{
    public class ActiveActivityState : BaseActivityState
    {
        public ActiveActivityState(ECommerceDbContext dbContext, IMapper mapper, IServiceProvider serviceProvider)
            : base(dbContext, mapper, serviceProvider)
        {
        }

        public override async Task<ActivityResponse> DeactivateAsync(int id)
        {
            var entity = await DbContext.Activities.FindAsync(id);
            if (entity == null)
            {
                throw new NotFoundException($"Activity with id {id} not found.");
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
