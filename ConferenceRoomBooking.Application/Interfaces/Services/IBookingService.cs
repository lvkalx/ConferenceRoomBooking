using ConferenceRoomBooking.Application.Common;
using ConferenceRoomBooking.Application.DTOs.Bookings;

namespace ConferenceRoomBooking.Application.Interfaces.Services;

public interface IBookingService
{
    Task<Result<BookingDto>> CreateBookingAsync(CreateBookingDto dto, CancellationToken ct = default);
    Task<Result> CancelBookingAsync(Guid bookingId, CancellationToken ct = default);
    Task<Result<BookingDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
}