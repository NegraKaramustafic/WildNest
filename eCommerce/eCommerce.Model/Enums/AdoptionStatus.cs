using System.Text.Json.Serialization;

namespace eCommerce.Model.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AdoptionStatus
    {
        Active = 0,
        Cancelled = 1
    }
}
