using System;
using eCommerce.Model;
using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Enums;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.ActivityStateMachine;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services;

public class ActivityService : BaseReadService<Activity, ActivityResponse, ActivitySearchObject>, IActivityService
{

    protected BaseActivityState ActivityState { get; }

    private readonly IAuthenticatedUserAccessor _userAccessor;

    public ActivityService(
        ECommerceDbContext dbContext,
        MapsterMapper.IMapper mapper,
        BaseActivityState activityState,
        IAuthenticatedUserAccessor userAccessor,
        ILoggerFactory loggerFactory)
        : base(mapper, dbContext, loggerFactory)
    {
        ActivityState = activityState;
        _userAccessor = userAccessor;
    }

    protected override Task<IQueryable<Activity>> IncludeRelatedEntitiesAsync(ActivitySearchObject? search, IQueryable<Activity> query = null!)
    {
        query = query.Include(a => a.Animal).Include(a => a.ActivityType);
        return base.IncludeRelatedEntitiesAsync(search, query);
    }

    protected override IQueryable<Activity> ApplyFilters(IQueryable<Activity> query, ActivitySearchObject? search)
    {
        if (!_userAccessor.IsInRole(Roles.Admin))
        {
            query = query.Where(a => a.Status == ActivityStatus.Active);
        }

        if (search != null)
        {
            if (search.ActivityTypeId.HasValue)
            {
                query = query.Where(a => a.ActivityTypeId == search.ActivityTypeId.Value);
            }

            if (search.Status.HasValue)
            {
                query = query.Where(a => a.Status == search.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(search.Name))
            {
                var name = search.Name.Trim();
                query = query.Where(a => a.Name.Contains(name));
            }
        }

        return query;
    }

    protected override IQueryable<Activity> ApplyDefaultOrder(IQueryable<Activity> query)
    {
        return query.OrderBy(a => a.Name).ThenBy(a => a.Id);
    }

    public override async Task<PageResult<ActivityResponse>> GetAllAsync(ActivitySearchObject? search = null)
    {
        var page = await base.GetAllAsync(search);
        foreach (var item in page.Items)
        {
            item.AllowedActions = ActivityState.GetActivityState(item.Status).GetAllowedActions();
        }

        return page;
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
            ThrowNotFound("Activity", id, "Activity not found.");
        }

        var state = ActivityState.GetActivityState(entity.Status);
        return await state.UpdateAsync(id, request);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dbContext.Activities.FindAsync(id);
        if (entity == null)
        {
            ThrowNotFound("Activity", id, "Activity not found.");
        }

        var state = ActivityState.GetActivityState(entity.Status);
        await state.DeleteAsync(id);
    }

    public async Task<ActivityResponse> ActivateAsync(int id)
    {
        var entity = await _dbContext.Activities.FindAsync(id);
        if (entity == null)
        {
            ThrowNotFound("Activity", id, "Activity not found.");
        }

        var state = ActivityState.GetActivityState(entity.Status);
        return await state.ActivateAsync(id);
    }

    public async Task<ActivityResponse> DeactivateAsync(int id)
    {
        var entity = await _dbContext.Activities.FindAsync(id);
        if (entity == null)
        {
            ThrowNotFound("Activity", id, "Activity not found.");
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
            ThrowNotFound("Activity", id, "Activity not found.");
        }

        var state = ActivityState.GetActivityState(entity.Status);
        return state.GetAllowedActions();
    }

    public override async Task<ActivityResponse> GetByIdAsync(int id)
    {
        var entity = await _dbContext.Activities
            .AsNoTracking()
            .Include(a => a.Animal)
            .Include(a => a.ActivityType)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (entity == null || !CanViewActivity(entity))
        {
            ThrowNotFound("Activity", id, "Activity not found.");
        }

        var response = _mapper.Map<ActivityResponse>(entity);
        response.AllowedActions = await GetAllowedActionsAsync(id);
        return response;
    }

    private bool CanViewActivity(Activity entity)
    {
        return entity.Status == ActivityStatus.Active || _userAccessor.IsInRole(Roles.Admin);
    }
}
