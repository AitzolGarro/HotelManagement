using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.Models.DTOs;

public class GuestLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string BookingReference { get; set; } = string.Empty;
}

public class GuestLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expires { get; set; }
    public GuestProfileDto Guest { get; set; } = null!;
}

public class GuestProfileDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Nationality { get; set; }
    public string? PreferredLanguage { get; set; }
    public bool MarketingOptIn { get; set; }
    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; }
    public bool IsVip { get; set; }
    public string? VipStatus { get; set; }
    public int LoyaltyPoints { get; set; }
    public string LoyaltyTier { get; set; } = "Standard";
}

public class GuestSpecialRequestDto
{
    [Required(ErrorMessage = "Special request text is required")]
    [StringLength(1000, ErrorMessage = "Special request cannot exceed 1000 characters")]
    public string SpecialRequests { get; set; } = string.Empty;
}

public class GuestNotificationPreferencesDto
{
    public bool BookingConfirmations { get; set; } = true;
    public bool CheckInReminders { get; set; } = true;
    public bool CheckOutReminders { get; set; } = true;
    public bool ModificationConfirmations { get; set; } = true;
    public bool PromotionalOffers { get; set; }
    public bool EmailChannel { get; set; } = true;
    public bool SmsChannel { get; set; }
}