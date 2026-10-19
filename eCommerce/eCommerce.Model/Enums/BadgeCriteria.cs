using System.Text.Json.Serialization;

namespace eCommerce.Model.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BadgeCriteria
    {
        FirstAdoption = 0,
        FirstBooking = 1,
        ThreeDonations = 2,
        // 3 was TenAnimalVisits. The number stays unused so FiveAdoptions remains 4.
        FiveAdoptions = 4
    }
}
