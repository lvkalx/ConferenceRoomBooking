using ConferenceRoomBooking.Domain.Entities;
using ConferenceRoomBooking.Infrastructure.Reports;
using ConferenceRoomBooking.Infrastructure.Tests.TestHelpers;
using FluentAssertions;

namespace ConferenceRoomBooking.Infrastructure.Tests.Reports;

public class ReportServiceTests
{
    private static ConferenceRoom CreateRoom(string name = "Room A", int capacity = 50, decimal rate = 2000m) =>
        new(name, capacity, rate);

    private static Booking CreateBooking(
        Guid roomId, DateTime start, DateTime end, decimal totalPrice = 0m, IEnumerable<Service>? services = null)
    {
        var booking = new Booking(roomId, start, end, services ?? Enumerable.Empty<Service>());
        booking.SetTotalPrice(totalPrice);
        return booking;
    }

    #region GetOccupancyReportAsync

    [Fact]
    public async Task GetOccupancyReportAsync_WhenToIsNotAfterFrom_ThrowsArgumentException()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var sut = new ReportService(context);
        var from = new DateTime(2026, 9, 1);
        var to = new DateTime(2026, 9, 1);

        var act = async () => await sut.GetOccupancyReportAsync(from, to);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetOccupancyReportAsync_ComputesOccupancyRateForFullyOverlappingBooking()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        // Booking occupies the whole reporting window: 10:00-12:00 out of a 10:00-12:00 window => 100%
        var booking = CreateBooking(room.Id, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(room);
            setup.Bookings.Add(booking);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new ReportService(context);

        var result = await sut.GetOccupancyReportAsync(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));

