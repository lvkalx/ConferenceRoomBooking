using ConferenceRoomBooking.Domain.Entities;

namespace ConferenceRoomBooking.Application.Interfaces.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Booking>> GetByRoomIdAsync(Guid roomId, CancellationToken ct = default);
    Task<List<Booking>> GetAllAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task AddAsync(Booking booking, CancellationToken ct = default);
    Task<bool> SaveChangesAsync(CancellationToken ct = default);
}