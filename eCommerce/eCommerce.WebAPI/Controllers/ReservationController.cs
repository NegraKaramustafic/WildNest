using eCommerce.Model;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Authorize]
[Route("api/Reservation")]
public class ReservationController : BaseCRUDController<ReservationResponse, ReservationSearchObject, ReservationInsertRequest, ReservationUpdateRequest, IReservationService>
{
    public ReservationController(IReservationService reservationService) : base(reservationService)
    {
    }

    [HttpPost("{id}/confirm")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> Confirm(int id)
    {
        var result = await _service.ConfirmAsync(id);
        return Ok(result);
    }

    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> Cancel(int id)
    {
        var result = await _service.CancelAsync(id);
        return Ok(result);
    }

    [HttpPost("{id}/cancel-with-reason")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> CancelWithReason(int id, [FromBody] ReservationCancelRequest request)
    {
        var result = await _service.CancelWithReasonAsync(id, request);
        return Ok(result);
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> Complete(int id)
    {
        var result = await _service.CompleteAsync(id);
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
