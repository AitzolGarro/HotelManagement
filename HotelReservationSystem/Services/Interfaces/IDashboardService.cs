using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardKpiDto> GetDashboardKpiAsync(int? hotelId = null);
    Task<OccupancyRateDto> GetOccupancyRatesAsync(int? hotelId = null);
    Task<RevenueTrackingDto> GetRevenueTrackingAsync(int? hotelId = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<DailyOperationsDto> GetDailyOperationsAsync(DateTime? date = null, int? hotelId = null);
    Task<NotificationPanelDto> GetNotificationPanelAsync(int? hotelId = null);
    Task<SystemNotificationDto> CreateNotificationAsync(NotificationType type, string title, string message, string? relatedEntityType = null, int? relatedEntityId = null);
    Task<bool> MarkNotificationAsReadAsync(int notificationId);
    Task<bool> MarkAllNotificationsAsReadAsync(int? hotelId = null);
    Task<List<SystemNotificationDto>> GetUnreadNotificationsAsync(int? hotelId = null);
    Task<List<DailyRevenueDto>> GetDailyRevenueBreakdownAsync(DateTime startDate, DateTime endDate, int? hotelId = null);
    Task<List<WeeklyRevenueDto>> GetWeeklyRevenueBreakdownAsync(DateTime startDate, DateTime endDate, int? hotelId = null);
    Task<decimal> CalculateOccupancyRateAsync(DateTime startDate, DateTime endDate, int? hotelId = null);
    Task<decimal> CalculateRevenueAsync(DateTime startDate, DateTime endDate, int? hotelId = null);
    Task<List<RecentReservationDto>> GetRecentReservationsAsync(int? hotelId = null, int limit = 10);
}