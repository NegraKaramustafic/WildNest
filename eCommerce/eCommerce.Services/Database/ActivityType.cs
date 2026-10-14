using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace eCommerce.Services.Database
{
    public class ActivityType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
