using ConferenceRoomBooking.Domain.Entities;
using ConferenceRoomBooking.Infrastructure.Data.Repositories;
using ConferenceRoomBooking.Infrastructure.Tests.TestHelpers;
using FluentAssertions;

namespace ConferenceRoomBooking.Application.Tests.Repositories;

public class BookingRepositoryTests
{
    private static ConferenceRoom CreateRoom(string name = "Room A", int capacity = 50, decimal rate = 2000m) =>
        new(name, capacity, rate);

    private static Booking CreateBooking(Guid roomId, DateTime start, DateTime end, IEnumerable<Service>? services = null) =>
        new(roomId, start, end, services ?? Enumerable.Empty<Service>());

    #region AddAsync / SaveChangesAsync

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsBooking()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(room);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new BookingRepository(context);
        var booking = CreateBooking(room.Id, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));

        await sut.AddAsync(booking);
        var saved = await sut.SaveChangesAsync();

        saved.Should().BeTrue();
        (await context.Bookings.FindAsync(booking.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenNothingToPersist_ReturnsFalse()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var sut = new BookingRepository(context);

        var saved = await sut.SaveChangesAsync();

        saved.Should().BeFalse();
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenBookingExists_ReturnsBookingWithRoomAndServicesIncluded()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        var service = new Service("Projector", 500m);
        var booking = CreateBooking(room.Id, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc), new[] { service });

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Services.Add(service);
            setup.Rooms.Add(room);
            setup.Bookings.Add(booking);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new BookingRepository(context);

        var result = await sut.GetByIdAsync(booking.Id);

        result.Should().NotBeNull();
        result!.Room.Should().NotBeNull();
        result.Room!.Id.Should().Be(room.Id);
        result.SelectedServices.Should().ContainSingle(s => s.Name == "Projector");
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookingDoesNotExist_ReturnsNull()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var sut = new BookingRepository(context);

        var result = await sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    #endregion

    #region GetByRoomIdAsync

    [Fact]
    public async Task GetByRoomIdAsync_ReturnsOnlyBookingsForRequestedRoom()
    {
        var dbName = Guid.NewGuid().ToString();
        var roomA = CreateRoom("Room A");
        var roomB = CreateRoom("Room B");
        var bookingA = CreateBooking(roomA.Id, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
        var bookingB = CreateBooking(roomB.Id, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.AddRange(roomA, roomB);
            setup.Bookings.AddRange(bookingA, bookingB);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new BookingRepository(context);

        var result = await sut.GetByRoomIdAsync(roomA.Id);

        result.Should().ContainSingle(b => b.Id == bookingA.Id);
        result.Should().NotContain(b => b.Id == bookingB.Id);
    }

    [Fact]
    public async Task GetByRoomIdAsync_WhenNoBookingsForRoom_ReturnsEmptyList()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var sut = new BookingRepository(context);

        var result = await sut.GetByRoomIdAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    #endregion

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_WithoutFilters_ReturnsAllBookings()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        var booking1 = CreateBooking(room.Id, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
        var booking2 = CreateBooking(room.Id, new DateTime(2026, 9, 2, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc));

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(room);
            setup.Bookings.AddRange(booking1, booking2);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new BookingRepository(context);

        var result = await sut.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WithFromFilter_ExcludesBookingsStartingBeforeFrom()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        var early = CreateBooking(room.Id, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
        var late = CreateBooking(room.Id, new DateTime(2026, 9, 5, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc));

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(room);
            setup.Bookings.AddRange(early, late);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new BookingRepository(context);

        var result = await sut.GetAllAsync(from: new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc));

        result.Should().ContainSingle(b => b.Id == late.Id);
    }

    [Fact]
    public async Task GetAllAsync_WithToFilter_ExcludesBookingsEndingAfterTo()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        var early = CreateBooking(room.Id, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
        var late = CreateBooking(room.Id, new DateTime(2026, 9, 5, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc));

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(room);
            setup.Bookings.AddRange(early, late);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new BookingRepository(context);

        var result = await sut.GetAllAsync(to: new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc));

        result.Should().ContainSingle(b => b.Id == early.Id);
    }

    [Fact]
    public async Task GetAllAsync_WithFromAndToFilters_ReturnsOnlyBookingsWithinRange()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        var before = CreateBooking(room.Id, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
        var within = CreateBooking(room.Id, new DateTime(2026, 9, 3, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc));
        var after = CreateBooking(room.Id, new DateTime(2026, 9, 10, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc));

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(room);
            setup.Bookings.AddRange(before, within, after);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new BookingRepository(context);

        var result = await sut.GetAllAsync(
            from: new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
            to: new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc));

        result.Should().ContainSingle(b => b.Id == within.Id);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoBookingsExist_ReturnsEmptyList()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var sut = new BookingRepository(context);

        var result = await sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    #endregion
}