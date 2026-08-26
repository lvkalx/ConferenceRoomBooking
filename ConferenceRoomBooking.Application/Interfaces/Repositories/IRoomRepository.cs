using ConferenceRoomBooking.Domain.Entities;

namespace ConferenceRoomBooking.Application.Interfaces.Repositories;

public interface IRoomRepository
{
    Task<ConferenceRoom?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ConferenceRoom>> GetAllAsync(CancellationToken ct = default);
    Task<List<ConferenceRoom>> GetAvailableAsync(DateTime start, DateTime end, int? minCapacity, CancellationToken ct = default);
    Task AddAsync(ConferenceRoom room, CancellationToken ct = default);
    void Update(ConferenceRoom room);
    Task<bool> SaveChangesAsync(CancellationToken ct = default);
}