using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using eCommerce.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : BaseCRUDController<UserResponse, UserSearch, UserInsertRequest, UserUpdateRequest, IUserService>
{
    public UsersController(IUserService userService) : base(userService)
    {
    }

    [Authorize]
    [Authorization("Admin")]
    public override Task<PageResult<UserResponse>> GetAll([FromQuery] UserSearch? search)
    {
        return base.GetAll(search);
    }

    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public override Task<ActionResult<UserResponse>> GetById(int id)
    {
        return base.GetById(id);
    }

    [HttpPost]
    [Authorize]
    [Authorization("Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public override async Task<ActionResult<UserResponse>> Create([FromBody] UserInsertRequest request)
    {
        var result = await _service.InsertAsync(request);
        return result;
    }

    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public override async Task<ActionResult<UserResponse>> Update(int id, [FromBody] UserUpdateRequest request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request);
            return result;
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    [Authorization("Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public override async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [Authorize]
    [HttpPut("ChangePassword")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword([FromBody] UserPasswordChangeRequest request)
    {
        try
        {
            await _service.ChangePasswordAsync(request);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
