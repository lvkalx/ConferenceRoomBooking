namespace ConferenceRoomBooking.Domain.Exceptions;

/// <summary>
/// Базовий клас для всіх доменних (бізнесових) винятків.
/// Використовується у ExceptionHandlingMiddleware для мапінгу на HTTP-статуси.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}