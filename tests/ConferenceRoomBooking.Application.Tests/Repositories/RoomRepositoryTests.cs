using ConferenceRoomBooking.Application.Interfaces.Repositories;
using ConferenceRoomBooking.Domain.Entities;
using ConferenceRoomBooking.Infrastructure.Data.Repositories;
using ConferenceRoomBooking.Infrastructure.Tests.TestHelpers;
using FluentAssertions;

namespace ConferenceRoomBooking.Application.Tests.Repositories;

public class RoomRepositoryTests
{
    private static ConferenceRoom CreateRoom(string name = "Room A", int capacity = 50, decimal rate = 2000m) =>
        new(name, capacity, rate);

    #region AddAsync / SaveChangesAsync

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsRoom()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var sut = new RoomRepository(context);
        var room = CreateRoom();

        await sut.AddAsync(room);
        var saved = await sut.SaveChangesAsync();

        saved.Should().BeTrue();
        (await context.Rooms.FindAsync(room.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenNothingToPersist_ReturnsFalse()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var sut = new RoomRepository(context);

        var saved = await sut.SaveChangesAsync();

        saved.Should().BeFalse();
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenRoomExists_ReturnsRoomWithServicesIncluded()
    {
        var dbName = Guid.NewGuid().ToString();
        var roomId = await SeedRoomWithServiceAsync(dbName);

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new RoomRepository(context);

        var result = await sut.GetByIdAsync(roomId);

        result.Should().NotBeNull();
        result!.AvailableServices.Should().ContainSingle(s => s.Name == "Projector");
    }

    [Fact]
    public async Task GetByIdAsync_WhenRoomDoesNotExist_ReturnsNull()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var sut = new RoomRepository(context);

        var result = await sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenRoomIsSoftDeleted_ReturnsNull()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        room.MarkAsDeleted();
        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(room);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new RoomRepository(context);

        var result = await sut.GetByIdAsync(room.Id);

        result.Should().BeNull("the global query filter should hide soft-deleted rooms");
    }

    #endregion

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ExcludesSoftDeletedRooms()
    {
        var dbName = Guid.NewGuid().ToString();
        var active = CreateRoom("Active Room");
        var deleted = CreateRoom("Deleted Room");
        deleted.MarkAsDeleted();

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.AddRange(active, deleted);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new RoomRepository(context);

        var result = await sut.GetAllAsync();

        result.Should().ContainSingle(r => r.Id == active.Id);
        result.Should().NotContain(r => r.Id == deleted.Id);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoRoomsExist_ReturnsEmptyList()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var sut = new RoomRepository(context);

        var result = await sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    #endregion

    #region GetAvailableAsync

    [Fact]
    public async Task GetAvailableAsync_WhenNoBookings_ReturnsRoom()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(room);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new RoomRepository(context);

        var result = await sut.GetAvailableAsync(
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            minCapacity: null);

        result.Should().ContainSingle(r => r.Id == room.Id);
    }

    [Fact]
    public async Task GetAvailableAsync_ExcludesRoomWithOverlappingActiveBooking()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        var booking = new Booking(room.Id,
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            Enumerable.Empty<Service>());

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(room);
            setup.Bookings.Add(booking);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new RoomRepository(context);

        var result = await sut.GetAvailableAsync(
            new DateTime(2026, 9, 1, 11, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 13, 0, 0, DateTimeKind.Utc),
            minCapacity: null);

        result.Should().NotContain(r => r.Id == room.Id);
    }

    [Fact]
    public async Task GetAvailableAsync_IncludesRoomWhenOverlappingBookingIsCancelled()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        var booking = new Booking(room.Id,
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            Enumerable.Empty<Service>());
        booking.Cancel();

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(room);
            setup.Bookings.Add(booking);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new RoomRepository(context);

        var result = await sut.GetAvailableAsync(
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            minCapacity: null);

        result.Should().ContainSingle(r => r.Id == room.Id);
    }

    [Fact]
    public async Task GetAvailableAsync_WhenRequestedRangeTouchesBookingBoundary_ReturnsRoom()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        var booking = new Booking(room.Id,
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            Enumerable.Empty<Service>());

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(room);
            setup.Bookings.Add(booking);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new RoomRepository(context);

        // Requesting exactly [12:00-14:00) right after the existing booking ends at 12:00.
        var result = await sut.GetAvailableAsync(
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 14, 0, 0, DateTimeKind.Utc),
            minCapacity: null);

        result.Should().ContainSingle(r => r.Id == room.Id);
    }

    [Fact]
    public async Task GetAvailableAsync_FiltersOutRoomsBelowMinCapacity()
    {
        var dbName = Guid.NewGuid().ToString();
        var small = CreateRoom("Small Room", capacity: 10);
        var big = CreateRoom("Big Room", capacity: 100);

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.AddRange(small, big);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new RoomRepository(context);

        var result = await sut.GetAvailableAsync(
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            minCapacity: 50);

        result.Should().ContainSingle(r => r.Id == big.Id);
    }

    [Fact]
    public async Task GetAvailableAsync_WithNullMinCapacity_DoesNotFilterByCapacity()
    {
        var dbName = Guid.NewGuid().ToString();
        var small = CreateRoom("Small Room", capacity: 5);

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(small);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new RoomRepository(context);

        var result = await sut.GetAvailableAsync(
            new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            minCapacity: null);

        result.Should().ContainSingle(r => r.Id == small.Id);
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_PersistsModificationsAfterSaveChanges()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(room);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new RoomRepository(context);
        var tracked = await sut.GetByIdAsync(room.Id);
        tracked!.SetBaseHourlyRate(9999m);

        sut.Update(tracked);
        await sut.SaveChangesAsync();

        await using var verifyContext = InMemoryDbContextFactory.Create(dbName);
        var reloaded = await verifyContext.Rooms.FindAsync(room.Id);
        reloaded!.BaseHourlyRate.Should().Be(9999m);
    }

    #endregion

    private static async Task<Guid> SeedRoomWithServiceAsync(string dbName)
    {
        var room = CreateRoom();
        room.AddService(new Service("Projector", 500m));

        await using var context = InMemoryDbContextFactory.Create(dbName);
        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        return room.Id;
    }
}