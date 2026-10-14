using eCommerce.Model;
using eCommerce.Model.Responses;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

/// <summary>
/// Personalized, explainable activity recommendations for the signed-in user.
/// UserId is taken from the JWT — it is not accepted on the query string or body.
/// </summary>
[Authorize]
[ApiController]
[Route("api/Recommendation")]
public class RecommendationController : ControllerBase
{
    private readonly IRecommendationService _service;

    public RecommendationController(IRecommendationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Returns ranked Active activities the caller has not already reserved (Confirmed/Completed).
    /// <paramref name="limit"/> defaults to 5 and must be between 1 and 20.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RecommendationResponse>> Get([FromQuery] int? limit)
    {
        var result = await _service.GetRecommendationsAsync(limit);
        return Ok(result);
    }

    /// <summary>
    /// Retrains the MatrixFactorization model from current Review and Reservation rows. Admin only.
    /// </summary>
    [HttpPost("retrain")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RecommendationRetrainResponse>> Retrain()
    {
        var result = await _service.RetrainAsync();
        return Ok(result);
    }
}
