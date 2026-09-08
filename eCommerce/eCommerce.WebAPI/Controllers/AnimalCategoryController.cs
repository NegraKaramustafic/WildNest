using eCommerce.Services;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Model.Requests;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Route("api/AnimalCategory")]
public class AnimalCategoryController : BaseCRUDController<AnimalCategoryResponse, AnimalCategorySearchObject, AnimalCategoryInsertRequest, AnimalCategoryUpdateRequest, IAnimalCategoryService>
{
    public AnimalCategoryController(IAnimalCategoryService animalCategoryService) : base(animalCategoryService)
    {
    }
}
