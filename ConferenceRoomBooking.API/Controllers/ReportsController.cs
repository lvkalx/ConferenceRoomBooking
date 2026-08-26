using ConferenceRoomBooking.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBooking.API.Controllers;

/// <summary>
/// Аналітичні звіти для бізнесу: завантаженість залів, дохід, популярність послуг.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>Звіт завантаженості залів за період (кількість бронювань, % зайнятості).</summary>
    [HttpGet("occupancy")]
    [ProducesResponseType(typeof(RoomOccupancyReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOccupancy(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (to <= from)
            return BadRequest(new { error = "Дата 'to' має бути пізніше 'from'." });

        var report = await _reportService.GetOccupancyReportAsync(from, to, ct);
        return Ok(report);
    }

    /// <summary>Звіт доходу за період, загалом та по кожному залу.</summary>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(RevenueReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRevenue(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (to <= from)
            return BadRequest(new { error = "Дата 'to' має бути пізніше 'from'." });

        var report = await _reportService.GetRevenueReportAsync(from, to, ct);
        return Ok(report);
    }

    /// <summary>Звіт популярності послуг за період (скільки разів обирали, дохід).</summary>
    [HttpGet("popular-services")]
    [ProducesResponseType(typeof(List<PopularServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPopularServices(
        [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (to <= from)
            return BadRequest(new { error = "Дата 'to' має бути пізніше 'from'." });

        var report = await _reportService.GetPopularServicesAsync(from, to, ct);
        return Ok(report);
    }
}