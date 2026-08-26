using ConferenceRoomBooking.Application.Interfaces.Services;
using ConferenceRoomBooking.Domain.Enums;

namespace ConferenceRoomBooking.Application.Services;

/// <summary>
/// Розраховує вартість бронювання з урахуванням часових тарифних зон.
/// Правила (пріоритет зверху вниз, якщо перетинаються):
///  - Пікові (12:00–14:00): націнка +15%
///  - Ранкові (06:00–09:00): знижка -10%
///  - Вечірні (18:00–23:00): знижка -20%
///  - Стандартні (09:00–18:00, крім пікових): базова вартість
/// Бронювання поза 06:00–23:00 наразі не підтримується (немає тарифу).
/// </summary>
public class PricingService : IPricingService
{
    private static readonly TimeSpan MorningStart = TimeSpan.FromHours(6);
    private static readonly TimeSpan StandardStart = TimeSpan.FromHours(9);
    private static readonly TimeSpan PeakStart = TimeSpan.FromHours(12);
    private static readonly TimeSpan PeakEnd = TimeSpan.FromHours(14);
    private static readonly TimeSpan EveningStart = TimeSpan.FromHours(18);
    private static readonly TimeSpan EveningEnd = TimeSpan.FromHours(23);

    public decimal CalculateBookingPrice(
        decimal baseHourlyRate,
        DateTime start,
        DateTime end,
        IEnumerable<decimal> selectedServicesPrices)
    {
        if (end <= start)
            throw new ArgumentException("Час завершення має бути пізніше часу початку.");

        var roomCost = CalculateRoomCost(baseHourlyRate, start, end);
        var servicesCost = selectedServicesPrices.Sum();

        return Math.Round(roomCost + servicesCost, 2);
    }

    private decimal CalculateRoomCost(decimal baseHourlyRate, DateTime start, DateTime end)
    {
        decimal total = 0m;
        var boundaries = BuildSegmentBoundaries(start, end);

        for (var i = 0; i < boundaries.Count - 1; i++)
        {
            var segmentStart = boundaries[i];
            var segmentEnd = boundaries[i + 1];
            var hours = (decimal)(segmentEnd - segmentStart).TotalHours;

            var rateMultiplier = GetRateMultiplier(segmentStart.TimeOfDay);
            total += baseHourlyRate * hours * rateMultiplier;
        }

        return total;
    }

    /// <summary>
    /// Розбиває проміжок бронювання на під-сегменти в точках зміни тарифу
    /// (06:00, 09:00, 12:00, 14:00, 18:00, 23:00), щоб кожен сегмент мав єдиний коефіцієнт.
    /// </summary>
    private static List<DateTime> BuildSegmentBoundaries(DateTime start, DateTime end)
    {
        var boundaries = new List<DateTime> { start, end };

        var dayAnchors = new[] { MorningStart, StandardStart, PeakStart, PeakEnd, EveningStart, EveningEnd };

        var currentDay = start.Date;
        while (currentDay <= end.Date)
        {
            foreach (var anchor in dayAnchors)
            {
                var point = currentDay + anchor;
                if (point > start && point < end)
                    boundaries.Add(point);
            }
            currentDay = currentDay.AddDays(1);
        }

        boundaries.Sort();
        return boundaries.Distinct().ToList();
    }

    private static decimal GetRateMultiplier(TimeSpan timeOfDay)
    {
        var slot = ResolveTimeSlot(timeOfDay);

        return slot switch
        {
            TimeSlotType.Peak => 1.15m,
            TimeSlotType.Morning => 0.90m,
            TimeSlotType.Evening => 0.80m,
            TimeSlotType.Standard => 1.00m,
            _ => throw new InvalidOperationException($"Непідтримуваний тарифний слот: {slot}")
        };
    }

    private static TimeSlotType ResolveTimeSlot(TimeSpan timeOfDay)
    {
        // Пікові години мають пріоритет над стандартними, оскільки вони "вкладені" в 09:00–18:00
        if (timeOfDay >= PeakStart && timeOfDay < PeakEnd)
            return TimeSlotType.Peak;

        if (timeOfDay >= MorningStart && timeOfDay < StandardStart)
            return TimeSlotType.Morning;

        if (timeOfDay >= StandardStart && timeOfDay < EveningStart)
            return TimeSlotType.Standard;

        if (timeOfDay >= EveningStart && timeOfDay < EveningEnd)
            return TimeSlotType.Evening;

        throw new ArgumentOutOfRangeException(
            nameof(timeOfDay),
            "Бронювання поза межами 06:00–23:00 наразі не підтримується.");
    }
}