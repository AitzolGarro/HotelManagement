using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces;

public interface INotificationService
{
    // Core notification management
    Task<SystemNotificationDto> CreateNotificationAsync(NotificationType type, string title, string message, string? relatedEntityType = null, int? relatedEntityId = null);
    Task<List<SystemNotificationDto>> GetNotificationsAsync(int? hotelId = null, bool unreadOnly = false);
    Task<bool> MarkNotificationAsReadAsync(int notificationId);
    Task<bool> MarkAllNotificationsAsReadAsync(int? hotelId = null);
    Task<int> GetUnreadCountAsync(int? hotelId = null);
    Task<NotificationStatsDto> GetNotificationStatsAsync(int? hotelId = null);
    
    // Email notifications
    Task SendEmailNotificationAsync(string email, string subject, string message);
    
    // System alerts and specialized notifications
    Task SendSystemAlertAsync(NotificationType type, string title, string message, int? hotelId = null);
    Task SendReservationUpdateNotificationAsync(int reservationId, string updateType, string details, int? hotelId = null);
    Task SendConflictNotificationAsync(string conflictType, string details, int? hotelId = null, int? reservationId = null);
    
    // Browser notifications
    Task SendBrowserNotificationAsync(BrowserNotificationRequest request, string? userId = null, int? hotelId = null);
}