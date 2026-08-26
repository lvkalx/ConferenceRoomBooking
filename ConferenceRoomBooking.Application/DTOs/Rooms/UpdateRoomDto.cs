namespace ConferenceRoomBooking.Application.DTOs.Rooms;

/// <summary>
/// Всі поля опційні (nullable) — оновлюємо лише те, що передано (partial update).
/// </summary>
public class UpdateRoomDto
{
    public string? Name { get; set; }
    public int? Capacity { get; set; }
    public decimal? BaseHourlyRate { get; set; }
    public List<CreateServiceDto>? ServicesToAdd { get; set; }
    public List<Guid>? ServiceIdsToRemove { get; set; }
}