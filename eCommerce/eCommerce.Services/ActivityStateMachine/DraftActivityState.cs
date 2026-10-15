using eCommerce.Model.Enums;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services.ActivityStateMachine
{
    public class DraftActivityState : BaseActivityState
    {
        public DraftActivityState(
            ECommerceDbContext dbContext,
            IMapper mapper,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory)
            : base(dbContext, mapper, serviceProvider, loggerFactory)
        {
        }

        public override async Task<ActivityResponse> UpdateAsync(int id, ActivityUpdateRequest request)
        {
            var entity = await DbContext.Activities.FindAsync(id);
            if (entity == null)
            {
                ThrowNotFound(id);
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
                ThrowNotFound(id);
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
                ThrowNotFound(id);
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
