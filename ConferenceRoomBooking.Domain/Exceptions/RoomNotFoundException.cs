namespace ConferenceRoomBooking.Domain.Exceptions;

public sealed class RoomNotFoundException : DomainException
{
    public Guid RoomId { get; }

    public RoomNotFoundException(Guid roomId)
        : base($"Конференц-зал з ID '{roomId}' не знайдено.")
    {
        RoomId = roomId;
    }
}