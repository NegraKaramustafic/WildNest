using System;
using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using eCommerce.Services.ReservationStateMachine;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services;

public class ReservationService : BaseReadService<Reservation, ReservationResponse, ReservationSearchObject>, IReservationService
{
    private const string AdminRole = "Admin";

    private readonly IAuthenticatedUserAccessor _userAccessor;

    protected BaseReservationState ReservationState { get; }

    public ReservationService(
        ECommerceDbContext dbContext,
        MapsterMapper.IMapper mapper,
        BaseReservationState reservationState,
        IAuthenticatedUserAccessor userAccessor)
        : base(mapper, dbContext)
    {
        ReservationState = reservationState;
        _userAccessor = userAccessor;
    }

    protected override Task<IQueryable<Reservation>> IncludeRelatedEntitiesAsync(ReservationSearchObject? search, IQueryable<Reservation> query = null!)
    {
        query = query
            .Include(r => r.User)
            .Include(r => r.Schedule)
                .ThenInclude(s => s.Activity);
        return base.IncludeRelatedEntitiesAsync(search, query);
    }

    protected override IEnumerable<Reservation> ApplyFilters(IEnumerable<Reservation> query, ReservationSearchObject? search)
    {
        if (_userAccessor.IsInRole(AdminRole))
        {
            if (search?.UserId.HasValue == true)
            {
                query = query.Where(r => r.UserId == search.UserId.Value);
            }
        }
        else
        {
            var userId = _userAccessor.GetUserId();
            query = userId.HasValue
                ? query.Where(r => r.UserId == userId.Value)
                : query.Where(r => false);
        }

        if (search != null)
        {
            if (search.ScheduleId.HasValue)
            {
                query = query.Where(r => r.ScheduleId == search.ScheduleId.Value);
            }

            if (search.Status.HasValue)
            {
                query = query.Where(r => r.Status == search.Status.Value);
            }
        }

        return query;
    }

    public override async Task<ReservationResponse> GetByIdAsync(int id)
    {
        var entity = await _dbContext.Reservations
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Schedule)
                .ThenInclude(s => s.Activity)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (entity == null || !CanAccessReservation(entity.UserId))
        {
            throw new KeyNotFoundException($"Reservation with id {id} not found.");
        }

        var response = _mapper.Map<ReservationResponse>(entity);
        response.AllowedActions = await GetAllowedActionsAsync(id);
        return response;
    }

    public Task<ReservationResponse> InsertAsync(ReservationInsertRequest request)
    {
        if (!_userAccessor.IsInRole(AdminRole))
        {
            var userId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");
            request.UserId = userId;
        }

        var state = ReservationState.GetReservationState(nameof(InitialReservationState));
        return state.InsertAsync(request);
    }

    public async Task<ReservationResponse> UpdateAsync(int id, ReservationUpdateRequest request)
    {
        var entity = await GetAccessibleEntityAsync(id);
        var state = ReservationState.GetReservationState(entity.Status);
        return await state.UpdateAsync(id, request);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await GetAccessibleEntityAsync(id);
        var state = ReservationState.GetReservationState(entity.Status);
        await state.DeleteAsync(id);
    }

    public async Task<ReservationResponse> ConfirmAsync(int id)
    {
        EnsureAdmin();
        var entity = await GetAccessibleEntityAsync(id);
        var state = ReservationState.GetReservationState(entity.Status);
        return await state.ConfirmAsync(id);
    }

    public async Task<ReservationResponse> CancelAsync(int id)
    {
        var entity = await GetAccessibleEntityAsync(id);
        var state = ReservationState.GetReservationState(entity.Status);
        return await state.CancelAsync(id);
    }

    public async Task<ReservationResponse> CompleteAsync(int id)
    {
        EnsureAdmin();
        var entity = await GetAccessibleEntityAsync(id);
        var state = ReservationState.GetReservationState(entity.Status);
        return await state.CompleteAsync(id);
    }

    public async Task<List<string>> GetAllowedActionsAsync(int id)
    {
        if (id <= 0)
        {
            var initialState = ReservationState.GetReservationState(nameof(InitialReservationState));
            return initialState.GetAllowedActions();
        }

        var entity = await GetAccessibleEntityAsync(id);
        var state = ReservationState.GetReservationState(entity.Status);
        return state.GetAllowedActions();
    }

    private async Task<Reservation> GetAccessibleEntityAsync(int id)
    {
        var entity = await _dbContext.Reservations.FindAsync(id);
        if (entity == null || !CanAccessReservation(entity.UserId))
        {
            throw new KeyNotFoundException($"Reservation with id {id} not found.");
        }

        return entity;
    }

    private bool CanAccessReservation(int reservationUserId)
    {
        if (_userAccessor.IsInRole(AdminRole))
        {
            return true;
        }

        var userId = _userAccessor.GetUserId();
        return userId.HasValue && userId.Value == reservationUserId;
    }

    private void EnsureAdmin()
    {
        if (!_userAccessor.IsInRole(AdminRole))
        {
            throw new KeyNotFoundException("Reservation not found.");
        }
    }
}
