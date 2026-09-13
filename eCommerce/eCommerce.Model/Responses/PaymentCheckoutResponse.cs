using eCommerce.Model.Enums;

namespace eCommerce.Model.Responses
{
    public class PaymentCheckoutResponse
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public string? ClientSecret { get; set; }
        public string? SessionUrl { get; set; }
        public string? StripePaymentIntentId { get; set; }
        public string? StripeCheckoutSessionId { get; set; }
        public PaymentRelatedEntityType RelatedEntityType { get; set; }
        public int RelatedEntityId { get; set; }
    }
}
