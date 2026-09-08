using eCommerce.Model.Enums;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using MapsterMapper;

namespace eCommerce.Services.ActivityStateMachine
{
    public class InitialActivityState : BaseActivityState
    {
        public InitialActivityState(ECommerceDbContext dbContext, IMapper mapper, IServiceProvider serviceProvider)
            : base(dbContext, mapper, serviceProvider)
        {
        }

        public override async Task<ActivityResponse> InsertAsync(ActivityInsertRequest request)
        {
            var entity = Mapper.Map<Activity>(request);
            entity.Status = ActivityStatus.Draft;
            entity.CreatedAt = DateTime.UtcNow;
            DbContext.Activities.Add(entity);
            await DbContext.SaveChangesAsync();
            return await MapResponseAsync(entity);
        }

        public override List<string> GetAllowedActions()
        {
            return base.GetAllowedActions().Append(nameof(InsertAsync)).ToList();
        }
    }
}
