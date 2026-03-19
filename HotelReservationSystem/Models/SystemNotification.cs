using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Models;

public class SystemNotification
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public int? HotelId { get; set; }
    public string? UserId { get; set; }
    public bool IsRead { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class NotificationPreference
{
    public int Id { get; set; }

    // UserId is nullable – staff users have a UserId, guests use GuestId instead
    public int? UserId { get; set; }

    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public bool BrowserPushEnabled { get; set; } = true;
    public string Channels { get; set; } = "Email,BrowserPush";

    // Guest portal notification preferences (only set when GuestId is populated)
    public int? GuestId { get; set; }
    public bool BookingConfirmations { get; set; } = true;
    public bool CheckInReminders { get; set; } = true;
    public bool CheckOutReminders { get; set; } = true;
    public bool ModificationConfirmations { get; set; } = true;
    public bool PromotionalOffers { get; set; } = false;
    public bool EmailChannel { get; set; } = true;
    public bool SmsChannel { get; set; } = false;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Guest? Guest { get; set; }
}

public class NotificationTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;   // e.g. "ReservationCreated"
    public string Channel { get; set; } = "Email";           // Email | Sms | BrowserPush | InApp
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsActive { get; set; } = true;
}