using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class ActivityTypeInsertRequest
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
