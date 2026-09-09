using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface IBadgeService : IBaseCRUDService<BadgeResponse, BadgeSearchObject, BadgeInsertRequest, BadgeUpdateRequest>
    {
    }
}
