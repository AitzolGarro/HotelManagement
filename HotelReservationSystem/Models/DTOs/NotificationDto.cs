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
    BookingComSync = 8
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