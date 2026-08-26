using ConferenceRoomBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Infrastructure.Tests.TestHelpers;

/// <summary>
/// Creates isolated <see cref="AppDbContext"/> instances backed by the EF Core InMemory provider.
/// Each context gets a uniquely named database (unless one is explicitly reused), so tests never
/// leak state into one another even when xunit runs them in parallel.
///
/// Note: the InMemory provider does not enforce relational-only concerns (CHECK constraints,
/// decimal(18,2) column precision, Postgres extensions). Those are Npgsql/DB-level concerns and
/// are intentionally out of scope for these tests, which target repository/query/reporting logic.
/// </summary>
public static class InMemoryDbContextFactory
{
    public static AppDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        return new AppDbContext(options);
    }
}