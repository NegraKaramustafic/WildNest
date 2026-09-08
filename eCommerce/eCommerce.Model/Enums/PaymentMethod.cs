using System.Text.Json.Serialization;

namespace eCommerce.Model.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentMethod
    {
        Card = 0,
        PayPal = 1,
        Cash = 2
    }
}
