using eCommerce.Model.Enums;

namespace eCommerce.Model.Responses
{
    public class AnimalResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Species { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Habitat { get; set; }
        public string? Diet { get; set; }
        public decimal Weight { get; set; }
        public string? Lifespan { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public AnimalTag Tag { get; set; }
        public bool IsActive { get; set; }
        public decimal? AdoptionPrice { get; set; }
        public int ActiveAdoptionCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
