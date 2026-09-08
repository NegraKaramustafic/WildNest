using System.ComponentModel.DataAnnotations;
using eCommerce.Model.Enums;

namespace eCommerce.Model.Requests
{
    public class PaymentUpdateRequest
    {
        [Required]
        public PaymentMethod Method { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [MaxLength(255)]
        public string? StripePaymentIntentId { get; set; }
    }
}
