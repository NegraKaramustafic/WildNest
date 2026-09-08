using System.Text.Json.Serialization;

namespace eCommerce.Model.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ActivityType
    {
        Feeding = 0,
        Safari = 1,
        Workshop = 2,
        Experience = 3,
        Adventure = 4
    }
}
