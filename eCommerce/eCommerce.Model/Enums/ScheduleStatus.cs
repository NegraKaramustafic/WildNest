using System.Text.Json.Serialization;

namespace eCommerce.Model.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ScheduleStatus
    {
        Active = 0,
        Cancelled = 1
    }
}
