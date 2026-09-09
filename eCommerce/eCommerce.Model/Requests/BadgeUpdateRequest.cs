using System.ComponentModel.DataAnnotations;
using eCommerce.Model.Enums;

namespace eCommerce.Model.Requests
{
    public class BadgeUpdateRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? IconUrl { get; set; }

        [Required]
        public BadgeCriteria Criteria { get; set; }
    }
}
