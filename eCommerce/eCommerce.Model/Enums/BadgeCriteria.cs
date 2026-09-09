using System.Text.Json.Serialization;

namespace eCommerce.Model.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BadgeCriteria
    {
        FirstAdoption = 0,
        FirstBooking = 1,
        ThreeDonations = 2,
        TenAnimalVisits = 3,
        FiveAdoptions = 4
    }
}
