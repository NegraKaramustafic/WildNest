using System.Text.Json.Serialization;

namespace eCommerce.Model.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AnimalTag
    {
        None = 0,
        Popular = 1,
        New = 2,
        Adoptable = 3
    }
}
