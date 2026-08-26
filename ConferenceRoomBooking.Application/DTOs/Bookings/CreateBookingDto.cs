namespace ConferenceRoomBooking.Application.DTOs.Bookings;

public class CreateBookingDto
{
    public Guid RoomId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<Guid> SelectedServiceIds { get; set; } = new();
}