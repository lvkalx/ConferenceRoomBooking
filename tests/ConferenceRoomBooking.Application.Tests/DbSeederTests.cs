using ConferenceRoomBooking.Infrastructure.Data.Seed;
using ConferenceRoomBooking.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Infrastructure.Tests.Seed;

public class DbSeederTests
{
    [Fact]
    public async Task SeedAsync_WhenDatabaseIsEmpty_SeedsThreeRoomsAndThreeServices()
    {
        await using var context = InMemoryDbContextFactory.Create();

        await DbSeeder.SeedAsync(context);

        context.Rooms.Should().HaveCount(3);
        context.Services.Should().HaveCount(3);
    }

    [Fact]
    public async Task SeedAsync_SeedsExpectedRoomNamesAndCapacities()
    {
        await using var context = InMemoryDbContextFactory.Create();

        await DbSeeder.SeedAsync(context);

        context.Rooms.Should().Contain(r => r.Name == "Зал А" && r.Capacity == 50 && r.BaseHourlyRate == 2000m);
        context.Rooms.Should().Contain(r => r.Name == "Зал B" && r.Capacity == 100 && r.BaseHourlyRate == 3500m);
        context.Rooms.Should().Contain(r => r.Name == "Зал C" && r.Capacity == 30 && r.BaseHourlyRate == 1500m);
    }

    [Fact]
    public async Task SeedAsync_AssignsServicesToRoomsAsDescribed()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var context = InMemoryDbContextFactory.Create(dbName))
        {
            await DbSeeder.SeedAsync(context);
        }

        await using var verify = InMemoryDbContextFactory.Create(dbName);
        var rooms = await verify.Rooms
            .Include(r => r.AvailableServices)
            .ToListAsync();

        var roomA = rooms.First(r => r.Name == "Зал А");
        var roomB = rooms.First(r => r.Name == "Зал B");
        var roomC = rooms.First(r => r.Name == "Зал C");

        roomA.AvailableServices.Select(s => s.Name).Should().BeEquivalentTo(new[] { "Проєктор", "Wi-Fi"});
        roomB.AvailableServices.Select(s => s.Name).Should().BeEquivalentTo(new[] { "Проєктор", "Wi-Fi", "Звук" });
        roomC.AvailableServices.Select(s => s.Name).Should().BeEquivalentTo(new[] { "Wi-Fi" });
    }

    [Fact]
    public async Task SeedAsync_WhenRoomsAlreadyExist_DoesNothing()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var setup = InMemoryDbContextFactory.Create(dbName))
        {
            setup.Rooms.Add(new ConferenceRoomBooking.Domain.Entities.ConferenceRoom("Existing Room", 10, 100m));
            await setup.SaveChangesAsync();
        }

        await using (var context = InMemoryDbContextFactory.Create(dbName))
        {
            await DbSeeder.SeedAsync(context);
        }

        await using var verify = InMemoryDbContextFactory.Create(dbName);
        verify.Rooms.Should().ContainSingle(r => r.Name == "Existing Room");
        verify.Services.Should().BeEmpty();
    }

    [Fact]
    public async Task SeedAsync_CalledTwiceInSequence_IsIdempotent()
    {
        var dbName = Guid.NewGuid().ToString();

        await using (var context1 = InMemoryDbContextFactory.Create(dbName))
        {
            await DbSeeder.SeedAsync(context1);
        }

        await using (var context2 = InMemoryDbContextFactory.Create(dbName))
        {
            await DbSeeder.SeedAsync(context2);
        }

        await using var verify = InMemoryDbContextFactory.Create(dbName);
        verify.Rooms.Should().HaveCount(3);
    }
}