using System.Reflection;
using ConferenceRoomBooking.Domain.Entities;
using ConferenceRoomBooking.Domain.Exceptions;
using FluentAssertions;

namespace ConferenceRoomBooking.Domain.Tests.Entities;

public class ConferenceRoomTests
{
    private static ConferenceRoom CreateRoom(string name = "Room A", int capacity = 10, decimal rate = 100m) =>
        new(name, capacity, rate);

    /// <summary>
    /// ConferenceRoom.Bookings is populated exclusively through EF Core navigation and has no
    /// public API to append to it. To unit-test the IsAvailable/EnsureAvailable business rules
    /// in isolation (without EF Core), we inject into the private backing field via reflection —
    /// this keeps the test a pure, fast unit test instead of promoting it to an EF/integration test.
    /// </summary>
    private static void AttachBooking(ConferenceRoom room, Booking booking)
    {
        var field = typeof(ConferenceRoom).GetField("_bookings", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Could not locate _bookings field via reflection.");

        var list = (List<Booking>)field.GetValue(room)!;
        list.Add(booking);
    }

    #region Constructor

    [Fact]
    public void Constructor_WithValidData_CreatesRoom()
    {
        var room = CreateRoom("Conference Hall", 20, 250.50m);

        room.Id.Should().NotBeEmpty();
        room.Name.Should().Be("Conference Hall");
        room.Capacity.Should().Be(20);
        room.BaseHourlyRate.Should().Be(250.50m);
        room.IsDeleted.Should().BeFalse();
        room.AvailableServices.Should().BeEmpty();
        room.Bookings.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_GeneratesUniqueId()
    {
        var room1 = CreateRoom();
        var room2 = CreateRoom();

        room1.Id.Should().NotBe(room2.Id);
    }

    [Fact]
    public void Constructor_TrimsName()
    {
        var room = CreateRoom("  Room With Spaces  ");

        room.Name.Should().Be("Room With Spaces");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_WithNonPositiveCapacity_ThrowsArgumentException(int capacity)
    {
        var act = () => CreateRoom(capacity: capacity);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Місткість залу має бути більшою за нуль*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveRate_ThrowsArgumentException(decimal rate)
    {
        var act = () => CreateRoom(rate: rate);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Базова вартість оренди має бути більшою за нуль*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        var act = () => CreateRoom(name!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Назва залу є обов'язковою*");
    }

    #endregion

    #region SetName / SetCapacity / SetBaseHourlyRate

    [Fact]
    public void SetName_WithValidName_UpdatesName()
    {
        var room = CreateRoom();

        room.SetName("New Name");

        room.Name.Should().Be("New Name");
    }

    [Fact]
    public void SetCapacity_WithValidValue_UpdatesCapacity()
    {
        var room = CreateRoom();

        room.SetCapacity(50);

        room.Capacity.Should().Be(50);
    }

    [Fact]
    public void SetBaseHourlyRate_WithValidValue_UpdatesRate()
    {
        var room = CreateRoom();

        room.SetBaseHourlyRate(500m);

        room.BaseHourlyRate.Should().Be(500m);
    }

    [Fact]
    public void SetCapacity_WithZero_ThrowsArgumentException()
    {
        var room = CreateRoom();

        var act = () => room.SetCapacity(0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetBaseHourlyRate_WithNegative_ThrowsArgumentException()
    {
        var room = CreateRoom();

        var act = () => room.SetBaseHourlyRate(-10m);

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Services

    [Fact]
    public void AddService_AddsNewServiceToAvailableServices()
    {
        var room = CreateRoom();
        var service = new Service("Projector", 20m);

        room.AddService(service);

        room.AvailableServices.Should().ContainSingle().Which.Should().Be(service);
    }

    [Fact]
    public void AddService_WhenServiceAlreadyExists_IsIdempotent()
    {
        var room = CreateRoom();
        var service = new Service("Projector", 20m);
        room.AddService(service);

        room.AddService(service);

        room.AvailableServices.Should().ContainSingle();
    }

    [Fact]
    public void RemoveService_WhenServiceExists_RemovesIt()
    {
        var room = CreateRoom();
        var service = new Service("Projector", 20m);
        room.AddService(service);

        room.RemoveService(service.Id);

        room.AvailableServices.Should().BeEmpty();
    }

    [Fact]
    public void RemoveService_WhenServiceDoesNotExist_DoesNotThrow()
    {
        var room = CreateRoom();

        var act = () => room.RemoveService(Guid.NewGuid());

        act.Should().NotThrow();
        room.AvailableServices.Should().BeEmpty();
    }

    [Fact]
    public void ReplaceServices_ClearsOldServicesAndAddsNewOnes()
    {
        var room = CreateRoom();
        room.AddService(new Service("Old Service", 5m));

        var newServices = new[] { new Service("Wi-Fi", 10m), new Service("Sound", 30m) };
        room.ReplaceServices(newServices);

        room.AvailableServices.Should().BeEquivalentTo(newServices);
    }

    [Fact]
    public void ReplaceServices_WithEmptyCollection_ClearsAllServices()
    {
        var room = CreateRoom();
        room.AddService(new Service("Old Service", 5m));

        room.ReplaceServices(Enumerable.Empty<Service>());

        room.AvailableServices.Should().BeEmpty();
    }

    #endregion

    #region MarkAsDeleted

    [Fact]
    public void MarkAsDeleted_SetsIsDeletedToTrue()
    {
        var room = CreateRoom();

        room.MarkAsDeleted();

        room.IsDeleted.Should().BeTrue();
    }

    #endregion

    #region IsAvailable / EnsureAvailable

    [Fact]
    public void IsAvailable_WhenNoBookings_ReturnsTrue()
    {
        var room = CreateRoom();

        var result = room.IsAvailable(
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));

        result.Should().BeTrue();
    }

    [Fact]
    public void EnsureAvailable_WhenNoBookings_DoesNotThrow()
    {
        var room = CreateRoom();

        var act = () => room.EnsureAvailable(
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));

        act.Should().NotThrow();
    }

    [Fact]
    public void IsAvailable_WhenRequestedRangeOverlapsExistingActiveBooking_ReturnsFalse()
    {
        var room = CreateRoom();
        var existing = new Booking(room.Id,
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            Enumerable.Empty<Service>());
        AttachBooking(room, existing);

        var result = room.IsAvailable(
            new DateTime(2026, 9, 1, 11, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 13, 0, 0, DateTimeKind.Utc));

        result.Should().BeFalse();
    }

    [Fact]
    public void IsAvailable_WhenRequestedRangeEndsExactlyAtExistingBookingStart_ReturnsTrue()
    {
        var room = CreateRoom();
        var existing = new Booking(room.Id,
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            Enumerable.Empty<Service>());
        AttachBooking(room, existing);

        var result = room.IsAvailable(
            new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc));

        result.Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_WhenRequestedRangeStartsExactlyAtExistingBookingEnd_ReturnsTrue()
    {
        var room = CreateRoom();
        var existing = new Booking(room.Id,
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            Enumerable.Empty<Service>());
        AttachBooking(room, existing);

        var result = room.IsAvailable(
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 14, 0, 0, DateTimeKind.Utc));

        result.Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_IgnoresCancelledBookings()
    {
        var room = CreateRoom();
        var cancelled = new Booking(room.Id,
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            Enumerable.Empty<Service>());
        cancelled.Cancel();
        AttachBooking(room, cancelled);

        var result = room.IsAvailable(
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));

        result.Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_WithExcludingBookingId_IgnoresThatBookingWhenCheckingOverlap()
    {
        var room = CreateRoom();
        var existing = new Booking(room.Id,
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            Enumerable.Empty<Service>());
        AttachBooking(room, existing);

        var result = room.IsAvailable(
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            excludingBookingId: existing.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public void EnsureAvailable_WhenAvailable_DoesNotThrow()
    {
        var room = CreateRoom();

        var existing = new Booking(
            room.Id,
            new DateTime(2026, 9, 1, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 16, 0, 0, DateTimeKind.Utc),
            Enumerable.Empty<Service>());

        AttachBooking(room, existing);

        var act = () => room.EnsureAvailable(
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureAvailable_WhenNotAvailable_ThrowsRoomNotAvailableExceptionWithRoomIdAndPeriod()
    {
        var room = CreateRoom();
        var existing = new Booking(room.Id,
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            Enumerable.Empty<Service>());
        AttachBooking(room, existing);

        var start = new DateTime(2026, 9, 1, 11, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 9, 1, 13, 0, 0, DateTimeKind.Utc);

        var act = () => room.EnsureAvailable(start, end);

        act.Should().Throw<RoomNotAvailableException>()
            .Which.Should().Match<RoomNotAvailableException>(ex =>
                ex.RoomId == room.Id && ex.RequestedStart == start && ex.RequestedEnd == end);
    }

    #endregion
}