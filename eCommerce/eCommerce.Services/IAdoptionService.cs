using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface IAdoptionService : IBaseCRUDService<AdoptionResponse, AdoptionSearchObject, AdoptionInsertRequest, AdoptionUpdateRequest>
    {
        Task<AdoptionResponse> CancelAsync(int id);
        Task<AdoptionResponse> ApproveAsync(int id);
        Task<AdoptionResponse> RejectAsync(int id, AdoptionRejectRequest request);
    }
}
