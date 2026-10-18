using eCommerce.Model.Enums;
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

        public InitialReservationState(
            ECommerceDbContext dbContext,
            IMapper mapper,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            IValidator<ReservationInsertRequest> insertValidator,
            IAuthenticatedUserAccessor userAccessor)
            : base(dbContext, mapper, serviceProvider, loggerFactory)
        {
            _insertValidator = insertValidator;
            _userAccessor = userAccessor;
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

            if (schedule == null)
            {
                throw new ClientException("Schedule not found.");
            }

            if (schedule.CapacityLeft < request.GuestCount)
            {
                throw new ClientException("Not enough spots available.");
            }

            schedule.CapacityLeft -= request.GuestCount;

            var entity = Mapper.Map<Reservation>(request);
            entity.UserId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");
            entity.Status = ReservationStatus.Pending;
            entity.TotalPrice = request.GuestCount * schedule.Activity.Price;
            entity.Code = await GenerateUniqueCodeAsync();
            entity.CreatedAt = DateTime.UtcNow;

            DbContext.Reservations.Add(entity);
            await DbContext.SaveChangesAsync();
            return await MapResponseAsync(entity);
        }

        public override List<string> GetAllowedActions()
        {
            return base.GetAllowedActions().Append(nameof(InsertAsync)).ToList();
        }
    }
}
