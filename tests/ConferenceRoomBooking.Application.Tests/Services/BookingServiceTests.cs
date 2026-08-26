using AutoMapper;
using ConferenceRoomBooking.Application.Common;
using ConferenceRoomBooking.Application.DTOs.Bookings;
using ConferenceRoomBooking.Application.Interfaces.Repositories;
using ConferenceRoomBooking.Application.Interfaces.Services;
using ConferenceRoomBooking.Application.Mapping;
using ConferenceRoomBooking.Application.Services;
using ConferenceRoomBooking.Domain.Entities;
using ConferenceRoomBooking.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ConferenceRoomBooking.Application.Tests.Services;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepository = new();
    private readonly Mock<IRoomRepository> _roomRepository = new();
    private readonly Mock<IPricingService> _pricingService = new();

    private readonly IMapper _mapper;
    private readonly BookingService _sut;

    public BookingServiceTests()
    {
        _mapper = CreateMapper();

        _sut = new BookingService(
            _bookingRepository.Object,
            _roomRepository.Object,
            _pricingService.Object,
            _mapper);
    }

    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance);

        return config.CreateMapper();
    }

    private static ConferenceRoom CreateRoom(
        string name = "Зал А",
        int capacity = 50,
        decimal hourlyRate = 2000m)
    {
        return new ConferenceRoom(name, capacity, hourlyRate);
    }

    private static CreateBookingDto CreateBookingDto(
        Guid roomId,
        DateTime? start = null,
        DateTime? end = null,
        IEnumerable<Guid>? serviceIds = null)
    {
        return new CreateBookingDto
        {
            RoomId = roomId,
            StartTime = start ?? new DateTime(2026, 9, 1, 10, 0, 0),
            EndTime = end ?? new DateTime(2026, 9, 1, 12, 0, 0),
            SelectedServiceIds = serviceIds?.ToList() ?? []
        };
    }

    #region CreateBookingAsync

    [Fact]
    public async Task CreateBookingAsync_WhenRoomNotFound_ReturnsNotFound()
    {
        var roomId = Guid.NewGuid();

        _roomRepository
            .Setup(r => r.GetByIdAsync(
                roomId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConferenceRoom?)null);

        var result = await _sut.CreateBookingAsync(
            CreateBookingDto(roomId));

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);

        _bookingRepository.Verify(
            r => r.AddAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _bookingRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenRoomIsUnavailable_ReturnsConflict()
    {
        var room = CreateRoom();

        var existingBooking = new Booking(
            room.Id,
            new DateTime(2026, 9, 1, 10, 0, 0),
            new DateTime(2026, 9, 1, 12, 0, 0),
            Enumerable.Empty<Service>());

        var bookingsField = typeof(ConferenceRoom)
            .GetField(
                "_bookings",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);

        var bookings = (List<Booking>)bookingsField!.GetValue(room)!;
        bookings.Add(existingBooking);

        _roomRepository
            .Setup(r => r.GetByIdAsync(
                room.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        var result = await _sut.CreateBookingAsync(
            CreateBookingDto(room.Id));

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);

        _bookingRepository.Verify(
            r => r.AddAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenSelectedServiceIsUnavailable_ReturnsValidationError()
    {
        var room = CreateRoom();

        var availableService = new Service("Проєктор", 500m);
        room.AddService(availableService);

        var unavailableServiceId = Guid.NewGuid();

        _roomRepository
            .Setup(r => r.GetByIdAsync(
                room.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        var dto = CreateBookingDto(
            room.Id,
            serviceIds: [availableService.Id, unavailableServiceId]);

        var result = await _sut.CreateBookingAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Validation);

        _bookingRepository.Verify(
            r => r.AddAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_WithValidData_CreatesAndConfirmsBooking()
    {
        var room = CreateRoom();

        var projector = new Service("Проєктор", 500m);
        var wifi = new Service("Wi-Fi", 300m);

        room.AddService(projector);
        room.AddService(wifi);

        _roomRepository
            .Setup(r => r.GetByIdAsync(
                room.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        _pricingService
            .Setup(p => p.CalculateBookingPrice(
                room.BaseHourlyRate,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<IEnumerable<decimal>>()))
            .Returns(4300m);

        _bookingRepository
            .Setup(r => r.AddAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _bookingRepository
            .Setup(r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = CreateBookingDto(
            room.Id,
            serviceIds: [projector.Id, wifi.Id]);

        var result = await _sut.CreateBookingAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        result.Value!.RoomId.Should().Be(room.Id);
        result.Value.RoomName.Should().Be(room.Name);
        result.Value.StartTime.Should().Be(dto.StartTime);
        result.Value.EndTime.Should().Be(dto.EndTime);
        result.Value.TotalPrice.Should().Be(4300m);
        result.Value.Status.Should().Be(BookingStatus.Confirmed);

        result.Value.SelectedServices.Should()
            .BeEquivalentTo("Проєктор", "Wi-Fi");

        _bookingRepository.Verify(
            r => r.AddAsync(
                It.Is<Booking>(b =>
                    b.RoomId == room.Id &&
                    b.TotalPrice == 4300m &&
                    b.Status == BookingStatus.Confirmed &&
                    b.SelectedServices.Count == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _bookingRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WithoutServices_CreatesBookingWithNoServices()
    {
        var room = CreateRoom();

        _roomRepository
            .Setup(r => r.GetByIdAsync(
                room.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        _pricingService
            .Setup(p => p.CalculateBookingPrice(
                room.BaseHourlyRate,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<IEnumerable<decimal>>()))
            .Returns(4000m);

        _bookingRepository
            .Setup(r => r.AddAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _bookingRepository
            .Setup(r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.CreateBookingAsync(
            CreateBookingDto(room.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SelectedServices.Should().BeEmpty();
        result.Value.TotalPrice.Should().Be(4000m);

        _pricingService.Verify(
            p => p.CalculateBookingPrice(
                room.BaseHourlyRate,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.Is<IEnumerable<decimal>>(prices => !prices.Any())),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_PassesSelectedServicePricesToPricingService()
    {
        var room = CreateRoom();

        var projector = new Service("Проєктор", 500m);
        var wifi = new Service("Wi-Fi", 300m);

        room.AddService(projector);
        room.AddService(wifi);

        _roomRepository
            .Setup(r => r.GetByIdAsync(
                room.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        _pricingService
            .Setup(p => p.CalculateBookingPrice(
                It.IsAny<decimal>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<IEnumerable<decimal>>()))
            .Returns(4300m);

        _bookingRepository
            .Setup(r => r.AddAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _bookingRepository
            .Setup(r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = CreateBookingDto(
            room.Id,
            serviceIds: [projector.Id, wifi.Id]);

        var result = await _sut.CreateBookingAsync(dto);

        result.IsSuccess.Should().BeTrue();

        _pricingService.Verify(
            p => p.CalculateBookingPrice(
                room.BaseHourlyRate,
                dto.StartTime,
                dto.EndTime,
                It.Is<IEnumerable<decimal>>(prices =>
                    prices.OrderBy(x => x)
                        .SequenceEqual(new[] { 300m, 500m }))),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenSaveChangesFailsWithExclusionViolation_ReturnsConflict()
    {
        var room = CreateRoom();

        _roomRepository
            .Setup(r => r.GetByIdAsync(
                room.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        _pricingService
            .Setup(p => p.CalculateBookingPrice(
                It.IsAny<decimal>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<IEnumerable<decimal>>()))
            .Returns(4000m);

        _bookingRepository
            .Setup(r => r.AddAsync(
                It.IsAny<Booking>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _bookingRepository
            .Setup(r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new DbUpdateException(
                    "Exclusion constraint violation.",
                    new Npgsql.PostgresException(
                        "exclusion violation",
                        "ERROR",
                        "ERROR",
                        "23P01")));

        var result = await _sut.CreateBookingAsync(
            CreateBookingDto(room.Id));

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
    }

    #endregion

    #region CancelBookingAsync

    [Fact]
    public async Task CancelBookingAsync_WhenBookingNotFound_ReturnsNotFound()
    {
        var bookingId = Guid.NewGuid();

        _bookingRepository
            .Setup(r => r.GetByIdAsync(
                bookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);

        var result = await _sut.CancelBookingAsync(bookingId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);

        _bookingRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelBookingAsync_WhenBookingExists_CancelsAndSaves()
    {
        var booking = new Booking(
            Guid.NewGuid(),
            new DateTime(2026, 9, 1, 10, 0, 0),
            new DateTime(2026, 9, 1, 12, 0, 0),
            Enumerable.Empty<Service>());

        booking.Confirm();

        _bookingRepository
            .Setup(r => r.GetByIdAsync(
                booking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        _bookingRepository
            .Setup(r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.CancelBookingAsync(booking.Id);

        result.IsSuccess.Should().BeTrue();
        booking.Status.Should().Be(BookingStatus.Cancelled);

        _bookingRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenBookingNotFound_ReturnsNotFound()
    {
        var bookingId = Guid.NewGuid();

        _bookingRepository
            .Setup(r => r.GetByIdAsync(
                bookingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);

        var result = await _sut.GetByIdAsync(bookingId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookingExists_ReturnsMappedDto()
    {
        var room = CreateRoom("Зал B");

        var projector = new Service("Проєктор", 500m);
        room.AddService(projector);

        var booking = new Booking(
            room.Id,
            new DateTime(2026, 9, 1, 10, 0, 0),
            new DateTime(2026, 9, 1, 12, 0, 0),
            [projector]);

        booking.SetTotalPrice(4500m);
        booking.Confirm();

        // MappingProfile бере RoomName з booking.Room.
        typeof(Booking)
            .GetProperty(nameof(Booking.Room))!
            .SetValue(booking, room);

        _bookingRepository
            .Setup(r => r.GetByIdAsync(
                booking.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        var result = await _sut.GetByIdAsync(booking.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        result.Value!.Id.Should().Be(booking.Id);
        result.Value.RoomId.Should().Be(room.Id);
        result.Value.RoomName.Should().Be("Зал B");
        result.Value.TotalPrice.Should().Be(4500m);
        result.Value.Status.Should().Be(BookingStatus.Confirmed);

        result.Value.SelectedServices
            .Should()
            .ContainSingle()
            .Which.Should()
            .Be("Проєктор");
    }

    #endregion
}