using System.ComponentModel.DataAnnotations;

namespace eCommerce.Services.Database
{
    public class StripeProcessedEvent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string EventId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty;

        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
