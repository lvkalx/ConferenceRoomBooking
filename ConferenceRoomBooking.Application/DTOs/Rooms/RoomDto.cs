namespace ConferenceRoomBooking.Application.DTOs.Rooms;

public class RoomDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int Capacity { get; set; }
    public decimal BaseHourlyRate { get; set; }
    public List<ServiceDto> AvailableServices { get; set; } = new();
}

public class ServiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
}