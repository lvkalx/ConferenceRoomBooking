using AutoMapper;
using ConferenceRoomBooking.Application.Common;
using ConferenceRoomBooking.Application.DTOs.Bookings;
using ConferenceRoomBooking.Application.DTOs.Rooms;
using ConferenceRoomBooking.Application.Interfaces.Repositories;
using ConferenceRoomBooking.Application.Mapping;
using ConferenceRoomBooking.Application.Services;
using ConferenceRoomBooking.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ConferenceRoomBooking.Application.Tests.Services;

public class RoomServiceTests
{
    private readonly Mock<IRoomRepository> _roomRepository = new();
    private readonly IMapper _mapper;
    private readonly RoomService _sut;

    public RoomServiceTests()
    {
        _mapper = CreateMapper();
        _sut = new RoomService(_roomRepository.Object, _mapper);
    }

    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        return config.CreateMapper();
    }

    private static ConferenceRoom CreateRoom(
        string name = "Room A",
        int capacity = 10,
        decimal rate = 100m) =>
        new(name, capacity, rate);

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_WithServices_PersistsRoomAndReturnsMappedDto()
    {
        var dto = new CreateRoomDto
        {
            Name = "Hall A",
            Capacity = 20,
            BaseHourlyRate = 500m,
            Services =
            [
                new CreateServiceDto
                {
                    Name = "Projector",
                    Price = 50m
                }
            ]
        };

        _roomRepository
            .Setup(r => r.AddAsync(
                It.IsAny<ConferenceRoom>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _roomRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.CreateAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        result.Value!.Name.Should().Be("Hall A");
        result.Value.Capacity.Should().Be(20);
        result.Value.BaseHourlyRate.Should().Be(500m);

        result.Value.AvailableServices.Should()
            .ContainSingle(s =>
                s.Name == "Projector" &&
                s.Price == 50m);

        _roomRepository.Verify(
            r => r.AddAsync(
                It.Is<ConferenceRoom>(room =>
                    room.Name == "Hall A" &&
                    room.Capacity == 20 &&
                    room.BaseHourlyRate == 500m &&
                    room.AvailableServices.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _roomRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithoutServices_PersistsRoomWithEmptyServiceList()
    {
        var dto = new CreateRoomDto
        {
            Name = "Hall B",
            Capacity = 5,
            BaseHourlyRate = 100m
        };

        _roomRepository
            .Setup(r => r.AddAsync(
                It.IsAny<ConferenceRoom>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _roomRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.CreateAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AvailableServices.Should().BeEmpty();

        _roomRepository.Verify(
            r => r.AddAsync(
                It.Is<ConferenceRoom>(room =>
                    room.Name == "Hall B" &&
                    room.Capacity == 5 &&
                    room.BaseHourlyRate == 100m &&
                    room.AvailableServices.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _roomRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_WhenRoomNotFound_ReturnsNotFoundFailure()
    {
        _roomRepository
            .Setup(r => r.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConferenceRoom?)null);

        var result = await _sut.UpdateAsync(
            Guid.NewGuid(),
            new UpdateRoomDto
            {
                Name = "Whatever"
            });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);

        _roomRepository.Verify(
            r => r.Update(It.IsAny<ConferenceRoom>()),
            Times.Never);

        _roomRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithOnlyNameProvided_UpdatesNameAndLeavesOtherFieldsUnchanged()
    {
        var room = CreateRoom("Old Name", 10, 100m);

        _roomRepository
            .Setup(r => r.GetByIdAsync(
                room.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        _roomRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.UpdateAsync(
            room.Id,
            new UpdateRoomDto
            {
                Name = "New Name"
            });

        result.IsSuccess.Should().BeTrue();

        room.Name.Should().Be("New Name");
        room.Capacity.Should().Be(10);
        room.BaseHourlyRate.Should().Be(100m);

        _roomRepository.Verify(
            r => r.Update(room),
            Times.Once);

        _roomRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithCapacityAndRateProvided_UpdatesOnlyThoseFields()
    {
        var room = CreateRoom("Room X", 10, 100m);

        _roomRepository
            .Setup(r => r.GetByIdAsync(
                room.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        _roomRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.UpdateAsync(
            room.Id,
            new UpdateRoomDto
            {
                Capacity = 30,
                BaseHourlyRate = 250m
            });

        result.IsSuccess.Should().BeTrue();

        room.Name.Should().Be("Room X");
        room.Capacity.Should().Be(30);
        room.BaseHourlyRate.Should().Be(250m);

        _roomRepository.Verify(
            r => r.Update(room),
            Times.Once);

        _roomRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithServicesToAddAndRemove_AppliesBothChanges()
    {
        var room = CreateRoom();

        var oldService = new Service("Old Service", 15m);
        room.AddService(oldService);

        _roomRepository
            .Setup(r => r.GetByIdAsync(
                room.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        _roomRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new UpdateRoomDto
        {
            ServicesToAdd =
            [
                new CreateServiceDto
                {
                    Name = "WiFi",
                    Price = 10m
                }
            ],
            ServiceIdsToRemove =
            [
                oldService.Id
            ]
        };

        var result = await _sut.UpdateAsync(room.Id, dto);

        result.IsSuccess.Should().BeTrue();

        room.AvailableServices.Should()
            .ContainSingle(s =>
                s.Name == "WiFi" &&
                s.Price == 10m);

        _roomRepository.Verify(
            r => r.Update(room),
            Times.Once);

        _roomRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ReplaceAsync

    [Fact]
    public async Task ReplaceAsync_WhenRoomNotFound_ReturnsNotFoundFailure()
    {
        _roomRepository
            .Setup(r => r.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConferenceRoom?)null);

        var result = await _sut.ReplaceAsync(
            Guid.NewGuid(),
            new ReplaceRoomDto
            {
                Name = "X",
                Capacity = 1,
                BaseHourlyRate = 1m
            });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);

        _roomRepository.Verify(
            r => r.Update(It.IsAny<ConferenceRoom>()),
            Times.Never);

        _roomRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplaceAsync_ReplacesAllFieldsAndFullyReplacesServiceList()
    {
        var room = CreateRoom("Old Name", 10, 100m);
        room.AddService(new Service("Old Service", 15m));

        _roomRepository
            .Setup(r => r.GetByIdAsync(
                room.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        _roomRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new ReplaceRoomDto
        {
            Name = "Replaced",
            Capacity = 99,
            BaseHourlyRate = 999m,
            Services =
            [
                new CreateServiceDto
                {
                    Name = "Sound System",
                    Price = 20m
                }
            ]
        };

        var result = await _sut.ReplaceAsync(room.Id, dto);

        result.IsSuccess.Should().BeTrue();

        room.Name.Should().Be("Replaced");
        room.Capacity.Should().Be(99);
        room.BaseHourlyRate.Should().Be(999m);

        room.AvailableServices.Should()
            .ContainSingle(s =>
                s.Name == "Sound System" &&
                s.Price == 20m);

        room.AvailableServices
            .Should()
            .NotContain(s => s.Name == "Old Service");

        _roomRepository.Verify(
            r => r.Update(room),
            Times.Once);

        _roomRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_WhenRoomNotFound_ReturnsNotFoundFailure()
    {
        _roomRepository
            .Setup(r => r.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConferenceRoom?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);

        _roomRepository.Verify(
            r => r.Update(It.IsAny<ConferenceRoom>()),
            Times.Never);

        _roomRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenRoomExists_MarksAsDeletedAndPersists()
    {
        var room = CreateRoom();

        _roomRepository
            .Setup(r => r.GetByIdAsync(
                room.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        _roomRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.DeleteAsync(room.Id);

        result.IsSuccess.Should().BeTrue();
        room.IsDeleted.Should().BeTrue();

        _roomRepository.Verify(
            r => r.Update(room),
            Times.Once);

        _roomRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenRoomNotFound_ReturnsNotFoundFailure()
    {
        _roomRepository
            .Setup(r => r.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConferenceRoom?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRoomExists_ReturnsMappedDto()
    {
        var room = CreateRoom("Hall C", 15, 300m);
        room.AddService(new Service("Projector", 50m));

        _roomRepository
            .Setup(r => r.GetByIdAsync(
                room.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        var result = await _sut.GetByIdAsync(room.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        result.Value!.Id.Should().Be(room.Id);
        result.Value.Name.Should().Be("Hall C");
        result.Value.Capacity.Should().Be(15);
        result.Value.BaseHourlyRate.Should().Be(300m);

        result.Value.AvailableServices.Should()
            .ContainSingle(s =>
                s.Name == "Projector" &&
                s.Price == 50m);
    }

    #endregion

    #region GetAvailableAsync

    [Fact]
    public async Task GetAvailableAsync_PassesCriteriaToRepositoryAndReturnsMappedRooms()
    {
        var start = new DateTime(
            2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

        var end = new DateTime(
            2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

        var roomA = CreateRoom("A");
        var roomB = CreateRoom("B");

        var rooms = new List<ConferenceRoom>
        {
            roomA,
            roomB
        };

        _roomRepository
            .Setup(r => r.GetAvailableAsync(
                start,
                end,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rooms);

        var result = await _sut.GetAvailableAsync(
            new AvailabilityRequestDto
            {
                StartTime = start,
                EndTime = end,
                MinCapacity = 5
            });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        result.Value!
            .Select(r => r.Name)
            .Should()
            .BeEquivalentTo("A", "B");

        _roomRepository.Verify(
            r => r.GetAvailableAsync(
                start,
                end,
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAvailableAsync_WhenNoRoomsMatch_ReturnsEmptyList()
    {
        var start = new DateTime(
            2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

        var end = new DateTime(
            2026, 9, 1, 11, 0, 0, DateTimeKind.Utc);

        _roomRepository
            .Setup(r => r.GetAvailableAsync(
                start,
                end,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.GetAvailableAsync(
            new AvailabilityRequestDto
            {
                StartTime = start,
                EndTime = end
            });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        _roomRepository.Verify(
            r => r.GetAvailableAsync(
                start,
                end,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}