        var item = result.Rooms.Should().ContainSingle(r => r.RoomId == room.Id).Subject;
        item.TotalBookings.Should().Be(1);
        item.OccupancyRatePercent.Should().Be(100m);
        item.TotalBookedHours.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public async Task GetOccupancyReportAsync_ClipsBookedHoursToReportingWindow()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        // Booking runs 08:00-14:00, but the report window is only 10:00-12:00 (2h out of 4h window = 50%)
        var booking = CreateBooking(room.Id, new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 14, 0, 0, DateTimeKind.Utc));

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(room);
            setup.Bookings.Add(booking);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new ReportService(context);

        var result = await sut.GetOccupancyReportAsync(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 14, 0, 0, DateTimeKind.Utc));

        var item = result.Rooms.Should().ContainSingle(r => r.RoomId == room.Id).Subject;
        item.TotalBookedHours.Should().Be(TimeSpan.FromHours(4));
        item.OccupancyRatePercent.Should().Be(100m);
    }

    [Fact]
    public async Task GetOccupancyReportAsync_ExcludesCancelledBookings()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        var booking = CreateBooking(room.Id, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
        booking.Cancel();

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(room);
            setup.Bookings.Add(booking);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new ReportService(context);

        var result = await sut.GetOccupancyReportAsync(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));

        var item = result.Rooms.Should().ContainSingle(r => r.RoomId == room.Id).Subject;
        item.TotalBookings.Should().Be(0);
        item.OccupancyRatePercent.Should().Be(0m);
    }

    [Fact]
    public async Task GetOccupancyReportAsync_ExcludesBookingsOutsideRequestedRange()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        var booking = CreateBooking(room.Id, new DateTime(2026, 9, 1, 1, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 2, 0, 0, DateTimeKind.Utc));

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(room);
            setup.Bookings.Add(booking);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new ReportService(context);

        var result = await sut.GetOccupancyReportAsync(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));

        var item = result.Rooms.Should().ContainSingle(r => r.RoomId == room.Id).Subject;
        item.TotalBookings.Should().Be(0);
    }

    [Fact]
    public async Task GetOccupancyReportAsync_IncludesSoftDeletedRooms()
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
        var sut = new ReportService(context);

        var result = await sut.GetOccupancyReportAsync(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));

        result.Rooms.Should().ContainSingle(r => r.RoomId == room.Id, "the report must not hide historical (soft-deleted) rooms");
    }

    [Fact]
    public async Task GetOccupancyReportAsync_WhenNoRoomsExist_ReturnsEmptyReport()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var sut = new ReportService(context);

        var result = await sut.GetOccupancyReportAsync(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));

        result.Rooms.Should().BeEmpty();
    }

    #endregion

    #region GetRevenueReportAsync

    [Fact]
    public async Task GetRevenueReportAsync_SumsRevenuePerRoomAndTotal()
    {
        var dbName = Guid.NewGuid().ToString();
        var roomA = CreateRoom("Room A");
        var roomB = CreateRoom("Room B");
        var bookingA1 = CreateBooking(roomA.Id, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc), totalPrice: 1000m);
        var bookingA2 = CreateBooking(roomA.Id, new DateTime(2026, 9, 2, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc), totalPrice: 500m);
        var bookingB1 = CreateBooking(roomB.Id, new DateTime(2026, 9, 3, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc), totalPrice: 2000m);

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.AddRange(roomA, roomB);
            setup.Bookings.AddRange(bookingA1, bookingA2, bookingB1);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new ReportService(context);

        var result = await sut.GetRevenueReportAsync(new DateTime(2026, 9, 1), new DateTime(2026, 9, 10));

        result.TotalRevenue.Should().Be(3500m);
        result.ByRoom.Should().HaveCount(2);
        result.ByRoom.Should().ContainSingle(r => r.RoomId == roomA.Id && r.Revenue == 1500m && r.BookingsCount == 2);
        result.ByRoom.Should().ContainSingle(r => r.RoomId == roomB.Id && r.Revenue == 2000m && r.BookingsCount == 1);
    }

    [Fact]
    public async Task GetRevenueReportAsync_OrdersRoomsByRevenueDescending()
    {
        var dbName = Guid.NewGuid().ToString();
        var roomLow = CreateRoom("Low");
        var roomHigh = CreateRoom("High");
        var bookingLow = CreateBooking(roomLow.Id, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc), totalPrice: 100m);
        var bookingHigh = CreateBooking(roomHigh.Id, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc), totalPrice: 9000m);

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.AddRange(roomLow, roomHigh);
            setup.Bookings.AddRange(bookingLow, bookingHigh);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new ReportService(context);

        var result = await sut.GetRevenueReportAsync(new DateTime(2026, 9, 1), new DateTime(2026, 9, 2));

        result.ByRoom.Select(r => r.RoomId).Should().ContainInOrder(roomHigh.Id, roomLow.Id);
    }

    [Fact]
    public async Task GetRevenueReportAsync_ExcludesCancelledBookings()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        var booking = CreateBooking(room.Id, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc), totalPrice: 1000m);
        booking.Cancel();

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(room);
            setup.Bookings.Add(booking);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new ReportService(context);

        var result = await sut.GetRevenueReportAsync(new DateTime(2026, 9, 1), new DateTime(2026, 9, 2));

        result.TotalRevenue.Should().Be(0m);
        result.ByRoom.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRevenueReportAsync_FiltersByBookingStartTime()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        var inRange = CreateBooking(room.Id, new DateTime(2026, 9, 5, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc), totalPrice: 1000m);
        var outOfRange = CreateBooking(room.Id, new DateTime(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc), totalPrice: 5000m);

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(room);
            setup.Bookings.AddRange(inRange, outOfRange);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new ReportService(context);

        var result = await sut.GetRevenueReportAsync(new DateTime(2026, 9, 1), new DateTime(2026, 10, 1));

        result.TotalRevenue.Should().Be(1000m);
    }

    [Fact]
    public async Task GetRevenueReportAsync_WhenNoBookingsInRange_ReturnsZeroTotalAndEmptyList()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var sut = new ReportService(context);

        var result = await sut.GetRevenueReportAsync(new DateTime(2026, 9, 1), new DateTime(2026, 9, 2));

        result.TotalRevenue.Should().Be(0m);
        result.ByRoom.Should().BeEmpty();
    }

    #endregion

    #region GetPopularServicesAsync

    [Fact]
    public async Task GetPopularServicesAsync_AggregatesTimesBookedAndRevenueByServiceName()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        var projector = new Service("Projector", 500m);
        var wifi = new Service("Wi-Fi", 200m);

        var booking1 = CreateBooking(room.Id, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc), services: new[] { projector, wifi });
        var booking2 = CreateBooking(room.Id, new DateTime(2026, 9, 2, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc), services: new[] { projector });

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Services.AddRange(projector, wifi);
            setup.Rooms.Add(room);
            setup.Bookings.AddRange(booking1, booking2);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new ReportService(context);

        var result = await sut.GetPopularServicesAsync(new DateTime(2026, 9, 1), new DateTime(2026, 9, 10));

        result.Should().ContainSingle(s => s.ServiceName == "Projector" && s.TimesBooked == 2 && s.TotalRevenue == 1000m);
        result.Should().ContainSingle(s => s.ServiceName == "Wi-Fi" && s.TimesBooked == 1 && s.TotalRevenue == 200m);
    }

    [Fact]
    public async Task GetPopularServicesAsync_OrdersByTimesBookedDescending()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        var popular = new Service("Popular", 100m);
        var rare = new Service("Rare", 100m);

        var booking1 = CreateBooking(room.Id, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc), services: new[] { popular, rare });
        var booking2 = CreateBooking(room.Id, new DateTime(2026, 9, 2, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc), services: new[] { popular });

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Services.AddRange(popular, rare);
            setup.Rooms.Add(room);
            setup.Bookings.AddRange(booking1, booking2);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new ReportService(context);

        var result = await sut.GetPopularServicesAsync(new DateTime(2026, 9, 1), new DateTime(2026, 9, 10));

        result.Select(s => s.ServiceName).Should().ContainInOrder("Popular", "Rare");
    }

    [Fact]
    public async Task GetPopularServicesAsync_ExcludesCancelledBookings()
    {
        var dbName = Guid.NewGuid().ToString();
        var room = CreateRoom();
        var service = new Service("Projector", 500m);
        var booking = CreateBooking(room.Id, new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc), services: new[] { service });
        booking.Cancel();

        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Services.Add(service);
            setup.Rooms.Add(room);
            setup.Bookings.Add(booking);
            await setup.SaveChangesAsync();
        }

        await using var context = InMemoryDbContextFactory.Create(dbName);
        var sut = new ReportService(context);

        var result = await sut.GetPopularServicesAsync(new DateTime(2026, 9, 1), new DateTime(2026, 9, 10));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPopularServicesAsync_WhenNoBookingsInRange_ReturnsEmptyList()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var sut = new ReportService(context);

        var result = await sut.GetPopularServicesAsync(new DateTime(2026, 9, 1), new DateTime(2026, 9, 10));

        result.Should().BeEmpty();
    }

    #endregion
}