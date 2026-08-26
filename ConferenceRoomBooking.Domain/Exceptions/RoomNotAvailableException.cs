namespace ConferenceRoomBooking.Domain.Exceptions;

public sealed class RoomNotAvailableException : DomainException
{
    public Guid RoomId { get; }
    public DateTime RequestedStart { get; }
    public DateTime RequestedEnd { get; }

    public RoomNotAvailableException(Guid roomId, DateTime requestedStart, DateTime requestedEnd)
        : base($"Зал '{roomId}' вже заброньовано на період " +
               $"{requestedStart:dd.MM.yyyy HH:mm}–{requestedEnd:HH:mm}.")
    {
        RoomId = roomId;
        RequestedStart = requestedStart;
        RequestedEnd = requestedEnd;
    }
}