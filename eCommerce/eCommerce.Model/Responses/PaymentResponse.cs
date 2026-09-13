using eCommerce.Model.Enums;

namespace eCommerce.Model.Responses
{
    public class PaymentResponse
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
        public string? StripePaymentIntentId { get; set; }
        public string? StripeCheckoutSessionId { get; set; }
        public string? StripeRefundId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
