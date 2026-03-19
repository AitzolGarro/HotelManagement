using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Models;

public class SystemNotification
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public int? HotelId { get; set; }
    public string? UserId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationPreference
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public bool BrowserPushEnabled { get; set; } = true;
    public string Channels { get; set; } = "Email,BrowserPush"; // comma separated string for ease
    
    public User? User { get; set; }
}

public class NotificationTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
}