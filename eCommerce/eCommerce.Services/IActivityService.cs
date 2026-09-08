using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface IActivityService : IBaseCRUDService<ActivityResponse, ActivitySearchObject, ActivityInsertRequest, ActivityUpdateRequest>
    {
        Task<ActivityResponse> ActivateAsync(int id);
        Task<ActivityResponse> DeactivateAsync(int id);
        Task<List<string>> GetAllowedActionsAsync(int id);
    }
}
