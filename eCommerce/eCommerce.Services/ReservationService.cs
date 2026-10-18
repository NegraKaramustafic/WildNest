using System;
using eCommerce.Model;
using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using eCommerce.Services.ReservationStateMachine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using eCommerce.Model.Exceptions;
using FluentValidation;

namespace eCommerce.Services;

public class ReservationService : BaseReadService<Reservation, ReservationResponse, ReservationSearchObject>, IReservationService
{

    private readonly IAuthenticatedUserAccessor _userAccessor;
    private readonly INotificationEventService _notificationEvents;
    private readonly IPaymentService _paymentService;
    private readonly IValidator<ReservationCancelRequest> _cancelValidator;

    protected BaseReservationState ReservationState { get; }

    public ReservationService(
        ECommerceDbContext dbContext,
        MapsterMapper.IMapper mapper,
        BaseReservationState reservationState,
        IAuthenticatedUserAccessor userAccessor,
        INotificationEventService notificationEvents,
        IPaymentService paymentService,
        IValidator<ReservationCancelRequest> cancelValidator,
        ILoggerFactory loggerFactory)
        : base(mapper, dbContext, loggerFactory)
    {
        ReservationState = reservationState;
        _userAccessor = userAccessor;
        _notificationEvents = notificationEvents;
        _paymentService = paymentService;
        _cancelValidator = cancelValidator;
    }

    protected override Task<IQueryable<Reservation>> IncludeRelatedEntitiesAsync(ReservationSearchObject? search, IQueryable<Reservation> query = null!)
    {
        query = query
            .Include(r => r.User)
            .Include(r => r.Schedule)
                .ThenInclude(s => s.Activity)
            .Include(r => r.Payment)
            .Include(r => r.ConfirmedByUser)
            .Include(r => r.CancelledByUser);
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

            if (search.ActivityId.HasValue)
            {
                query = query.Where(r => r.Schedule.ActivityId == search.ActivityId.Value);
            }

            EnsureDateRange(search.DateFrom, search.DateTo);

            if (search.DateFrom.HasValue)
            {
                query = query.Where(r => r.Schedule.Date >= search.DateFrom.Value);
            }

            if (search.DateTo.HasValue)
            {
                query = query.Where(r => r.Schedule.Date <= search.DateTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(search.FTS))
            {
                var term = search.FTS.Trim();
                query = query.Where(r =>
                    r.Code.Contains(term)
                    || r.User.FirstName.Contains(term)
                    || r.User.LastName.Contains(term)
                    || (r.User.FirstName + " " + r.User.LastName).Contains(term)
                    || r.Schedule.Activity.Name.Contains(term));
            }
        }

        return query;
    }

    public override async Task<PageResult<ReservationResponse>> GetAllAsync(ReservationSearchObject? search = null)
    {
        var page = await base.GetAllAsync(search);
        foreach (var item in page.Items)
        {
            item.AllowedActions = ReservationState.GetReservationState(item.Status).GetAllowedActions();
        }

        return page;
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
            .Include(r => r.Payment)
            .Include(r => r.ConfirmedByUser)
            .Include(r => r.CancelledByUser)
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
        var adminId = RequireAdminUserId();
        entity.ConfirmedByUserId = adminId;
        entity.ConfirmedAt = DateTime.UtcNow;
        var state = ReservationState.GetReservationState(entity.Status);
        var confirmed = await state.ConfirmAsync(id);
        await _notificationEvents.NotifyReservationConfirmedAsync(confirmed);
        return confirmed;
    }

    public Task<ReservationResponse> CancelAsync(int id)
    {
        return CancelCoreAsync(id, reason: null, recordAdminDecision: false);
    }

    public async Task<ReservationResponse> CancelWithReasonAsync(int id, ReservationCancelRequest request)
    {
        EnsureAdmin(id);
        await ThrowFirstErrorAsync(_cancelValidator, request);
        return await CancelCoreAsync(id, request.Reason.Trim(), recordAdminDecision: true);
    }

    private async Task<ReservationResponse> CancelCoreAsync(int id, string? reason, bool recordAdminDecision)
    {
        var entity = await GetAccessibleEntityAsync(id);
        var paymentId = entity.PaymentId;
        if (recordAdminDecision)
        {
            entity.CancelledByUserId = RequireAdminUserId();
            entity.CancelledAt = DateTime.UtcNow;
            entity.CancellationReason = reason;
        }

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

    private int RequireAdminUserId()
    {
        return _userAccessor.GetUserId()
            ?? throw new ClientException("Administrator id is missing.");
    }

    private static void EnsureDateRange(DateOnly? from, DateOnly? to)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw new ClientException("From date cannot be after to date.");
        }
    }

    private static async Task ThrowFirstErrorAsync<T>(IValidator<T> validator, T request)
    {
        var result = await validator.ValidateAsync(request);
        if (!result.IsValid)
        {
            throw new ClientException(result.Errors[0].ErrorMessage);
        }
    }
}
