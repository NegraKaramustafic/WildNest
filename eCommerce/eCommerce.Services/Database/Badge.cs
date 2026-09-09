using System.ComponentModel.DataAnnotations;
using eCommerce.Model.Enums;

namespace eCommerce.Services.Database
{
    public class Badge
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? IconUrl { get; set; }

        public BadgeCriteria Criteria { get; set; }
    }
}
