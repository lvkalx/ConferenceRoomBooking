using AutoMapper;
using ConferenceRoomBooking.Application.Common;
using ConferenceRoomBooking.Application.DTOs.Bookings;
using ConferenceRoomBooking.Application.Interfaces.Repositories;
using ConferenceRoomBooking.Application.Interfaces.Services;
using ConferenceRoomBooking.Domain.Entities;
using ConferenceRoomBooking.Domain.Exceptions;
using Npgsql;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IPricingService _pricingService;
    private readonly IMapper _mapper;

    public BookingService(
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository,
        IPricingService pricingService,
        IMapper mapper)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _pricingService = pricingService;
        _mapper = mapper;
    }

    public async Task<Result<BookingDto>> CreateBookingAsync(CreateBookingDto dto, CancellationToken ct = default)
    {
        var room = await _roomRepository.GetByIdAsync(dto.RoomId, ct);
        if (room is null)
            return Result.Failure<BookingDto>($"Зал з ID '{dto.RoomId}' не знайдено.", ResultErrorType.NotFound);

        if (!room.IsAvailable(dto.StartTime, dto.EndTime))
            return Result.Failure<BookingDto>(
                $"Зал вже заброньовано на цей період.", ResultErrorType.Conflict);

        var selectedServices = room.AvailableServices
            .Where(s => dto.SelectedServiceIds.Contains(s.Id))
            .ToList();

        if (selectedServices.Count != dto.SelectedServiceIds.Distinct().Count())
            return Result.Failure<BookingDto>(
                "Одна або більше обраних послуг недоступні у цьому залі.", ResultErrorType.Validation);

        var booking = new Booking(room.Id, dto.StartTime, dto.EndTime, selectedServices);

        var totalPrice = _pricingService.CalculateBookingPrice(
            room.BaseHourlyRate,
            dto.StartTime,
            dto.EndTime,
            selectedServices.Select(s => s.Price));

        booking.SetTotalPrice(totalPrice);
        booking.Confirm();

        try
        {
            await _bookingRepository.AddAsync(booking, ct);
            await _bookingRepository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsExclusionConstraintViolation(ex))
        {
            return Result.Failure<BookingDto>(
                "Зал щойно заброньовано іншим клієнтом на цей період. Спробуйте інший час.",
                ResultErrorType.Conflict);
        }

        var resultDto = _mapper.Map<BookingDto>(booking);
        resultDto.RoomName = room.Name;

        return Result.Success(resultDto);
    }

    public async Task<Result> CancelBookingAsync(Guid bookingId, CancellationToken ct = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId, ct);
        if (booking is null)
            return Result.Failure($"Бронювання з ID '{bookingId}' не знайдено.", ResultErrorType.NotFound);

        booking.Cancel();
        await _bookingRepository.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<BookingDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(id, ct);
        if (booking is null)
            return Result.Failure<BookingDto>($"Бронювання з ID '{id}' не знайдено.", ResultErrorType.NotFound);

        return Result.Success(_mapper.Map<BookingDto>(booking));
    }

    private static bool IsExclusionConstraintViolation(DbUpdateException ex)
    {
        // PostgreSQL SqlState 23P01 = exclusion_violation
        return ex.InnerException is PostgresException { SqlState: "23P01" };
    }
}