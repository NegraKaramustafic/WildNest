using System.Text.Json.Serialization;

namespace eCommerce.Model.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ActivityStatus
    {
        Draft = 0,
        Active = 1,
        Inactive = 2
    }
}
