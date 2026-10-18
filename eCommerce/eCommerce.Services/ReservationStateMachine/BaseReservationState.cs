using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services.ReservationStateMachine
{
    public class BaseReservationState
    {
        private const string CodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int CodeLength = 6;
        private const int MaxCodeAttempts = 20;

        protected ECommerceDbContext DbContext { get; }
        protected IMapper Mapper { get; }
        protected IServiceProvider ServiceProvider { get; }
        protected ILogger Logger { get; }

        public BaseReservationState(
            ECommerceDbContext dbContext,
            IMapper mapper,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory)
        {
            DbContext = dbContext;
            Mapper = mapper;
            ServiceProvider = serviceProvider;
            Logger = loggerFactory.CreateLogger(GetType());
        }

        [DoesNotReturn]
        protected void ThrowNotFound(int id)
        {
            Logger.LogWarning("Reservation {Id} was not found.", id);
            throw new NotFoundException("Reservation not found.");
        }

        public virtual Task<ReservationResponse> InsertAsync(ReservationInsertRequest request)
        {
            throw new ClientException("Cannot insert a reservation in its current state.");
        }

        public virtual Task<ReservationResponse> UpdateAsync(int id, ReservationUpdateRequest request)
        {
            throw new ClientException("Cannot update a reservation in its current state.");
        }

        public virtual Task<ReservationResponse> ConfirmAsync(int id)
        {
            throw new ClientException("Cannot confirm a reservation in its current state.");
        }

        public virtual Task<ReservationResponse> CancelAsync(int id)
        {
            throw new ClientException("Cannot cancel a reservation in its current state.");
        }

        public virtual Task<ReservationResponse> CompleteAsync(int id)
        {
            throw new ClientException("Cannot complete a reservation in its current state.");
        }

        public virtual Task<ReservationResponse> DeleteAsync(int id)
        {
            throw new ClientException("Cannot delete a reservation in its current state.");
        }

        public BaseReservationState GetReservationState(string stateName)
        {
            switch (stateName)
            {
                case nameof(InitialReservationState):
                    return ServiceProvider.GetService<InitialReservationState>()!;
                case nameof(PendingReservationState):
                    return ServiceProvider.GetService<PendingReservationState>()!;
                case nameof(ConfirmedReservationState):
                    return ServiceProvider.GetService<ConfirmedReservationState>()!;
                case nameof(CompletedReservationState):
                    return ServiceProvider.GetService<CompletedReservationState>()!;
                case nameof(CancelledReservationState):
                    return ServiceProvider.GetService<CancelledReservationState>()!;
                default:
                    throw new ClientException($"Unknown reservation state: {stateName}.");
            }
        }

        public BaseReservationState GetReservationState(ReservationStatus status)
        {
            return status switch
            {
                ReservationStatus.Pending => GetReservationState(nameof(PendingReservationState)),
                ReservationStatus.Confirmed => GetReservationState(nameof(ConfirmedReservationState)),
                ReservationStatus.Completed => GetReservationState(nameof(CompletedReservationState)),
                ReservationStatus.Cancelled => GetReservationState(nameof(CancelledReservationState)),
                _ => throw new ClientException($"Unknown reservation status: {status}.")
            };
        }

        public virtual List<string> GetAllowedActions()
        {
            return new List<string>();
        }

        protected async Task RestoreCapacityAsync(Reservation entity)
        {
            var schedule = await DbContext.Schedules.FindAsync(entity.ScheduleId);
            if (schedule != null)
            {
                schedule.CapacityLeft += entity.GuestCount;
            }
        }

        protected async Task<string> GenerateUniqueCodeAsync()
        {
            for (var attempt = 0; attempt < MaxCodeAttempts; attempt++)
            {
                var suffix = new char[CodeLength];
                for (var i = 0; i < CodeLength; i++)
                {
                    suffix[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
                }

                var code = $"WN-{new string(suffix)}";
                if (!await DbContext.Reservations.AnyAsync(r => r.Code == code))
                {
                    return code;
                }
            }

            throw new InvalidOperationException("Unable to generate a unique reservation code.");
        }

        protected async Task<ReservationResponse> MapResponseAsync(Reservation entity)
        {
            if (entity.User == null)
            {
                await DbContext.Entry(entity).Reference(r => r.User).LoadAsync();
            }

            if (entity.Schedule == null)
            {
                await DbContext.Entry(entity).Reference(r => r.Schedule).LoadAsync();
            }

            if (entity.Schedule != null && entity.Schedule.Activity == null)
            {
                await DbContext.Entry(entity.Schedule).Reference(s => s.Activity).LoadAsync();
            }

            if (entity.PaymentId.HasValue && entity.Payment == null)
            {
                await DbContext.Entry(entity).Reference(r => r.Payment).LoadAsync();
            }

            if (entity.ConfirmedByUserId.HasValue && entity.ConfirmedByUser == null)
            {
                await DbContext.Entry(entity).Reference(r => r.ConfirmedByUser).LoadAsync();
            }

            if (entity.CancelledByUserId.HasValue && entity.CancelledByUser == null)
            {
                await DbContext.Entry(entity).Reference(r => r.CancelledByUser).LoadAsync();
            }

            var response = Mapper.Map<ReservationResponse>(entity);
            response.AllowedActions = GetReservationState(entity.Status).GetAllowedActions();
            return response;
        }
    }
}
