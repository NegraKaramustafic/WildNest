using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface IPaymentService : IBaseCRUDService<PaymentResponse, PaymentSearchObject, PaymentInsertRequest, PaymentUpdateRequest>
    {
        Task<PaymentResponse> CompleteAsync(int id);
        Task<PaymentResponse> FailAsync(int id);
    }
}
