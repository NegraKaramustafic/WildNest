using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface IAnimalCategoryService : IBaseCRUDService<AnimalCategoryResponse, AnimalCategorySearchObject, AnimalCategoryInsertRequest, AnimalCategoryUpdateRequest>
    {
    }
}
