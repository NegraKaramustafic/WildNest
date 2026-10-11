using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using eCommerce.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Route("api/Animal")]
public class AnimalController : BaseReadController<AnimalResponse, AnimalSearchObject, IAnimalService>
{
    public AnimalController(IAnimalService animalService) : base(animalService)
    {
    }

    [HttpPost]
    [Authorize]
    [Authorization("Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AnimalResponse>> Create([FromBody] AnimalInsertRequest request)
    {
        var result = await _service.InsertAsync(request);
        return result;
    }

    [HttpPut("{id}")]
    [Authorize]
    [Authorization("Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AnimalResponse>> Update(int id, [FromBody] AnimalUpdateRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return result;
    }

    [HttpDelete("{id}")]
    [Authorize]
    [Authorization("Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
