using ConferenceRoomBooking.Domain.ValueObjects;
using FluentAssertions;

namespace ConferenceRoomBooking.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidAmountAndDefaultCurrency_CreatesMoneyInUah()
    {
        var money = Money.Create(100m);

        money.Amount.Should().Be(100m);
        money.Currency.Should().Be("UAH");
    }

    [Fact]
    public void Create_WithExplicitCurrency_UsesThatCurrency()
    {
        var money = Money.Create(50m, "USD");

        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_WithZeroAmount_IsAllowed()
    {
        var money = Money.Create(0m);

        money.Amount.Should().Be(0m);
    }

    [Fact]
    public void Create_WithNegativeAmount_ThrowsArgumentException()
    {
        var act = () => Money.Create(-1m);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*не може бути від'ємною*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidCurrency_ThrowsArgumentException(string? currency)
    {
        var act = () => Money.Create(10m, currency!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Валюта є обов'язковою*");
    }

    [Fact]
    public void Zero_WithDefaultCurrency_ReturnsZeroUah()
    {
        var money = Money.Zero();

        money.Amount.Should().Be(0m);
        money.Currency.Should().Be("UAH");
    }

    [Fact]
    public void Zero_WithExplicitCurrency_ReturnsZeroInThatCurrency()
    {
        var money = Money.Zero("EUR");

        money.Amount.Should().Be(0m);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Add_WithSameCurrency_ReturnsSum()
    {
        var a = Money.Create(100m, "UAH");
        var b = Money.Create(50m, "UAH");

        var result = a.Add(b);

        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("UAH");
    }

    [Fact]
    public void Add_WithDifferentCurrency_ThrowsInvalidOperationException()
    {
        var a = Money.Create(100m, "UAH");
        var b = Money.Create(50m, "USD");

        var act = () => a.Add(b);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*різною валютою*UAH*USD*");
    }

    [Fact]
    public void PlusOperator_WithSameCurrency_ReturnsSum()
    {
        var a = Money.Create(30m);
        var b = Money.Create(20m);

        var result = a + b;

        result.Amount.Should().Be(50m);
    }

    [Theory]
    [InlineData(100, -20, 80)]
    [InlineData(100, 15, 115)]
    [InlineData(100, 0, 100)]
    public void ApplyPercentage_AdjustsAmountAccordingly(decimal amount, decimal percentage, decimal expected)
    {
        var money = Money.Create(amount);

        var result = money.ApplyPercentage(percentage);

        result.Amount.Should().Be(expected);
    }

    [Fact]
    public void ApplyPercentage_RoundsToTwoDecimalPlaces()
    {
        var money = Money.Create(10m);

        var result = money.ApplyPercentage(33.333m);

        result.Amount.Should().Be(13.33m);
    }

    [Fact]
    public void ApplyPercentage_PreservesCurrency()
    {
        var money = Money.Create(10m, "USD");

        var result = money.ApplyPercentage(10m);

        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Multiply_WithPositiveFactor_ReturnsScaledAmount()
    {
        var money = Money.Create(10m);

        var result = money.Multiply(3);

        result.Amount.Should().Be(30m);
    }

    [Fact]
    public void Multiply_WithZeroFactor_ReturnsZero()
    {
        var money = Money.Create(10m);

        var result = money.Multiply(0);

        result.Amount.Should().Be(0m);
    }

    [Fact]
    public void Multiply_RoundsToTwoDecimalPlaces()
    {
        var money = Money.Create(10m);

        var result = money.Multiply(1.111m);

        result.Amount.Should().Be(11.11m);
    }

    [Fact]
    public void Multiply_WithNegativeFactor_ThrowsArgumentException()
    {
        var money = Money.Create(10m);

        var act = () => money.Multiply(-1);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*не може бути від'ємним*");
    }

    [Fact]
    public void Equals_WithSameAmountAndCurrency_ReturnsTrue()
    {
        var a = Money.Create(10m, "UAH");
        var b = Money.Create(10m, "UAH");

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentAmount_ReturnsFalse()
    {
        var a = Money.Create(10m);
        var b = Money.Create(20m);

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentCurrency_ReturnsFalse()
    {
        var a = Money.Create(10m, "UAH");
        var b = Money.Create(10m, "USD");

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        var a = Money.Create(10m);

        a.Equals(null).Should().BeFalse();
        (a == null).Should().BeFalse();
        (null == a).Should().BeFalse();
    }

    [Fact]
    public void EqualsObject_WithNonMoneyObject_ReturnsFalse()
    {
        var a = Money.Create(10m);

        a.Equals(new object()).Should().BeFalse();
    }

    [Fact]
    public void TwoNullMoneyReferences_AreEqualViaOperator()
    {
        Money? a = null;
        Money? b = null;

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_ForEqualValues_AreEqual()
    {
        var a = Money.Create(10m, "UAH");
        var b = Money.Create(10m, "UAH");

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ToString_FormatsAmountWithTwoDecimalsAndCurrency()
    {
        var money = Money.Create(10m, "UAH");

        money.ToString().Should().Be("10.00 UAH");
    }
}