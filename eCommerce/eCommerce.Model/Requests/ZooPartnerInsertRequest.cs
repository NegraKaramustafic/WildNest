using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class ZooPartnerInsertRequest
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

        /// <summary>
        /// PNG or JPEG payload. When set, it replaces ImageUrl.
        /// </summary>
        public string? ImageBase64 { get; set; }

        /// <summary>
        /// image/png or image/jpeg. Required together with ImageBase64.
        /// </summary>
        public string? ImageContentType { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Goal amount must be greater than 0.")]
        public decimal GoalAmount { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
