using eCommerce.Services.Images;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("media")]
public class MediaController : ControllerBase
{
    private readonly IStoredImageService _images;

    public MediaController(IStoredImageService images)
    {
        _images = images;
    }

    [HttpGet("{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Get(string fileName)
    {
        var file = _images.Read(fileName);
        if (file == null)
        {
            return NotFound(new { message = "Image not found." });
        }

        return File(file.Bytes, file.ContentType);
    }
}
