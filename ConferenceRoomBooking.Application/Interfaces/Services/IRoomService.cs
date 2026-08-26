using ConferenceRoomBooking.Application.Common;
using ConferenceRoomBooking.Application.DTOs.Bookings;
using ConferenceRoomBooking.Application.DTOs.Rooms;

namespace ConferenceRoomBooking.Application.Interfaces.Services;

public interface IRoomService
{
    Task<Result<RoomDto>> CreateAsync(CreateRoomDto dto, CancellationToken ct = default);

    /// <summary>Часткове оновлення (PATCH) — змінює лише передані поля.</summary>
    Task<Result> UpdateAsync(Guid id, UpdateRoomDto dto, CancellationToken ct = default);

    /// <summary>Повна заміна ресурсу (PUT) — замінює зал цілком, включно з переліком послуг.</summary>
    Task<Result> ReplaceAsync(Guid id, ReplaceRoomDto dto, CancellationToken ct = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<List<RoomDto>>> GetAvailableAsync(AvailabilityRequestDto dto, CancellationToken ct = default);
    Task<Result<RoomDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
}