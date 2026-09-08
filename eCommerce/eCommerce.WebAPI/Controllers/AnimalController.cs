using eCommerce.Services;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Model.Requests;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Route("api/Animal")]
public class AnimalController : BaseCRUDController<AnimalResponse, AnimalSearchObject, AnimalInsertRequest, AnimalUpdateRequest, IAnimalService>
{
    public AnimalController(IAnimalService animalService) : base(animalService)
    {
    }
}
