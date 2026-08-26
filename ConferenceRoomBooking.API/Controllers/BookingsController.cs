using ConferenceRoomBooking.Application.Common;
using ConferenceRoomBooking.Application.DTOs.Bookings;
using ConferenceRoomBooking.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBooking.API.Controllers;

/// <summary>
/// Бронювання конференц-залів з розрахунком вартості.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>Отримати бронювання за ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _bookingService.GetByIdAsync(id, ct);
        return result.ToActionResult();
    }


    /// <summary>
    /// Забронювати зал на вказаний період з переліком послуг.
    /// Розраховує загальну вартість з урахуванням тарифних коефіцієнтів.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateBookingDto dto, CancellationToken ct)
    {
        var result = await _bookingService.CreateBookingAsync(dto, ct);

        if (!result.IsSuccess)
            return result.ToActionResult();

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Скасовує бронювання як бізнес-операцію та переводить його у відповідний стан.
    /// На відміну від CRUD-операцій, скасування може виконувати додаткову бізнес-логіку,
    /// зокрема оновлення доступності конференц-залу.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _bookingService.CancelBookingAsync(id, ct);
        return result.ToActionResult();
    }
}