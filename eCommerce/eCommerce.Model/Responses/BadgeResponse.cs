using eCommerce.Model.Enums;

namespace eCommerce.Model.Responses
{
    public class BadgeResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public BadgeCriteria Criteria { get; set; }
        public int HolderCount { get; set; }
    }
}
