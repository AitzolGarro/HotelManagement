using System.ComponentModel.DataAnnotations;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.Models.DTOs;

public class ReportFilterRequest
{
    public int? HotelId { get; set; }
    
    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }
    
    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }
    
    public ReportType ReportType { get; set; }
    public ExportFormat ExportFormat { get; set; } = ExportFormat.Json;
}

public class OccupancyReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? HotelId { get; set; }
    public string? HotelName { get; set; }
    public decimal OverallOccupancyRate { get; set; }
    public int TotalRooms { get; set; }
    public int TotalRoomNights { get; set; }
    public int OccupiedRoomNights { get; set; }
    public List<DailyOccupancyDto> DailyOccupancy { get; set; } = new();
    public List<RoomTypeOccupancyDto> RoomTypeBreakdown { get; set; } = new();
    public List<HotelOccupancyDto> HotelBreakdown { get; set; } = new();
}

public class DailyOccupancyDto
{
    public DateTime Date { get; set; }
    public decimal OccupancyRate { get; set; }
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int AvailableRooms { get; set; }
    public decimal Revenue { get; set; }
    public int ReservationCount { get; set; }
}

public class DailyRevenueDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int ReservationCount { get; set; }
    public decimal AverageRate { get; set; }
    public decimal AverageRevenuePerReservation { get; set; }
}

public class RoomTypeOccupancyDto
{
    public RoomType RoomType { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public int TotalRooms { get; set; }
    public decimal OccupancyRate { get; set; }
    public int OccupiedRoomNights { get; set; }
    public decimal AverageRate { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class HotelOccupancyDto
{
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public decimal OccupancyRate { get; set; }
    public int TotalRooms { get; set; }
    public int OccupiedRoomNights { get; set; }
    public decimal TotalRevenue { get; set; }
    public int ReservationCount { get; set; }
}

public class RevenueReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? HotelId { get; set; }
    public string? HotelName { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal ProjectedRevenue { get; set; }
    public decimal LastPeriodRevenue { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal VariancePercentage { get; set; }
    public decimal AverageRevenuePerDay { get; set; }
    public decimal AverageRevenuePerReservation { get; set; }
    public List<DailyRevenueDto> DailyRevenue { get; set; } = new();
    public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();
    public List<RevenueSourceDto> RevenueBySource { get; set; } = new();
    public List<RoomTypeRevenueDto> RevenueByRoomType { get; set; } = new();
}

public class MonthlyRevenueDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int ReservationCount { get; set; }
    public decimal AverageRevenuePerReservation { get; set; }
    public decimal VarianceFromPreviousMonth { get; set; }
}

public class RevenueSourceDto
{
    public ReservationSource Source { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int ReservationCount { get; set; }
    public decimal Percentage { get; set; }
    public decimal AverageRevenuePerReservation { get; set; }
}

public class RoomTypeRevenueDto
{
    public RoomType RoomType { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int ReservationCount { get; set; }
    public decimal Percentage { get; set; }
    public decimal AverageRate { get; set; }
}

public class GuestPatternReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? HotelId { get; set; }
    public string? HotelName { get; set; }
    public int TotalGuests { get; set; }
    public int UniqueGuests { get; set; }
    public int RepeatGuests { get; set; }
    public decimal RepeatGuestPercentage { get; set; }
    public decimal AverageStayDuration { get; set; }
    public decimal AverageLeadTime { get; set; }
    public List<BookingSourcePatternDto> BookingSourcePatterns { get; set; } = new();
    public List<StayDurationPatternDto> StayDurationPatterns { get; set; } = new();
    public List<SeasonalPatternDto> SeasonalPatterns { get; set; } = new();
    public List<GuestLoyaltyDto> GuestLoyalty { get; set; } = new();
}

public class BookingSourcePatternDto
{
    public ReservationSource Source { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public int ReservationCount { get; set; }
    public decimal Percentage { get; set; }
    public decimal AverageLeadTime { get; set; }
    public decimal AverageStayDuration { get; set; }
    public decimal AverageRevenue { get; set; }
}

public class StayDurationPatternDto
{
    public string DurationRange { get; set; } = string.Empty;
    public int ReservationCount { get; set; }
    public decimal Percentage { get; set; }
    public decimal AverageRevenue { get; set; }
}

public class SeasonalPatternDto
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int ReservationCount { get; set; }
    public decimal OccupancyRate { get; set; }
    public decimal AverageRevenue { get; set; }
    public decimal AverageStayDuration { get; set; }
}

public class GuestLoyaltyDto
{
    public int GuestId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public int TotalReservations { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime FirstStay { get; set; }
    public DateTime LastStay { get; set; }
    public decimal AverageStayDuration { get; set; }
    public string PreferredRoomType { get; set; } = string.Empty;
}

public class ReportExportDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public ExportFormat Format { get; set; }
}

public enum ReportType
{
    Occupancy = 1,
    Revenue = 2,
    GuestPattern = 3,
    Combined = 4
}

public enum ExportFormat
{
    Json = 1,
    Pdf = 2,
    Excel = 3,
    Csv = 4
}