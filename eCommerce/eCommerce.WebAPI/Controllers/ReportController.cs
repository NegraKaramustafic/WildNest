using eCommerce.Model;
using eCommerce.Model.Requests;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

/// <summary>
/// Administrator PDF reports.
/// </summary>
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/Report")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Reservations created in a period, with optional activity and status filters.
    /// </summary>
    [HttpGet("reservations")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reservations([FromQuery] ReservationReportRequest request)
    {
        var pdf = await _reportService.ReservationsPdfAsync(request);
        return File(pdf.Content, "application/pdf", pdf.FileName);
    }

    /// <summary>
    /// Donation totals grouped by zoo partner for a period.
    /// </summary>
    [HttpGet("donations")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Donations([FromQuery] DonationReportRequest request)
    {
        var pdf = await _reportService.DonationsPdfAsync(request);
        return File(pdf.Content, "application/pdf", pdf.FileName);
    }
}
