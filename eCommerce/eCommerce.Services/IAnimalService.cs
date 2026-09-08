using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface IAnimalService : IBaseCRUDService<AnimalResponse, AnimalSearchObject, AnimalInsertRequest, AnimalUpdateRequest>
    {
    }
}
