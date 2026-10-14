using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Authorize]
[Route("api/Adoption")]
public class AdoptionController : BaseCRUDController<AdoptionResponse, AdoptionSearchObject, AdoptionInsertRequest, AdoptionUpdateRequest, IAdoptionService>
{
    public AdoptionController(IAdoptionService adoptionService) : base(adoptionService)
    {
    }

    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdoptionResponse>> Cancel(int id)
    {
        var result = await _service.CancelAsync(id);
        return Ok(result);
    }
}
