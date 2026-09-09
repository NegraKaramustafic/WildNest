using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class ZooPartnerUpdateRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "GoalAmount must be greater than 0.")]
        public decimal GoalAmount { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
