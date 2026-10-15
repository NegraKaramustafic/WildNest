using System;
using eCommerce.Model;
using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using eCommerce.Services.Recommendation;
using eCommerce.Services.ReservationStateMachine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using eCommerce.Model.Exceptions;

namespace eCommerce.Services;

public class ReservationService : BaseReadService<Reservation, ReservationResponse, ReservationSearchObject>, IReservationService
{

    private readonly IAuthenticatedUserAccessor _userAccessor;
    private readonly INotificationEventService _notificationEvents;
    private readonly RecommendationModelCache _recommendationModelCache;
    private readonly IPaymentService _paymentService;

    protected BaseReservationState ReservationState { get; }

    public ReservationService(
        ECommerceDbContext dbContext,
        MapsterMapper.IMapper mapper,
        BaseReservationState reservationState,
        IAuthenticatedUserAccessor userAccessor,
        INotificationEventService notificationEvents,
        RecommendationModelCache recommendationModelCache,
        IPaymentService paymentService,
        ILoggerFactory loggerFactory)
        : base(mapper, dbContext, loggerFactory)
    {
        ReservationState = reservationState;
        _userAccessor = userAccessor;
        _notificationEvents = notificationEvents;
        _recommendationModelCache = recommendationModelCache;
        _paymentService = paymentService;
    }

    protected override Task<IQueryable<Reservation>> IncludeRelatedEntitiesAsync(ReservationSearchObject? search, IQueryable<Reservation> query = null!)
    {
        query = query
            .Include(r => r.User)
            .Include(r => r.Schedule)
                .ThenInclude(s => s.Activity);
        return base.IncludeRelatedEntitiesAsync(search, query);
    }

    protected override IQueryable<Reservation> ApplyFilters(IQueryable<Reservation> query, ReservationSearchObject? search)
    {
        if (_userAccessor.IsInRole(Roles.Admin))
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

    protected override IQueryable<Reservation> ApplyDefaultOrder(IQueryable<Reservation> query)
    {
        return query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id);
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
            ThrowNotFound("Reservation", id, "Reservation not found.");
        }

        var response = _mapper.Map<ReservationResponse>(entity);
        response.AllowedActions = await GetAllowedActionsAsync(id);
        return response;
    }

    public async Task<ReservationResponse> InsertAsync(ReservationInsertRequest request)
    {
        var state = ReservationState.GetReservationState(nameof(InitialReservationState));
        var created = await state.InsertAsync(request);
        await _notificationEvents.NotifyReservationCreatedAsync(created);
        return created;
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
        EnsureAdmin(id);
        var entity = await GetAccessibleEntityAsync(id);
        var state = ReservationState.GetReservationState(entity.Status);
        var confirmed = await state.ConfirmAsync(id);
        _recommendationModelCache.MarkDirty();
        await _notificationEvents.NotifyReservationConfirmedAsync(confirmed);
        return confirmed;
    }

    public async Task<ReservationResponse> CancelAsync(int id)
    {
        var entity = await GetAccessibleEntityAsync(id);
        var paymentId = entity.PaymentId;
        var ownsTransaction = _dbContext.Database.CurrentTransaction == null;
        ReservationResponse cancelled = null!;
        await DbTransactions.ExecuteAsync(_dbContext, async () =>
        {
            var state = ReservationState.GetReservationState(entity.Status);
            cancelled = await state.CancelAsync(id);
            if (paymentId.HasValue)
            {
                await _paymentService.HandleEntityCancellationAsync(paymentId.Value);
            }

            await _dbContext.SaveChangesAsync();
        });

        if (ownsTransaction)
        {
            await _notificationEvents.NotifyReservationCancelledAsync(cancelled);
        }

        return cancelled;
    }

    public async Task<ReservationResponse> CompleteAsync(int id)
    {
        EnsureAdmin(id);
        var entity = await GetAccessibleEntityAsync(id);
        var state = ReservationState.GetReservationState(entity.Status);
        var completed = await state.CompleteAsync(id);
        _recommendationModelCache.MarkDirty();
        return completed;
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
            ThrowNotFound("Reservation", id, "Reservation not found.");
        }

        return entity;
    }

    private bool CanAccessReservation(int reservationUserId)
    {
        if (_userAccessor.IsInRole(Roles.Admin))
        {
            return true;
        }

        var userId = _userAccessor.GetUserId();
        return userId.HasValue && userId.Value == reservationUserId;
    }

    private void EnsureAdmin(int id)
    {
        if (!_userAccessor.IsInRole(Roles.Admin))
        {
            _logger.LogWarning("Reservation {Id} is not accessible.", id);
            throw new NotFoundException("Reservation not found.");
        }
    }
}
