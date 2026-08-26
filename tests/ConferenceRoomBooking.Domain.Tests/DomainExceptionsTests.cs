using ConferenceRoomBooking.Domain.Exceptions;
using FluentAssertions;

namespace ConferenceRoomBooking.Domain.Tests.Exceptions;

public class RoomNotFoundExceptionTests
{
    [Fact]
    public void Constructor_SetsRoomIdAndMessage()
    {
        var roomId = Guid.NewGuid();

        var exception = new RoomNotFoundException(roomId);

        exception.RoomId.Should().Be(roomId);
        exception.Message.Should().Contain(roomId.ToString());
    }

    [Fact]
    public void IsAssignableTo_DomainException()
    {
        var exception = new RoomNotFoundException(Guid.NewGuid());

        exception.Should().BeAssignableTo<DomainException>();
    }
}

public class RoomNotAvailableExceptionTests
{
    [Fact]
    public void Constructor_SetsPropertiesAndFormattedMessage()
    {
        var roomId = Guid.NewGuid();
        var start = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

        var exception = new RoomNotAvailableException(roomId, start, end);

        exception.RoomId.Should().Be(roomId);
        exception.RequestedStart.Should().Be(start);
        exception.RequestedEnd.Should().Be(end);
        exception.Message.Should().Contain(roomId.ToString());
        exception.Message.Should().Contain("01.09.2026 10:00");
        exception.Message.Should().Contain("12:00");
    }

    [Fact]
    public void IsAssignableTo_DomainException()
    {
        var exception = new RoomNotAvailableException(Guid.NewGuid(), DateTime.Now, DateTime.Now.AddHours(1));

        exception.Should().BeAssignableTo<DomainException>();
    }
}

public class DomainExceptionTests
{
    private sealed class TestDomainException(string message) : DomainException(message);

    [Fact]
    public void Constructor_SetsMessage()
    {
        var exception = new TestDomainException("Something went wrong");

        exception.Message.Should().Be("Something went wrong");
    }

    [Fact]
    public void IsAssignableTo_Exception()
    {
        var exception = new TestDomainException("error");

        exception.Should().BeAssignableTo<Exception>();
    }
}