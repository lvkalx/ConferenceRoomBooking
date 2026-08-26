namespace ConferenceRoomBooking.Application.DTOs.Rooms;

public class CreateRoomDto
{
    public string Name { get; set; } = null!;
    public int Capacity { get; set; }
    public decimal BaseHourlyRate { get; set; }
    public List<CreateServiceDto> Services { get; set; } = new();
}

public class CreateServiceDto
{
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
}