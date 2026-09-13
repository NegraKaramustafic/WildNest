using System.Text.Json.Serialization;

namespace eCommerce.Model.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentRelatedEntityType
    {
        Ticket = 0,
        Reservation = 1,
        Adoption = 2,
        Donation = 3
    }
}
