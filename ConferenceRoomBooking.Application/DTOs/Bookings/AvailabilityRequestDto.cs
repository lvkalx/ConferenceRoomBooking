namespace ConferenceRoomBooking.Application.DTOs.Bookings;

public class AvailabilityRequestDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? MinCapacity { get; set; }
}