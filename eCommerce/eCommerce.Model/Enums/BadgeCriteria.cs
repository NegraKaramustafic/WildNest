using System.Text.Json.Serialization;

namespace eCommerce.Model.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BadgeCriteria
    {
        FirstAdoption = 0,
        FirstBooking = 1,
        DonationTotal = 2,
        // 3 was TenAnimalVisits. The number stays unused so FiveAdoptions remains 4.
        FiveAdoptions = 4
    }

    public static class DonationBadgeRule
    {
        public const decimal TotalEuros = 50m;
        public const string Description = "Donate a total of 50 EUR";
    }
}
