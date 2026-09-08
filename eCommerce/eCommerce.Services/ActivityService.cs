using System;
using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.ActivityStateMachine;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services;

public class ActivityService : BaseReadService<Activity, ActivityResponse, ActivitySearchObject>, IActivityService
{
    protected BaseActivityState ActivityState { get; }

    public ActivityService(ECommerceDbContext dbContext, MapsterMapper.IMapper mapper, BaseActivityState activityState)
        : base(mapper, dbContext)
    {
        ActivityState = activityState;
    }

    protected override Task<IQueryable<Activity>> IncludeRelatedEntitiesAsync(ActivitySearchObject? search, IQueryable<Activity> query = null!)
    {
        query = query.Include(a => a.Animal);
        return base.IncludeRelatedEntitiesAsync(search, query);
    }

    protected override IEnumerable<Activity> ApplyFilters(IEnumerable<Activity> query, ActivitySearchObject? search)
    {
        if (search != null)
        {
            if (search.Type.HasValue)
            {
                query = query.Where(a => a.Type == search.Type.Value);
            }

            if (search.Status.HasValue)
            {
                query = query.Where(a => a.Status == search.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(search.Name))
            {
                query = query.Where(a => a.Name.Contains(search.Name, StringComparison.OrdinalIgnoreCase));
            }
        }

        return query;
    }

    public Task<ActivityResponse> InsertAsync(ActivityInsertRequest request)
    {
        var state = ActivityState.GetActivityState(nameof(InitialActivityState));
        return state.InsertAsync(request);
    }

    public async Task<ActivityResponse> UpdateAsync(int id, ActivityUpdateRequest request)
    {
        var entity = await _dbContext.Activities.FindAsync(id);
        if (entity == null)
        {
            throw new KeyNotFoundException($"Activity with id {id} not found.");
        }

        var state = ActivityState.GetActivityState(entity.Status);
        return await state.UpdateAsync(id, request);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dbContext.Activities.FindAsync(id);
        if (entity == null)
        {
            throw new KeyNotFoundException($"Activity with id {id} not found.");
        }

        var state = ActivityState.GetActivityState(entity.Status);
        await state.DeleteAsync(id);
    }

    public async Task<ActivityResponse> ActivateAsync(int id)
    {
        var entity = await _dbContext.Activities.FindAsync(id);
        if (entity == null)
        {
            throw new KeyNotFoundException($"Activity with id {id} not found.");
        }

        var state = ActivityState.GetActivityState(entity.Status);
        return await state.ActivateAsync(id);
    }

    public async Task<ActivityResponse> DeactivateAsync(int id)
    {
        var entity = await _dbContext.Activities.FindAsync(id);
        if (entity == null)
        {
            throw new KeyNotFoundException($"Activity with id {id} not found.");
        }

        var state = ActivityState.GetActivityState(entity.Status);
        return await state.DeactivateAsync(id);
    }

    public async Task<List<string>> GetAllowedActionsAsync(int id)
    {
        if (id <= 0)
        {
            var initialState = ActivityState.GetActivityState(nameof(InitialActivityState));
            return initialState.GetAllowedActions();
        }

        var entity = await _dbContext.Activities.FindAsync(id);
        if (entity == null)
        {
            throw new KeyNotFoundException($"Activity with id {id} not found.");
        }

        var state = ActivityState.GetActivityState(entity.Status);
        return state.GetAllowedActions();
    }

    public override async Task<ActivityResponse> GetByIdAsync(int id)
    {
        var entity = await _dbContext.Activities
            .AsNoTracking()
            .Include(a => a.Animal)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (entity == null)
        {
            throw new KeyNotFoundException($"Activity with id {id} not found.");
        }

        var response = _mapper.Map<ActivityResponse>(entity);
        response.AllowedActions = await GetAllowedActionsAsync(id);
        return response;
    }
}
