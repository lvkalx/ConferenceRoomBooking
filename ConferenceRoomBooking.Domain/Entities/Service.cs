namespace ConferenceRoomBooking.Domain.Entities;

/// <summary>
/// Додаткова послуга (проєктор, Wi-Fi, звук тощо), яку можна підключити до залу/бронювання.
/// </summary>
public class Service
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }

    private Service() { } // для EF Core

    public Service(string name, decimal price)
    {
        SetName(name);
        SetPrice(price);
        Id = Guid.NewGuid();
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва послуги є обов'язковою.", nameof(name));

        Name = name.Trim();
    }

    public void SetPrice(decimal price)
    {
        if (price < 0)
            throw new ArgumentException("Вартість послуги не може бути від'ємною.", nameof(price));

        Price = price;
    }
}