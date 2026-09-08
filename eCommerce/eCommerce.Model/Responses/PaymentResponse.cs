using eCommerce.Model.Enums;

namespace eCommerce.Model.Responses
{
    public class PaymentResponse
    {
        public int Id { get; set; }
        public PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public string? StripePaymentIntentId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
