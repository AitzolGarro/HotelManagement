using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces;

public interface INotificationService
{
    // Core notification management
    Task<SystemNotificationDto> CreateNotificationAsync(NotificationType type, string title, string message, string? relatedEntityType = null, int? relatedEntityId = null);
    Task<List<SystemNotificationDto>> GetNotificationsAsync(int? hotelId = null, bool unreadOnly = false);
    Task<PagedResultDto<SystemNotificationDto>> GetUserNotificationsAsync(string? userId, int? hotelId, bool unreadOnly, NotificationType? typeFilter, int page, int pageSize);
    Task<bool> MarkNotificationAsReadAsync(int notificationId);
    Task<bool> MarkAllNotificationsAsReadAsync(int? hotelId = null);
    Task<bool> DeleteNotificationAsync(int notificationId);
    Task<int> GetUnreadCountAsync(int? hotelId = null);
    Task<NotificationStatsDto> GetNotificationStatsAsync(int? hotelId = null);

    // Preferences
    Task<NotificationPreferenceDto> GetUserPreferencesAsync(int? userId, int? guestId);
    Task<NotificationPreferenceDto> UpdateUserPreferencesAsync(int? userId, int? guestId, UpdateNotificationPreferencesRequest request);

    // Email notifications
    Task SendEmailNotificationAsync(string email, string subject, string message);
    Task SendTemplatedEmailAsync(string email, string eventType, Dictionary<string, string> variables);

    // SMS notifications
    Task SendSmsNotificationAsync(string phoneNumber, string message);
    Task SendTemplatedSmsAsync(string phoneNumber, string eventType, Dictionary<string, string> variables);

    // System alerts and specialized notifications
    Task SendSystemAlertAsync(NotificationType type, string title, string message, int? hotelId = null);
    Task SendReservationUpdateNotificationAsync(int reservationId, string updateType, string details, int? hotelId = null);
    Task SendConflictNotificationAsync(string conflictType, string details, int? hotelId = null, int? reservationId = null);

    // Browser notifications
    Task SendBrowserNotificationAsync(BrowserNotificationRequest request, string? userId = null, int? hotelId = null);

    // Reservation-specific notifications
    Task NotifyReservationCreatedAsync(int reservationId, int hotelId);
    Task NotifyReservationUpdatedAsync(int reservationId, int hotelId);
    Task NotifyReservationCancelledAsync(int reservationId, int hotelId);
}
