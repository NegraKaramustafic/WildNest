using eCommerce.Model.Enums;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using MapsterMapper;
using eCommerce.Model.Exceptions;

namespace eCommerce.Services.ActivityStateMachine
{
    public class DraftActivityState : BaseActivityState
    {
        public DraftActivityState(ECommerceDbContext dbContext, IMapper mapper, IServiceProvider serviceProvider)
            : base(dbContext, mapper, serviceProvider)
        {
        }

        public override async Task<ActivityResponse> UpdateAsync(int id, ActivityUpdateRequest request)
        {
            var entity = await DbContext.Activities.FindAsync(id);
            if (entity == null)
            {
                throw new NotFoundException($"Activity with id {id} not found.");
            }

            await EnsureActivityTypeExistsAsync(request.ActivityTypeId);
            entity.ActivityType = null;
            Mapper.Map(request, entity);
            await DbContext.SaveChangesAsync();
            return await MapResponseAsync(entity);
        }

        public override async Task<ActivityResponse> ActivateAsync(int id)
        {
            var entity = await DbContext.Activities.FindAsync(id);
            if (entity == null)
            {
                throw new NotFoundException($"Activity with id {id} not found.");
            }

            entity.Status = ActivityStatus.Active;
            await DbContext.SaveChangesAsync();
            return await MapResponseAsync(entity);
        }

        public override async Task<ActivityResponse> DeleteAsync(int id)
        {
            var entity = await DbContext.Activities.FindAsync(id);
            if (entity == null)
            {
                throw new NotFoundException($"Activity with id {id} not found.");
            }

            DbContext.Activities.Remove(entity);
            await DbContext.SaveChangesAsync();
            return Mapper.Map<ActivityResponse>(entity);
        }

        public override List<string> GetAllowedActions()
        {
            return new List<string> { nameof(UpdateAsync), nameof(ActivateAsync), nameof(DeleteAsync) };
        }
    }
}
