using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface IReservationService : IBaseCRUDService<ReservationResponse, ReservationSearchObject, ReservationInsertRequest, ReservationUpdateRequest>
    {
        Task<ReservationResponse> ConfirmAsync(int id);
        Task<ReservationResponse> CancelAsync(int id);
        Task<ReservationResponse> CompleteAsync(int id);
        Task<List<string>> GetAllowedActionsAsync(int id);
    }
}
