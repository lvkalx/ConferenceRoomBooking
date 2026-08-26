using AutoMapper;
using ConferenceRoomBooking.Application.Common;
using ConferenceRoomBooking.Application.DTOs.Bookings;
using ConferenceRoomBooking.Application.DTOs.Rooms;
using ConferenceRoomBooking.Application.Interfaces.Repositories;
using ConferenceRoomBooking.Application.Interfaces.Services;
using ConferenceRoomBooking.Domain.Entities;

namespace ConferenceRoomBooking.Application.Services;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IMapper _mapper;

    public RoomService(IRoomRepository roomRepository, IMapper mapper)
    {
        _roomRepository = roomRepository;
        _mapper = mapper;
    }

    public async Task<Result<RoomDto>> CreateAsync(CreateRoomDto dto, CancellationToken ct = default)
    {
        var room = new ConferenceRoom(dto.Name, dto.Capacity, dto.BaseHourlyRate);

        foreach (var serviceDto in dto.Services)
            room.AddService(new Service(serviceDto.Name, serviceDto.Price));

        await _roomRepository.AddAsync(room, ct);
        await _roomRepository.SaveChangesAsync(ct);

        return Result.Success(_mapper.Map<RoomDto>(room));
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateRoomDto dto, CancellationToken ct = default)
    {
        var room = await _roomRepository.GetByIdAsync(id, ct);
        if (room is null)
            return Result.Failure($"Зал з ID '{id}' не знайдено.", ResultErrorType.NotFound);

        if (dto.Name is not null)
            room.SetName(dto.Name);

        if (dto.Capacity is not null)
            room.SetCapacity(dto.Capacity.Value);

        if (dto.BaseHourlyRate is not null)
            room.SetBaseHourlyRate(dto.BaseHourlyRate.Value);

        if (dto.ServicesToAdd is not null)
            foreach (var s in dto.ServicesToAdd)
                room.AddService(new Service(s.Name, s.Price));

        if (dto.ServiceIdsToRemove is not null)
            foreach (var serviceId in dto.ServiceIdsToRemove)
                room.RemoveService(serviceId);

        _roomRepository.Update(room);
        await _roomRepository.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> ReplaceAsync(Guid id, ReplaceRoomDto dto, CancellationToken ct = default)
    {
        var room = await _roomRepository.GetByIdAsync(id, ct);
        if (room is null)
            return Result.Failure($"Зал з ID '{id}' не знайдено.", ResultErrorType.NotFound);

        // PUT — повна заміна стану ресурсу, без часткової логіки "якщо передано".
        room.SetName(dto.Name);
        room.SetCapacity(dto.Capacity);
        room.SetBaseHourlyRate(dto.BaseHourlyRate);
        room.ReplaceServices(dto.Services.Select(s => new Service(s.Name, s.Price)));

        _roomRepository.Update(room);
        await _roomRepository.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var room = await _roomRepository.GetByIdAsync(id, ct);
        if (room is null)
            return Result.Failure($"Зал з ID '{id}' не знайдено.", ResultErrorType.NotFound);

        room.MarkAsDeleted(); // soft delete — зберігаємо історію бронювань
        _roomRepository.Update(room);
        await _roomRepository.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<List<RoomDto>>> GetAvailableAsync(AvailabilityRequestDto dto, CancellationToken ct = default)
    {
        var rooms = await _roomRepository.GetAvailableAsync(dto.StartTime, dto.EndTime, dto.MinCapacity, ct);
        return Result.Success(_mapper.Map<List<RoomDto>>(rooms));
    }

    public async Task<Result<RoomDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var room = await _roomRepository.GetByIdAsync(id, ct);
        if (room is null)
            return Result.Failure<RoomDto>($"Зал з ID '{id}' не знайдено.", ResultErrorType.NotFound);

        return Result.Success(_mapper.Map<RoomDto>(room));
    }
}