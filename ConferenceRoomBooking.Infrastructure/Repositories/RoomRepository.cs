using ConferenceRoomBooking.Application.Interfaces.Repositories;
using ConferenceRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Infrastructure.Data.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly AppDbContext _context;

    public RoomRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ConferenceRoom?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Rooms
            .Include(r => r.AvailableServices)
            .Include(r => r.Bookings)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<List<ConferenceRoom>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Rooms
            .Include(r => r.AvailableServices)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<ConferenceRoom>> GetAvailableAsync(
        DateTime start, DateTime end, int? minCapacity, CancellationToken ct = default)
    {
        var query = _context.Rooms
            .Include(r => r.AvailableServices)
            .Include(r => r.Bookings)
            .AsNoTracking()
            .AsQueryable();

        if (minCapacity.HasValue)
            query = query.Where(r => r.Capacity >= minCapacity.Value);

        // Фільтруємо на рівні БД: зал доступний, якщо НЕМАЄ активних бронювань,
        // що перетинаються із запитаним проміжком.
        query = query.Where(r => !r.Bookings.Any(b =>
            b.Status != Domain.Enums.BookingStatus.Cancelled &&
            start < b.EndTime && end > b.StartTime));

        return await query.ToListAsync(ct);
    }

    public async Task AddAsync(ConferenceRoom room, CancellationToken ct = default)
    {
        await _context.Rooms.AddAsync(room, ct);
    }

    public void Update(ConferenceRoom room)
    {
        _context.Rooms.Update(room);
    }

    public async Task<bool> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct) > 0;
    }
}