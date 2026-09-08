using System.Text.Json.Serialization;

namespace eCommerce.Model.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReservationStatus
    {
        Pending = 0,
        Confirmed = 1,
        Completed = 2,
        Cancelled = 3
    }
}
