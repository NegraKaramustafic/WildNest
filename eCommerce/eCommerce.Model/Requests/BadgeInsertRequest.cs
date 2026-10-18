using System.ComponentModel.DataAnnotations;
using eCommerce.Model.Enums;

namespace eCommerce.Model.Requests
{
    public class BadgeInsertRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? IconUrl { get; set; }

        /// <summary>
        /// PNG or JPEG payload. When set, it replaces IconUrl.
        /// </summary>
        public string? ImageBase64 { get; set; }

        /// <summary>
        /// image/png or image/jpeg. Required together with ImageBase64.
        /// </summary>
        public string? ImageContentType { get; set; }

        [Required]
        public BadgeCriteria Criteria { get; set; }
    }
}
