using eCommerce.Model.Enums;

namespace eCommerce.Model.Responses
{
    public class ActivityResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? AnimalId { get; set; }
        public string? AnimalName { get; set; }
        public int ActivityTypeId { get; set; }
        public string ActivityTypeName { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public decimal Price { get; set; }
        public int Capacity { get; set; }
        public string? ImageUrl { get; set; }
        public ActivityStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> AllowedActions { get; set; } = new List<string>();
    }
}
