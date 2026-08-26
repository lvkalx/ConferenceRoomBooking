using ConferenceRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Infrastructure.Data.Seed;

/// <summary>
/// Наповнює базу початковими даними (зали А/B/C, базові послуги) при першому запуску.
/// Ідемпотентний — якщо дані вже є, нічого не робить.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Rooms.AnyAsync())
            return; // вже засіяно — виходимо

        var projector = new Service("Проєктор", 500m);
        var wifi = new Service("Wi-Fi", 300m);
        var sound = new Service("Звук", 700m);

        await context.Services.AddRangeAsync(projector, wifi, sound);

        var roomA = new ConferenceRoom("Зал А", capacity: 50, baseHourlyRate: 2000m);
        roomA.AddService(projector);
        roomA.AddService(wifi);

        var roomB = new ConferenceRoom("Зал B", capacity: 100, baseHourlyRate: 3500m);
        roomB.AddService(projector);
        roomB.AddService(wifi);
        roomB.AddService(sound);

        var roomC = new ConferenceRoom("Зал C", capacity: 30, baseHourlyRate: 1500m);
        roomC.AddService(wifi);

        await context.Rooms.AddRangeAsync(roomA, roomB, roomC);

        await context.SaveChangesAsync();
    }
}