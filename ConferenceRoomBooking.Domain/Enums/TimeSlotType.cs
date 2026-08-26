namespace ConferenceRoomBooking.Domain.Enums;

/// <summary>
/// Тип часового слоту, від якого залежить тарифікація бронювання.
/// </summary>
public enum TimeSlotType
{
    /// <summary>06:00–09:00, знижка 10%</summary>
    Morning,

    /// <summary>09:00–18:00 (крім пікових), базова вартість</summary>
    Standard,

    /// <summary>12:00–14:00, націнка 15%</summary>
    Peak,

    /// <summary>18:00–23:00, знижка 20%</summary>
    Evening
}