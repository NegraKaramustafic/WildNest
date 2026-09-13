using System.Text.Json.Serialization;

namespace eCommerce.Model.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TicketStatus
    {
        Active = 0,
        Used = 1,
        Cancelled = 2,
        PendingPayment = 3
    }
}
