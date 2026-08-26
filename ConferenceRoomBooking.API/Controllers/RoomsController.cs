using ConferenceRoomBooking.Application.Common;
using ConferenceRoomBooking.Application.DTOs.Bookings;
using ConferenceRoomBooking.Application.DTOs.Rooms;
using ConferenceRoomBooking.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBooking.API.Controllers;

/// <summary>
/// Управління конференц-залами: створення, редагування, видалення, пошук доступних.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    /// <summary>Отримати зал за ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _roomService.GetByIdAsync(id, ct);
        return result.ToActionResult();
    }

    /// <summary>Додати новий конференц-зал з переліком доступних послуг.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRoomDto dto, CancellationToken ct)
    {
        var result = await _roomService.CreateAsync(dto, ct);

        if (!result.IsSuccess)
            return result.ToActionResult();

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Часткове оновлення залу: змінює лише передані поля
    /// (наприклад, лише ціну, або лише додає одну послугу). Не ідемпотентно
    /// у загальному випадку (ServicesToAdd повторно застосований додасть дубль,
    /// якщо не покладатись на ідемпотентність AddService в домені).
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoomDto dto, CancellationToken ct)
    {
        var result = await _roomService.UpdateAsync(id, dto, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Повна заміна залу: клієнт присилає весь стан ресурсу, включно
    /// з повним переліком послуг. Усе, що не передано в Services, буде видалено.
    /// Ідемпотентно: повторний виклик з тим самим тілом дає той самий результат.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Replace(Guid id, [FromBody] ReplaceRoomDto dto, CancellationToken ct)
    {
        var result = await _roomService.ReplaceAsync(id, dto, ct);
        return result.ToActionResult();
    }

    /// <summary>Видалити конференц-зал (soft delete).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _roomService.DeleteAsync(id, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Пошук доступних залів за датою/часом та мінімальною місткістю.
    /// GET, бо операція не змінює стан (safe) і є ідемпотентною — ідеально кешується
    /// і посилання на конкретний пошук можна зберегти/переслати.
    /// </summary>
    [HttpGet("available")]
    [ProducesResponseType(typeof(List<RoomDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchAvailable([FromQuery] AvailabilityRequestDto dto, CancellationToken ct)
    {
        var result = await _roomService.GetAvailableAsync(dto, ct);
        return result.ToActionResult();
    }
}