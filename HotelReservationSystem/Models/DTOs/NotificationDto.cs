using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.Models.DTOs;

public enum NotificationType
{
    Info = 1,
    Warning = 2,
    Error = 3,
    Success = 4,
    ReservationUpdate = 5,
    Conflict = 6,
    SystemAlert = 7,
    BookingComSync = 8,
    Critical = 9,
    Overbooking = 10,
    MaintenanceConflict = 11
}

public enum NotificationPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

public class SystemNotificationDto
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public int? HotelId { get; set; }
    public string? UserId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class CreateNotificationRequest
{
    [Required]
    public NotificationType Type { get; set; }
    
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;
    
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public int? HotelId { get; set; }
    public string? UserId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class EmailNotificationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public Dictionary<string, object>? Metadata { get; set; }
}

public class BrowserNotificationRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Body { get; set; } = string.Empty;
    
    public string? Icon { get; set; }
    public string? Badge { get; set; }
    public string? Tag { get; set; }
    public bool RequireInteraction { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}

public class NotificationStatsDto
{
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int TodayCount { get; set; }
    public Dictionary<NotificationType, int> CountByType { get; set; } = new();
    public Dictionary<NotificationPriority, int> CountByPriority { get; set; } = new();
}

public class NotificationPreferenceDto
{
    public int Id { get; set; }
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public bool BrowserPushEnabled { get; set; } = true;
    public bool BookingConfirmations { get; set; } = true;
    public bool CheckInReminders { get; set; } = true;
    public bool CheckOutReminders { get; set; } = true;
    public bool ModificationConfirmations { get; set; } = true;
    public bool PromotionalOffers { get; set; } = false;
    public bool EmailChannel { get; set; } = true;
    public bool SmsChannel { get; set; } = false;
}

public class UpdateNotificationPreferencesRequest
{
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public bool BrowserPushEnabled { get; set; } = true;
    public bool BookingConfirmations { get; set; } = true;
    public bool CheckInReminders { get; set; } = true;
    public bool CheckOutReminders { get; set; } = true;
    public bool ModificationConfirmations { get; set; } = true;
    public bool PromotionalOffers { get; set; } = false;
    public bool EmailChannel { get; set; } = true;
    public bool SmsChannel { get; set; } = false;
}

public class SmsNotificationRequest
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(160)]
    public string Message { get; set; } = string.Empty;
}
