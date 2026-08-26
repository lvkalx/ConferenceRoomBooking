namespace ConferenceRoomBooking.Application.Interfaces.Services;

public interface IReportService
{
    Task<RoomOccupancyReportDto> GetOccupancyReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<RevenueReportDto> GetRevenueReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<PopularServiceDto>> GetPopularServicesAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

public class RoomOccupancyReportDto
{
    public List<RoomOccupancyItemDto> Rooms { get; set; } = new();
}

public class RoomOccupancyItemDto
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = null!;
    public int TotalBookings { get; set; }
    public decimal OccupancyRatePercent { get; set; }
    public TimeSpan TotalBookedHours { get; set; }
}

public class RevenueReportDto
{
    public decimal TotalRevenue { get; set; }
    public List<RoomRevenueItemDto> ByRoom { get; set; } = new();
}

public class RoomRevenueItemDto
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = null!;
    public decimal Revenue { get; set; }
    public int BookingsCount { get; set; }
}

public class PopularServiceDto
{
    public string ServiceName { get; set; } = null!;
    public int TimesBooked { get; set; }
    public decimal TotalRevenue { get; set; }
}