using ConferenceRoomBooking.Domain.Enums;

namespace ConferenceRoomBooking.Application.DTOs.Bookings;

public class BookingDto
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<string> SelectedServices { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; }
}