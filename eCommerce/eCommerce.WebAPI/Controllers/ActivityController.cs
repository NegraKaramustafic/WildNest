using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Route("api/Activity")]
public class ActivityController : BaseCRUDController<ActivityResponse, ActivitySearchObject, ActivityInsertRequest, ActivityUpdateRequest, IActivityService>
{
    public ActivityController(IActivityService activityService) : base(activityService)
    {
    }

    [HttpPost("{id}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActivityResponse>> Activate(int id)
    {
        var result = await _service.ActivateAsync(id);
        return Ok(result);
    }

    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActivityResponse>> Deactivate(int id)
    {
        var result = await _service.DeactivateAsync(id);
        return Ok(result);
    }

    [HttpGet("{id}/AllowedActions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<string>>> GetAllowedActions(int id)
    {
        var result = await _service.GetAllowedActionsAsync(id);
        return Ok(result);
    }
}
