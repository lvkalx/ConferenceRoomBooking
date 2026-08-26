namespace ConferenceRoomBooking.Application.Interfaces.Services;

public interface IPricingService
{
    /// <summary>
    /// Розраховує вартість оренди залу за проміжок часу з урахуванням
    /// ранкових/пікових/вечірніх коефіцієнтів, + вартість обраних послуг.
    /// </summary>
    decimal CalculateBookingPrice(decimal baseHourlyRate, DateTime start, DateTime end, IEnumerable<decimal> selectedServicesPrices);
}