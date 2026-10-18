using System.Text.Json.Serialization;

namespace eCommerce.Model.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NotificationType
    {
        TicketPurchased = 0,
        TicketVerified = 1,
        ReservationCreated = 2,
        ReservationConfirmed = 3,
        ReservationCancelled = 4,
        AdoptionCreated = 5,
        DonationReceived = 6,
        PaymentCompleted = 7,
        BadgeEarned = 8,
        PasswordReset = 9,
        UpcomingReminder = 10
    }
}
