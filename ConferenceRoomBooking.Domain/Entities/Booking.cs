using ConferenceRoomBooking.Domain.Enums;

namespace ConferenceRoomBooking.Domain.Entities;

/// <summary>
/// Бронювання конференц-залу на конкретний часовий проміжок з переліком обраних послуг.
/// </summary>
public class Booking
{
    private readonly List<Service> _selectedServices = new();

    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public ConferenceRoom? Room { get; private set; }

    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }

    public decimal TotalPrice { get; private set; }
    public BookingStatus Status { get; private set; }

    public IReadOnlyCollection<Service> SelectedServices => _selectedServices.AsReadOnly();

    private Booking() { } // для EF Core

    public Booking(Guid roomId, DateTime startTime, DateTime endTime, IEnumerable<Service> selectedServices)
    {
        if (endTime <= startTime)
            throw new ArgumentException("Час завершення має бути пізніше часу початку.");

        Id = Guid.NewGuid();
        RoomId = roomId;
        StartTime = startTime;
        EndTime = endTime;
        _selectedServices.AddRange(selectedServices);
        Status = BookingStatus.Pending;
    }

    public TimeSpan Duration => EndTime - StartTime;

    public void SetTotalPrice(decimal totalPrice)
    {
        if (totalPrice < 0)
            throw new ArgumentException("Вартість бронювання не може бути від'ємною.", nameof(totalPrice));

        TotalPrice = totalPrice;
    }

    public void Confirm() => Status = BookingStatus.Confirmed;
    public void Cancel() => Status = BookingStatus.Cancelled;
    public void Complete() => Status = BookingStatus.Completed;
}