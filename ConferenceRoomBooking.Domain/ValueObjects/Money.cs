using System.Globalization;

namespace ConferenceRoomBooking.Domain.ValueObjects;

/// <summary>
/// Value Object для грошової суми. Immutable, з валідацією при створенні.
/// </summary>
public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency = "UAH")
    {
        if (amount < 0)
            throw new ArgumentException("Сума не може бути від'ємною.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Валюта є обов'язковою.", nameof(currency));

        return new Money(amount, currency);
    }

    public static Money Zero(string currency = "UAH") => new(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money ApplyPercentage(decimal percentage)
    {
        // percentage: наприклад -20 для знижки 20%, +15 для націнки 15%
        var adjusted = Amount + Amount * (percentage / 100m);
        return new Money(Math.Round(adjusted, 2), Currency);
    }

    public Money Multiply(decimal factor)
    {
        if (factor < 0)
            throw new ArgumentException("Множник не може бути від'ємним.", nameof(factor));

        return new Money(Math.Round(Amount * factor, 2), Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException(
                $"Неможливо виконати операцію над сумами з різною валютою: {Currency} та {other.Currency}.");
    }

    public bool Equals(Money? other) =>
        other is not null && Amount == other.Amount && Currency == other.Currency;

    public override bool Equals(object? obj) => Equals(obj as Money);
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);
    public override string ToString() =>
     Amount.ToString("0.00", CultureInfo.InvariantCulture) + $" {Currency}";

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static bool operator ==(Money? left, Money? right) => Equals(left, right);
    public static bool operator !=(Money? left, Money? right) => !Equals(left, right);
}