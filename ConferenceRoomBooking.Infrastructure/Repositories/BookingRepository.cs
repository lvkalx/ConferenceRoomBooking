using ConferenceRoomBooking.Application.Interfaces.Repositories;
using ConferenceRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Infrastructure.Data.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Bookings
            .Include(b => b.Room)
            .Include(b => b.SelectedServices)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<List<Booking>> GetByRoomIdAsync(Guid roomId, CancellationToken ct = default)
    {
        return await _context.Bookings
            .Include(b => b.SelectedServices)
            .Where(b => b.RoomId == roomId)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<Booking>> GetAllAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var query = _context.Bookings
            .Include(b => b.Room)
            .Include(b => b.SelectedServices)
            .AsNoTracking()
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(b => b.StartTime >= from.Value);

        if (to.HasValue)
            query = query.Where(b => b.EndTime <= to.Value);

        return await query.ToListAsync(ct);
    }

    public async Task AddAsync(Booking booking, CancellationToken ct = default)
    {
        await _context.Bookings.AddAsync(booking, ct);
    }

    public async Task<bool> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct) > 0;
    }
}