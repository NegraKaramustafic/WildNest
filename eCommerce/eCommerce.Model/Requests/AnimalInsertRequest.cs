using System.ComponentModel.DataAnnotations;
using eCommerce.Model.Enums;

namespace eCommerce.Model.Requests
{
    public class AnimalInsertRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Species { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        public string? Habitat { get; set; }

        public string? Diet { get; set; }

        public decimal Weight { get; set; }

        public string? Lifespan { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public AnimalTag Tag { get; set; } = AnimalTag.None;

        public bool IsActive { get; set; } = true;

        public decimal? AdoptionPrice { get; set; }
    }
}
