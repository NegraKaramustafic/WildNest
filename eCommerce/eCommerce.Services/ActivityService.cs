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
using eCommerce.Services.Images;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services;

public class ActivityService : BaseReadService<Activity, ActivityResponse, ActivitySearchObject>, IActivityService
{

    protected BaseActivityState ActivityState { get; }

    private readonly IAuthenticatedUserAccessor _userAccessor;
    private readonly IValidator<ActivityInsertRequest> _insertValidator;
    private readonly IValidator<ActivityUpdateRequest> _updateValidator;
    private readonly IStoredImageService _images;

    public ActivityService(
        ECommerceDbContext dbContext,
        MapsterMapper.IMapper mapper,
        BaseActivityState activityState,
        IAuthenticatedUserAccessor userAccessor,
        IValidator<ActivityInsertRequest> insertValidator,
        IValidator<ActivityUpdateRequest> updateValidator,
        IStoredImageService images,
        ILoggerFactory loggerFactory)
        : base(mapper, dbContext, loggerFactory)
    {
        ActivityState = activityState;
        _userAccessor = userAccessor;
        _insertValidator = insertValidator;
        _updateValidator = updateValidator;
        _images = images;
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

    public async Task<ActivityResponse> InsertAsync(ActivityInsertRequest request)
    {
        await ValidateInsertAsync(request);

        string? created = null;
        try
        {
            created = ImageUpload.StoreIfPresent(_images, request.ImageBase64, request.ImageContentType);
            request.ImageUrl = ImageUpload.ResolveUploaded(null, request.ImageUrl, created);
            request.ImageBase64 = null;
            var state = ActivityState.GetActivityState(nameof(InitialActivityState));
            var saved = await state.InsertAsync(request);
            created = null;
            return saved;
        }
        catch
        {
            _images.Delete(created);
            throw;
        }
    }

    public async Task<ActivityResponse> UpdateAsync(int id, ActivityUpdateRequest request)
    {
        await ValidateUpdateAsync(request);

        var entity = await _dbContext.Activities.FirstOrDefaultAsync(activity => activity.Id == id);
        if (entity == null)
        {
            ThrowNotFound("Activity", id, "Activity not found.");
        }

        var state = ActivityState.GetActivityState(entity.Status);
        if (!state.GetAllowedActions().Contains(nameof(DraftActivityState.UpdateAsync)))
        {
            return await state.UpdateAsync(id, request);
        }

        var previousImage = entity.ImageUrl;
        string? created = null;
        try
        {
            created = ImageUpload.StoreIfPresent(_images, request.ImageBase64, request.ImageContentType);
            request.ImageUrl = ImageUpload.ResolveUploaded(previousImage, request.ImageUrl, created);
            request.ImageBase64 = null;
            var saved = await state.UpdateAsync(id, request);
            created = null;
            if (!string.Equals(previousImage, saved.ImageUrl, StringComparison.Ordinal))
            {
                _images.Delete(previousImage);
            }

            return saved;
        }
        catch
        {
            _images.Delete(created);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _dbContext.Activities.FirstOrDefaultAsync(activity => activity.Id == id);
        if (entity == null)
        {
            ThrowNotFound("Activity", id, "Activity not found.");
        }

        var image = entity.ImageUrl;
        var state = ActivityState.GetActivityState(entity.Status);
        await state.DeleteAsync(id);
        _images.Delete(image);
    }

    private async Task ValidateInsertAsync(ActivityInsertRequest request)
    {
        var result = await _insertValidator.ValidateAsync(request);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }

    private async Task ValidateUpdateAsync(ActivityUpdateRequest request)
    {
        var result = await _updateValidator.ValidateAsync(request);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
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
