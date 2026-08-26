using ConferenceRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<ConferenceRoom> Rooms => Set<ConferenceRoom>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Service> Services => Set<Service>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Необхідне розширення PostgreSQL для EXCLUDE constraint по (Guid + tsrange)
        modelBuilder.HasPostgresExtension("btree_gist");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}