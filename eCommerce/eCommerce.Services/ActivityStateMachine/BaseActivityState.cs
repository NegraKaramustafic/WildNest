using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using FluentValidation;
using FluentValidation.Results;
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
            throw new ClientException("Cannot insert an activity in its current state.");
        }

        public virtual Task<ActivityResponse> UpdateAsync(int id, ActivityUpdateRequest request)
        {
            throw new ClientException("Cannot update an activity in its current state.");
        }

        public virtual Task<ActivityResponse> ActivateAsync(int id)
        {
            throw new ClientException("Cannot activate an activity in its current state.");
        }

        public virtual Task<ActivityResponse> DeactivateAsync(int id)
        {
            throw new ClientException("Cannot deactivate an activity in its current state.");
        }

        public virtual Task<ActivityResponse> DeleteAsync(int id)
        {
            throw new ClientException("Cannot delete an activity in its current state.");
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
                    throw new ClientException($"Unknown activity state: {stateName}");
            }
        }

        public BaseActivityState GetActivityState(ActivityStatus status)
        {
            return status switch
            {
                ActivityStatus.Draft => GetActivityState(nameof(DraftActivityState)),
                ActivityStatus.Active => GetActivityState(nameof(ActiveActivityState)),
                ActivityStatus.Inactive => GetActivityState(nameof(InactiveActivityState)),
                _ => throw new ClientException($"Unknown activity status: {status}")
            };
        }

        public virtual List<string> GetAllowedActions()
        {
            return new List<string>();
        }

        protected async Task EnsureActivityTypeExistsAsync(int activityTypeId)
        {
            var exists = await DbContext.ActivityTypes
                .AsNoTracking()
                .AnyAsync(type => type.Id == activityTypeId);
            if (!exists)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(
                        nameof(ActivityInsertRequest.ActivityTypeId),
                        "Odabrani tip aktivnosti ne postoji.")
                });
            }
        }

        protected async Task<ActivityResponse> MapResponseAsync(Activity entity)
        {
            if (entity.AnimalId.HasValue && entity.Animal == null)
            {
                await DbContext.Entry(entity).Reference(a => a.Animal).LoadAsync();
            }

            if (entity.ActivityType == null)
            {
                await DbContext.Entry(entity).Reference(a => a.ActivityType).LoadAsync();
            }

            return Mapper.Map<ActivityResponse>(entity);
        }
    }
}
