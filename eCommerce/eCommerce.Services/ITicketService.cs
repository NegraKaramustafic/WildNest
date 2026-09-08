using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface ITicketService : IBaseCRUDService<TicketResponse, TicketSearchObject, TicketInsertRequest, TicketUpdateRequest>
    {
        Task<TicketResponse> VerifyAsync(TicketVerifyRequest request);
    }
}
