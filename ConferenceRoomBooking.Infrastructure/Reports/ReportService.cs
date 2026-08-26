using ConferenceRoomBooking.Application.Interfaces.Services;
using ConferenceRoomBooking.Domain.Enums;
using ConferenceRoomBooking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Infrastructure.Reports;

/// <summary>
/// Формує аналітичні звіти для бізнесу: завантаженість залів, дохід, популярність послуг.
/// Враховує лише бронювання зі статусом, відмінним від Cancelled.
/// </summary>
public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RoomOccupancyReportDto> GetOccupancyReportAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (to <= from)
            throw new ArgumentException("Дата 'to' має бути пізніше 'from'.");

        var totalPeriodHours = (decimal)(to - from).TotalHours;

        // IgnoreQueryFilters — звіт має враховувати навіть soft-deleted зали
        // (історична аналітика не повинна "губити" видалені зали)
        var rooms = await _context.Rooms
            .IgnoreQueryFilters()
            .Select(r => new
            {
                r.Id,
                r.Name,
                Bookings = r.Bookings
                    .Where(b => b.Status != BookingStatus.Cancelled)
                    .Where(b => b.StartTime < to && b.EndTime > from)
                    .Select(b => new { b.StartTime, b.EndTime })
                    .ToList()
            })
            .ToListAsync(ct);

        var items = rooms.Select(r =>
        {
            var bookedHours = r.Bookings.Sum(b =>
                (decimal)(Min(b.EndTime, to) - Max(b.StartTime, from)).TotalHours);

            var occupancyRate = totalPeriodHours > 0
                ? Math.Round(bookedHours / totalPeriodHours * 100m, 2)
                : 0m;

            return new RoomOccupancyItemDto
            {
                RoomId = r.Id,
                RoomName = r.Name,
                TotalBookings = r.Bookings.Count,
                OccupancyRatePercent = occupancyRate,
                TotalBookedHours = TimeSpan.FromHours((double)bookedHours)
            };
        }).ToList();

        return new RoomOccupancyReportDto { Rooms = items };
    }

    public async Task<RevenueReportDto> GetRevenueReportAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var bookings = await _context.Bookings
            .Include(b => b.Room)
            .Where(b => b.Status != BookingStatus.Cancelled)
            .Where(b => b.StartTime >= from && b.StartTime < to)
            .ToListAsync(ct);

        var byRoom = bookings
            .GroupBy(b => new { b.RoomId, RoomName = b.Room != null ? b.Room.Name : "Видалений зал" })
            .Select(g => new RoomRevenueItemDto
            {
                RoomId = g.Key.RoomId,
                RoomName = g.Key.RoomName,
                Revenue = g.Sum(b => b.TotalPrice),
                BookingsCount = g.Count()
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        return new RevenueReportDto
        {
            TotalRevenue = bookings.Sum(b => b.TotalPrice),
            ByRoom = byRoom
        };
    }

    public async Task<List<PopularServiceDto>> GetPopularServicesAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var bookings = await _context.Bookings
            .Include(b => b.SelectedServices)
            .Where(b => b.Status != BookingStatus.Cancelled)
            .Where(b => b.StartTime >= from && b.StartTime < to)
            .ToListAsync(ct);

        var result = bookings
            .SelectMany(b => b.SelectedServices)
            .GroupBy(s => s.Name)
            .Select(g => new PopularServiceDto
            {
                ServiceName = g.Key,
                TimesBooked = g.Count(),
                TotalRevenue = g.Sum(s => s.Price)
            })
            .OrderByDescending(x => x.TimesBooked)
            .ToList();

        return result;
    }

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
}