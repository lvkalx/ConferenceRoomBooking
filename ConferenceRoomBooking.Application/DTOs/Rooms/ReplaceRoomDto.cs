namespace ConferenceRoomBooking.Application.DTOs.Rooms;

/// <summary>
/// Повна заміна ресурсу (PUT). На відміну від UpdateRoomDto, всі поля обов'язкові:
/// клієнт присилає повний стан залу, включно з повним переліком послуг —
/// усе, що не передано в Services, буде видалено.
/// </summary>
public class ReplaceRoomDto
{
    public string Name { get; set; } = null!;
    public int Capacity { get; set; }
    public decimal BaseHourlyRate { get; set; }
    public List<CreateServiceDto> Services { get; set; } = new();
}