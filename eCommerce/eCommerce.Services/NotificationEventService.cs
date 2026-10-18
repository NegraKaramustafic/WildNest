using System.Security.Cryptography;
using System.Text;
using eCommerce.Model.Enums;
using eCommerce.Model;
using eCommerce.Model.Messages;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services
{
    public class NotificationEventService : INotificationEventService
    {

        private readonly INotificationPublisher _publisher;
        private readonly ECommerceDbContext _dbContext;

        public NotificationEventService(INotificationPublisher publisher, ECommerceDbContext dbContext)
        {
            _publisher = publisher;
            _dbContext = dbContext;
        }

        public Task NotifyTicketPurchasedAsync(TicketResponse ticket)
        {
            return PublishAsync(
                ticket.UserId,
                NotificationType.TicketPurchased,
                "Ticket purchased",
                $"Your zoo entry ticket {ticket.Code} is ready for {ticket.VisitDate:yyyy-MM-dd}.",
                "Ticket",
                ticket.Id);
        }

        public Task NotifyTicketVerifiedAsync(TicketResponse ticket)
        {
            return PublishAsync(
                ticket.UserId,
                NotificationType.TicketVerified,
                "Ticket verified",
                $"Ticket {ticket.Code} was verified at the gate.",
                "Ticket",
                ticket.Id);
        }

        public async Task NotifyReservationCreatedAsync(ReservationResponse reservation)
        {
            await PublishAsync(
                reservation.UserId,
                NotificationType.ReservationCreated,
                "Reservation created",
                $"Your booking for {reservation.ActivityName} is pending confirmation. Code: {reservation.Code}.",
                "Reservation",
                reservation.Id);

            var adminMessage =
                $"{reservation.UserFullName} booked {reservation.ActivityName} ({reservation.Code}).";
            foreach (var adminId in await GetAdminUserIdsAsync())
            {
                if (adminId == reservation.UserId)
                {
                    continue;
                }

                await PublishAsync(
                    adminId,
                    NotificationType.ReservationCreated,
                    "New reservation pending",
                    adminMessage,
                    "Reservation",
                    reservation.Id);
            }
        }

        public Task NotifyReservationConfirmedAsync(ReservationResponse reservation)
        {
            return PublishAsync(
                reservation.UserId,
                NotificationType.ReservationConfirmed,
                "Reservation confirmed",
                $"Your booking for {reservation.ActivityName} ({reservation.Code}) is confirmed.",
                "Reservation",
                reservation.Id,
                MessageIdFor($"wildnest:reservation:{reservation.Id}:confirmed"));
        }

        public Task NotifyReservationCancelledAsync(ReservationResponse reservation)
        {
            var message = $"Your booking for {reservation.ActivityName} ({reservation.Code}) was cancelled.";
            var reason = reservation.CancellationReason?.Trim();
            if (!string.IsNullOrEmpty(reason))
            {
                if (!reason.EndsWith('.'))
                {
                    reason += ".";
                }

                message = $"Your booking for {reservation.ActivityName} ({reservation.Code}) was cancelled. Reason: {reason}";
            }

            return PublishAsync(
                reservation.UserId,
                NotificationType.ReservationCancelled,
                "Reservation cancelled",
                message,
                "Reservation",
                reservation.Id,
                MessageIdFor($"wildnest:reservation:{reservation.Id}:cancelled"));
        }

        public Task NotifyAdoptionCreatedAsync(AdoptionResponse adoption)
        {
            return PublishAsync(
                adoption.UserId,
                NotificationType.AdoptionCreated,
                "Adoption started",
                $"You are now supporting {adoption.AnimalName}.",
                "Adoption",
                adoption.Id);
        }

        public Task NotifyAdoptionRejectedAsync(AdoptionResponse adoption)
        {
            var reason = adoption.RejectionReason?.Trim() ?? string.Empty;
            if (!reason.EndsWith('.'))
            {
                reason += ".";
            }

            return PublishAsync(
                adoption.UserId,
                NotificationType.AdoptionRejected,
                "Adoption rejected",
                $"Your adoption of {adoption.AnimalName} was rejected. Reason: {reason}",
                "Adoption",
                adoption.Id,
                MessageIdFor($"wildnest:adoption:{adoption.Id}:rejected"));
        }

        public Task NotifyDonationReceivedAsync(DonationResponse donation)
        {
            return PublishAsync(
                donation.UserId,
                NotificationType.DonationReceived,
                "Donation received",
                $"Thank you for donating {MoneyFormat.Format(donation.Amount)} to {donation.ZooPartnerName}.",
                "Donation",
                donation.Id);
        }

        public Task NotifyPaymentCompletedAsync(int userId, int paymentId, decimal amount)
        {
            return PublishAsync(
                userId,
                NotificationType.PaymentCompleted,
                "Payment completed",
                $"Your payment of {MoneyFormat.Format(amount)} was completed.",
                "Payment",
                paymentId);
        }

        public Task NotifyBadgeEarnedAsync(int userId, int badgeId, string badgeName)
        {
            return PublishAsync(
                userId,
                NotificationType.BadgeEarned,
                "Badge earned",
                $"You earned the {badgeName} badge.",
                "Badge",
                badgeId);
        }

        private async Task PublishAsync(
            int userId,
            NotificationType type,
            string title,
            string message,
            string relatedEntityType,
            int relatedEntityId,
            Guid? messageId = null)
        {
            await _publisher.PublishAsync(new NotificationMessage
            {
                MessageId = messageId ?? Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId
            });
        }

        private static Guid MessageIdFor(string scope)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(scope));
            return new Guid(hash.AsSpan(0, 16));
        }

        private Task<List<int>> GetAdminUserIdsAsync()
        {
            return _dbContext.UserRoles
                .Where(ur => ur.Role.Name == Roles.Admin)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();
        }
    }
}
