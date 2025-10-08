using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces;

public interface IReportingService
{
    // Occupancy Reports
    Task<OccupancyReportDto> GenerateOccupancyReportAsync(DateTime startDate, DateTime endDate, int? hotelId = null);
    Task<List<DailyOccupancyDto>> GetDailyOccupancyAsync(DateTime startDate, DateTime endDate, int? hotelId = null);
    Task<List<RoomTypeOccupancyDto>> GetRoomTypeOccupancyAsync(DateTime startDate, DateTime endDate, int? hotelId = null);
    Task<List<HotelOccupancyDto>> GetHotelOccupancyComparisonAsync(DateTime startDate, DateTime endDate);

    // Revenue Reports
    Task<RevenueReportDto> GenerateRevenueReportAsync(DateTime startDate, DateTime endDate, int? hotelId = null);
    Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync(DateTime startDate, DateTime endDate, int? hotelId = null);
    Task<List<RevenueSourceDto>> GetRevenueBySourceAsync(DateTime startDate, DateTime endDate, int? hotelId = null);
    Task<List<RoomTypeRevenueDto>> GetRevenueByRoomTypeAsync(DateTime startDate, DateTime endDate, int? hotelId = null);

    // Guest Pattern Reports
    Task<GuestPatternReportDto> GenerateGuestPatternReportAsync(DateTime startDate, DateTime endDate, int? hotelId = null);
    Task<List<BookingSourcePatternDto>> GetBookingSourcePatternsAsync(DateTime startDate, DateTime endDate, int? hotelId = null);
    Task<List<StayDurationPatternDto>> GetStayDurationPatternsAsync(DateTime startDate, DateTime endDate, int? hotelId = null);
    Task<List<SeasonalPatternDto>> GetSeasonalPatternsAsync(DateTime startDate, DateTime endDate, int? hotelId = null);
    Task<List<GuestLoyaltyDto>> GetGuestLoyaltyAnalysisAsync(DateTime startDate, DateTime endDate, int? hotelId = null);

    // Export Functionality
    Task<ReportExportDto> ExportReportAsync(ReportFilterRequest request);
    Task<byte[]> ExportToPdfAsync<T>(T reportData, string reportTitle);
    Task<byte[]> ExportToExcelAsync<T>(T reportData, string reportTitle);
    Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> reportData);

    // Variance Analysis
    Task<decimal> CalculateOccupancyVarianceAsync(DateTime currentStart, DateTime currentEnd, DateTime previousStart, DateTime previousEnd, int? hotelId = null);
    Task<decimal> CalculateRevenueVarianceAsync(DateTime currentStart, DateTime currentEnd, DateTime previousStart, DateTime previousEnd, int? hotelId = null);
}