using eCommerce.Model.Enums;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using eCommerce.Services.ReservationStateMachine;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services
{
    public class ReservationHoldService : IReservationHoldService
    {
        public const string PaymentTimeoutReason = "Payment was not completed in time.";
        public static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(30);

        private readonly ECommerceDbContext _dbContext;
        private readonly BaseReservationState _reservationState;
        private readonly IPaymentService _paymentService;
        private readonly INotificationEventService _notificationEvents;

        public ReservationHoldService(
            ECommerceDbContext dbContext,
            BaseReservationState reservationState,
            IPaymentService paymentService,
            INotificationEventService notificationEvents)
        {
            _dbContext = dbContext;
            _reservationState = reservationState;
            _paymentService = paymentService;
            _notificationEvents = notificationEvents;
        }

        public async Task<int> ReleaseExpiredHoldsAsync(DateTime utcNow, CancellationToken cancellationToken = default)
        {
            var cutoff = utcNow.Subtract(HoldDuration);
            var ids = await _dbContext.Reservations
                .AsNoTracking()
                .Where(reservation => reservation.Status == ReservationStatus.Pending && reservation.CreatedAt <= cutoff)
                .Where(reservation => reservation.PaymentId == null
                    || reservation.Payment!.Status == PaymentStatus.Pending
                    || reservation.Payment.Status == PaymentStatus.Failed)
                .Select(reservation => reservation.Id)
                .ToListAsync(cancellationToken);

            var released = 0;
            foreach (var id in ids)
            {
                if (await ReleaseOneAsync(id, utcNow, cancellationToken))
                {
                    released++;
                }
            }

            return released;
        }

        internal static bool IsExpiredUnpaidHold(Reservation reservation, DateTime utcNow)
        {
            if (reservation.Status != ReservationStatus.Pending)
            {
                return false;
            }

            if (reservation.CreatedAt > utcNow.Subtract(HoldDuration))
            {
                return false;
            }

            if (!reservation.PaymentId.HasValue)
            {
                return true;
            }

            return reservation.Payment?.Status is PaymentStatus.Pending or PaymentStatus.Failed;
        }

        private async Task<bool> ReleaseOneAsync(int id, DateTime utcNow, CancellationToken cancellationToken)
        {
            var entity = await _dbContext.Reservations
                .Include(reservation => reservation.Payment)
                .FirstOrDefaultAsync(reservation => reservation.Id == id, cancellationToken);
            if (entity == null || !IsExpiredUnpaidHold(entity, utcNow))
            {
                return false;
            }

            ReservationResponse? cancelled = null;
            await DbTransactions.ExecuteAsync(_dbContext, async () =>
            {
                await _dbContext.Entry(entity).ReloadAsync(cancellationToken);
                if (entity.PaymentId.HasValue)
                {
                    await _dbContext.Entry(entity).Reference(reservation => reservation.Payment).LoadAsync(cancellationToken);
                }
                else
                {
                    entity.Payment = null;
                }

                if (!IsExpiredUnpaidHold(entity, utcNow))
                {
                    return;
                }

                entity.CancelledByUserId = null;
                entity.CancelledAt = DateTime.UtcNow;
                entity.CancellationReason = PaymentTimeoutReason;
                var paymentId = entity.PaymentId;

                var state = _reservationState.GetReservationState(entity.Status);
                cancelled = await state.CancelAsync(entity.Id);
                if (paymentId.HasValue)
                {
                    await _paymentService.HandleEntityCancellationAsync(paymentId.Value);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            });

            if (cancelled == null)
            {
                return false;
            }

            await _notificationEvents.NotifyReservationCancelledAsync(cancelled);
            return true;
        }
    }
}
