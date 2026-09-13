using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class ReviewInsertRequest
    {
        [Required]
        public int ActivityId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        [Required]
        [MaxLength(500)]
        public string Comment { get; set; } = string.Empty;
    }
}
