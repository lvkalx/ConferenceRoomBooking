using ConferenceRoomBooking.Domain.Exceptions;

namespace ConferenceRoomBooking.Domain.Entities;

/// <summary>
/// Конференц-зал: місткість, базова вартість оренди за годину, доступні послуги.
/// </summary>
public class ConferenceRoom
{
    private readonly List<Service> _availableServices = new();
    private readonly List<Booking> _bookings = new();

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public int Capacity { get; private set; }
    public decimal BaseHourlyRate { get; private set; }
    public bool IsDeleted { get; private set; }

    public IReadOnlyCollection<Service> AvailableServices => _availableServices.AsReadOnly();
    public IReadOnlyCollection<Booking> Bookings => _bookings.AsReadOnly();

    private ConferenceRoom() { } // для EF Core

    public ConferenceRoom(string name, int capacity, decimal baseHourlyRate)
    {
        Id = Guid.NewGuid();
        SetName(name);
        SetCapacity(capacity);
        SetBaseHourlyRate(baseHourlyRate);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва залу є обов'язковою.", nameof(name));

        Name = name.Trim();
    }

    public void SetCapacity(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Місткість залу має бути більшою за нуль.", nameof(capacity));

        Capacity = capacity;
    }

    public void SetBaseHourlyRate(decimal rate)
    {
        if (rate <= 0)
            throw new ArgumentException("Базова вартість оренди має бути більшою за нуль.", nameof(rate));

        BaseHourlyRate = rate;
    }

    public void AddService(Service service)
    {
        if (_availableServices.Any(s => s.Id == service.Id))
            return; // ідемпотентно, без винятку

        _availableServices.Add(service);
    }

    public void RemoveService(Guid serviceId)
    {
        _availableServices.RemoveAll(s => s.Id == serviceId);
    }

    /// <summary>
    /// Повністю замінює перелік послуг залу (використовується при PUT — повній заміні ресурсу).
    /// На відміну від AddService/RemoveService, тут немає часткової семантики:
    /// старий список повністю зникає, новий стає єдиним джерелом правди.
    /// </summary>
    public void ReplaceServices(IEnumerable<Service> newServices)
    {
        _availableServices.Clear();
        _availableServices.AddRange(newServices);
    }

    public void MarkAsDeleted() => IsDeleted = true;

    /// <summary>
    /// Перевіряє, чи вільний зал на заданий проміжок (без урахування бронювання itself, якщо оновлюється).
    /// </summary>
    public bool IsAvailable(DateTime start, DateTime end, Guid? excludingBookingId = null)
    {
        return _bookings
            .Where(b => b.Status != Enums.BookingStatus.Cancelled)
            .Where(b => excludingBookingId == null || b.Id != excludingBookingId)
            .All(b => end <= b.StartTime || start >= b.EndTime);
    }

    public void EnsureAvailable(DateTime start, DateTime end)
    {
        if (!IsAvailable(start, end))
            throw new RoomNotAvailableException(Id, start, end);
    }
}