using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eCommerce.Model.Enums;

namespace eCommerce.Services.Database
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        public PaymentMethod Method { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [MaxLength(255)]
        public string? StripePaymentIntentId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
