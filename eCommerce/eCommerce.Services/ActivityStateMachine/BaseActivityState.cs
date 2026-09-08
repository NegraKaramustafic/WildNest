using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace eCommerce.Services.ActivityStateMachine
{
    public class BaseActivityState
    {
        protected ECommerceDbContext DbContext { get; }
        protected IMapper Mapper { get; }
        protected IServiceProvider ServiceProvider { get; }

        public BaseActivityState(ECommerceDbContext dbContext, IMapper mapper, IServiceProvider serviceProvider)
        {
            DbContext = dbContext;
            Mapper = mapper;
            ServiceProvider = serviceProvider;
        }

        public virtual Task<ActivityResponse> InsertAsync(ActivityInsertRequest request)
        {
            throw new ClinetException("Cannot insert an activity in its current state.");
        }

        public virtual Task<ActivityResponse> UpdateAsync(int id, ActivityUpdateRequest request)
        {
            throw new ClinetException("Cannot update an activity in its current state.");
        }

        public virtual Task<ActivityResponse> ActivateAsync(int id)
        {
            throw new ClinetException("Cannot activate an activity in its current state.");
        }

        public virtual Task<ActivityResponse> DeactivateAsync(int id)
        {
            throw new ClinetException("Cannot deactivate an activity in its current state.");
        }

        public virtual Task<ActivityResponse> DeleteAsync(int id)
        {
            throw new ClinetException("Cannot delete an activity in its current state.");
        }

        public BaseActivityState GetActivityState(string stateName)
        {
            switch (stateName)
            {
                case nameof(InitialActivityState):
                    return ServiceProvider.GetService<InitialActivityState>()!;
                case nameof(DraftActivityState):
                    return ServiceProvider.GetService<DraftActivityState>()!;
                case nameof(ActiveActivityState):
                    return ServiceProvider.GetService<ActiveActivityState>()!;
                case nameof(InactiveActivityState):
                    return ServiceProvider.GetService<InactiveActivityState>()!;
                default:
                    throw new ClinetException($"Unknown activity state: {stateName}");
            }
        }

        public BaseActivityState GetActivityState(ActivityStatus status)
        {
            return status switch
            {
                ActivityStatus.Draft => GetActivityState(nameof(DraftActivityState)),
                ActivityStatus.Active => GetActivityState(nameof(ActiveActivityState)),
                ActivityStatus.Inactive => GetActivityState(nameof(InactiveActivityState)),
                _ => throw new ClinetException($"Unknown activity status: {status}")
            };
        }

        public virtual List<string> GetAllowedActions()
        {
            return new List<string>();
        }

        protected async Task<ActivityResponse> MapResponseAsync(Activity entity)
        {
            if (entity.AnimalId.HasValue && entity.Animal == null)
            {
                await DbContext.Entry(entity).Reference(a => a.Animal).LoadAsync();
            }

            return Mapper.Map<ActivityResponse>(entity);
        }
    }
}
