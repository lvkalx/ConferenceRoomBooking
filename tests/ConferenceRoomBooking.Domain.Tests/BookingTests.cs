using ConferenceRoomBooking.Domain.Entities;
using ConferenceRoomBooking.Domain.Enums;
using FluentAssertions;

namespace ConferenceRoomBooking.Domain.Tests.Entities;

public class BookingTests
{
    private static readonly DateTime Start = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    private static List<Service> CreateServices(int count = 2)
    {
        var services = new List<Service>();
        for (var i = 0; i < count; i++)
            services.Add(new Service($"Service {i}", 10 * (i + 1)));

        return services;
    }

    [Fact]
    public void Constructor_WithValidData_CreatesBookingWithPendingStatus()
    {
        var roomId = Guid.NewGuid();
        var services = CreateServices();

        var booking = new Booking(roomId, Start, End, services);

        booking.Id.Should().NotBeEmpty();
        booking.RoomId.Should().Be(roomId);
        booking.StartTime.Should().Be(Start);
        booking.EndTime.Should().Be(End);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.SelectedServices.Should().BeEquivalentTo(services);
    }

    [Fact]
    public void Constructor_GeneratesUniqueIdForEachBooking()
    {
        var booking1 = new Booking(Guid.NewGuid(), Start, End, CreateServices(0));
        var booking2 = new Booking(Guid.NewGuid(), Start, End, CreateServices(0));

        booking1.Id.Should().NotBe(booking2.Id);
    }

    [Fact]
    public void Constructor_WithEmptyServices_CreatesBookingWithNoServices()
    {
        var booking = new Booking(Guid.NewGuid(), Start, End, Enumerable.Empty<Service>());

        booking.SelectedServices.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WhenEndTimeEqualsStartTime_ThrowsArgumentException()
    {
        var act = () => new Booking(Guid.NewGuid(), Start, Start, CreateServices(0));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Час завершення має бути пізніше часу початку*");
    }

    [Fact]
    public void Constructor_WhenEndTimeBeforeStartTime_ThrowsArgumentException()
    {
        var act = () => new Booking(Guid.NewGuid(), End, Start, CreateServices(0));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SelectedServices_IsReadOnly_AndReflectsConstructorInput()
    {
        var services = CreateServices(3);
        var booking = new Booking(Guid.NewGuid(), Start, End, services);

        booking.SelectedServices.Should().BeAssignableTo<IReadOnlyCollection<Service>>();
        booking.SelectedServices.Should().HaveCount(3);
    }

    [Fact]
    public void Duration_ReturnsDifferenceBetweenStartAndEnd()
    {
        var booking = new Booking(Guid.NewGuid(), Start, End, CreateServices(0));

        booking.Duration.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void SetTotalPrice_WithPositiveValue_SetsPrice()
    {
        var booking = new Booking(Guid.NewGuid(), Start, End, CreateServices(0));

        booking.SetTotalPrice(150.5m);

        booking.TotalPrice.Should().Be(150.5m);
    }

    [Fact]
    public void SetTotalPrice_WithZero_SetsPrice()
    {
        var booking = new Booking(Guid.NewGuid(), Start, End, CreateServices(0));

        booking.SetTotalPrice(0m);

        booking.TotalPrice.Should().Be(0m);
    }

    [Fact]
    public void SetTotalPrice_WithNegativeValue_ThrowsArgumentException()
    {
        var booking = new Booking(Guid.NewGuid(), Start, End, CreateServices(0));

        var act = () => booking.SetTotalPrice(-1m);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*не може бути від'ємною*")
            .And.ParamName.Should().Be("totalPrice");
    }

    [Fact]
    public void Confirm_ChangesStatusToConfirmed()
    {
        var booking = new Booking(Guid.NewGuid(), Start, End, CreateServices(0));

        booking.Confirm();

        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public void Cancel_ChangesStatusToCancelled()
    {
        var booking = new Booking(Guid.NewGuid(), Start, End, CreateServices(0));

        booking.Cancel();

        booking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public void Complete_ChangesStatusToCompleted()
    {
        var booking = new Booking(Guid.NewGuid(), Start, End, CreateServices(0));

        booking.Complete();

        booking.Status.Should().Be(BookingStatus.Completed);
    }

    [Fact]
    public void StatusTransitions_CanBeAppliedSequentially_LastWriteWins()
    {
        var booking = new Booking(Guid.NewGuid(), Start, End, CreateServices(0));

        booking.Confirm();
        booking.Cancel();
        booking.Complete();

        booking.Status.Should().Be(BookingStatus.Completed);
    }
}