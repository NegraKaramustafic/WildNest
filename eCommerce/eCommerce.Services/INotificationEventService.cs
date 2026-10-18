using eCommerce.Model.Responses;

namespace eCommerce.Services
{
    public interface INotificationEventService
    {
        Task NotifyTicketPurchasedAsync(TicketResponse ticket);
        Task NotifyTicketVerifiedAsync(TicketResponse ticket);
        Task NotifyReservationCreatedAsync(ReservationResponse reservation);
        Task NotifyReservationConfirmedAsync(ReservationResponse reservation);
        Task NotifyReservationCancelledAsync(ReservationResponse reservation);
        Task NotifyAdoptionCreatedAsync(AdoptionResponse adoption);
        Task NotifyAdoptionRejectedAsync(AdoptionResponse adoption);
        Task NotifyDonationReceivedAsync(DonationResponse donation);
        Task NotifyPaymentCompletedAsync(int userId, int paymentId, decimal amount);
        Task NotifyBadgeEarnedAsync(int userId, int badgeId, string badgeName);
    }
}
