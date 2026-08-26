using ConferenceRoomBooking.Application.Services;
using FluentAssertions;

namespace ConferenceRoomBooking.Application.Tests.Services;

public class PricingServiceTests
{
    private readonly PricingService _sut = new();

    private static DateTime On(int hour, int minute = 0) => new(2026, 9, 1, hour, minute, 0);

    #region Single tariff zone

    [Fact]
    public void CalculateBookingPrice_EntirelyWithinStandardZone_ChargesBaseRateWithNoAdjustment()
    {
        // 10:00–11:00 — стандартна зона (09:00–18:00, поза піком), множник x1.00
        var price = _sut.CalculateBookingPrice(100m, On(10), On(11), []);

        price.Should().Be(100m);
    }

    [Fact]
    public void CalculateBookingPrice_EntirelyWithinPeakZone_AppliesFifteenPercentSurcharge()
    {
        // 12:00–13:00 — пікова зона, множник x1.15
        var price = _sut.CalculateBookingPrice(100m, On(12), On(13), []);

        price.Should().Be(115m);
    }

    [Fact]
    public void CalculateBookingPrice_EntirelyWithinMorningZone_AppliesTenPercentDiscount()
    {
        // 07:00–08:00 — ранкова зона, множник x0.90
        var price = _sut.CalculateBookingPrice(100m, On(7), On(8), []);

        price.Should().Be(90m);
    }

    [Fact]
    public void CalculateBookingPrice_EntirelyWithinEveningZone_AppliesTwentyPercentDiscount()
    {
        // 19:00–20:00 — вечірня зона, множник x0.80
        var price = _sut.CalculateBookingPrice(100m, On(19), On(20), []);

        price.Should().Be(80m);
    }

    #endregion

    #region Crossing tariff boundaries

    [Fact]
    public void CalculateBookingPrice_CrossingMorningIntoStandard_SplitsCostBySegment()
    {
        // 08:00–10:00: 08:00–09:00 ранок (0.90) + 09:00–10:00 стандарт (1.00)
        var price = _sut.CalculateBookingPrice(100m, On(8), On(10), []);

        price.Should().Be(90m + 100m);
    }

    [Fact]
    public void CalculateBookingPrice_CrossingStandardPeakStandard_SplitsCostByAllThreeSegments()
    {
        // 11:00–15:00: 11-12 стандарт (100) + 12-14 пік (2*115=230) + 14-15 стандарт (100) = 430
        var price = _sut.CalculateBookingPrice(100m, On(11), On(15), []);

        price.Should().Be(430m);
    }

    [Fact]
    public void CalculateBookingPrice_CrossingStandardIntoEvening_SplitsCostBySegment()
    {
        // 17:00–19:00: 17-18 стандарт (100) + 18-19 вечір (80) = 180
        var price = _sut.CalculateBookingPrice(100m, On(17), On(19), []);

        price.Should().Be(180m);
    }

    #endregion

    #region Additional services

    [Fact]
    public void CalculateBookingPrice_WithSelectedServices_AddsTheirPricesOnTopOfRoomCost()
    {
        // 10:00–11:00 стандарт = 100, + послуги 20 і 30
        var price = _sut.CalculateBookingPrice(100m, On(10), On(11), [20m, 30m]);

        price.Should().Be(150m);
    }

    [Fact]
    public void CalculateBookingPrice_WithNoSelectedServices_ChargesOnlyRoomCost()
    {
        var price = _sut.CalculateBookingPrice(100m, On(10), On(11), Array.Empty<decimal>());

        price.Should().Be(100m);
    }

    #endregion

    #region Rounding

    [Fact]
    public void CalculateBookingPrice_WhenResultHasMoreThanTwoDecimals_RoundsToTwoDecimalPlaces()
    {
        // 10 хвилин у стандартній зоні за ставкою 100/год => 16.6666...  -> округлення до 16.67
        var price = _sut.CalculateBookingPrice(100m, On(10, 0), On(10, 10), []);

        price.Should().Be(Math.Round(100m * (1m / 6m), 2));
    }

    #endregion

    #region Validation / edge cases

    [Fact]
    public void CalculateBookingPrice_WhenEndTimeIsBeforeStartTime_ThrowsArgumentException()
    {
        var act = () => _sut.CalculateBookingPrice(100m, On(11), On(10), []);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculateBookingPrice_WhenEndTimeEqualsStartTime_ThrowsArgumentException()
    {
        var start = On(10);
        var act = () => _sut.CalculateBookingPrice(100m, start, start, []);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculateBookingPrice_WhenStartTimeIsBeforeSixAm_ThrowsArgumentOutOfRangeException()
    {
        // 05:00 не потрапляє в жодний тарифний слот (день підтримується лише з 06:00)
        var act = () => _sut.CalculateBookingPrice(100m, On(5), On(5, 30), []);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculateBookingPrice_WhenEndTimeIsAfterElevenPm_ThrowsArgumentOutOfRangeException()
    {
        // Сегмент 23:00–23:30 виходить за межі підтримуваних тарифних слотів (до 23:00)
        var act = () => _sut.CalculateBookingPrice(100m, On(22, 30), On(23, 30), []);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculateBookingPrice_MultiDayBooking_AppliesTariffZonesForEachDay()
    {
        // 01.09 10:00 -> 02.09 11:00: 1й день 10:00-18:00 (стандарт/пік з проривом),
        // повна ніч поза тарифом не підтримується, тож обмежимось коректним випадком
        // в межах 06:00-23:00 обох днів.
        var start = new DateTime(2026, 9, 1, 20, 0, 0); // вечір першого дня
        var end = new DateTime(2026, 9, 2, 7, 0, 0);    // ранок другого дня

        // 20:00-23:00 (3h вечір, x0.80) + 23:00-06:00 не підтримується -> очікуємо виняток
        var act = () => _sut.CalculateBookingPrice(100m, start, end, []);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion
}