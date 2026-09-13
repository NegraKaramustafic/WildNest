using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface IPaymentService : IBaseCRUDService<PaymentResponse, PaymentSearchObject, PaymentInsertRequest, PaymentUpdateRequest>
    {
        Task<PaymentCheckoutResponse> CreateCheckoutAsync(PaymentCheckoutRequest request);
        Task HandleWebhookAsync(string json, string? stripeSignature);
        Task<PaymentResponse> RefundAsync(int id);
        /// <param name="refundIfCompleted">
        /// True for refundable bookings (Ticket/Reservation). False for Adoption: a completed
        /// virtual-adoption charge is a non-refundable contribution by design.
        /// </param>
        Task HandleEntityCancellationAsync(int paymentId, bool refundIfCompleted = true);
    }
}
