using System.Text.Json.Serialization;

namespace eCommerce.Model.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TicketVerificationMethod
    {
        ManualCode = 0,
        QrScan = 1
    }
}
