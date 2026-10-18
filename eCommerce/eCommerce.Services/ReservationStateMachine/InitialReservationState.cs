using eCommerce.Model.Enums;
using eCommerce.Services;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services.ReservationStateMachine
{
    public class InitialReservationState : BaseReservationState
    {
        private readonly IValidator<ReservationInsertRequest> _insertValidator;
        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly ZooClock _zooClock;

        public InitialReservationState(
            ECommerceDbContext dbContext,
            IMapper mapper,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            IValidator<ReservationInsertRequest> insertValidator,
            IAuthenticatedUserAccessor userAccessor,
            ZooClock zooClock)
            : base(dbContext, mapper, serviceProvider, loggerFactory)
        {
            _insertValidator = insertValidator;
            _userAccessor = userAccessor;
            _zooClock = zooClock;
        }

        public override async Task<ReservationResponse> InsertAsync(ReservationInsertRequest request)
        {
            var validationResult = await _insertValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var schedule = await DbContext.Schedules
                .Include(s => s.Activity)
                .FirstOrDefaultAsync(s => s.Id == request.ScheduleId);

            if (schedule == null || schedule.Activity == null)
            {
                throw new ClientException("Schedule not found.");
            }

            if (schedule.Status != ScheduleStatus.Active)
            {
                throw new ClientException("This schedule is not active.");
            }

            var startsAt = schedule.Date.ToDateTime(schedule.StartTime);
            if (startsAt < _zooClock.ToLocal(DateTime.UtcNow))
            {
                throw new ClientException("This schedule is in the past.");
            }

            var userId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");

            if (await DbContext.Reservations.AnyAsync(r =>
                    r.UserId == userId
                    && r.ScheduleId == request.ScheduleId
                    && r.Status != ReservationStatus.Cancelled))
            {
                throw new ClientException("You already have a booking for this schedule.");
            }

            Reservation entity;
            await using var transaction = await DbContext.Database.BeginTransactionAsync();
            try
            {
                var claimed = await DbContext.Schedules
                    .Where(s => s.Id == schedule.Id
                        && s.Status == ScheduleStatus.Active
                        && s.CapacityLeft >= request.GuestCount)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(
                        s => s.CapacityLeft,
                        s => s.CapacityLeft - request.GuestCount));

                if (claimed != 1)
                {
                    throw new ClientException("Not enough spots available.");
                }

                DbContext.Entry(schedule).Property(s => s.CapacityLeft).IsModified = false;

                entity = Mapper.Map<Reservation>(request);
                entity.UserId = userId;
                entity.Status = ReservationStatus.Pending;
                entity.TotalPrice = request.GuestCount * schedule.Activity.Price;
                entity.Code = await GenerateUniqueCodeAsync();
                entity.CreatedAt = DateTime.UtcNow;

                DbContext.Reservations.Add(entity);
                await DbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex) when (IsDuplicateBooking(ex))
            {
                await transaction.RollbackAsync();
                throw new ClientException("You already have a booking for this schedule.");
            }
            catch
            {
                if (DbContext.Database.CurrentTransaction != null)
                {
                    await transaction.RollbackAsync();
                }

                throw;
            }

            return await MapResponseAsync(entity);
        }

        private static bool IsDuplicateBooking(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            return message.Contains("IX_Reservations_UserId_ScheduleId_Active", StringComparison.OrdinalIgnoreCase)
                || message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase);
        }

        public override List<string> GetAllowedActions()
        {
            return base.GetAllowedActions().Append(nameof(InsertAsync)).ToList();
        }
    }
}
