using System.ComponentModel.DataAnnotations;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.Models.DTOs.GuestPortal;

// ─── Authentication ───────────────────────────────────────────────────────────

public class GuestLoginViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Booking reference is required")]
    public string BookingReference { get; set; } = string.Empty;
}

// ─── Dashboard ────────────────────────────────────────────────────────────────

public class GuestDashboardViewModel
{
    public GuestPortalProfileViewModel Profile { get; set; } = null!;
    public List<GuestReservationSummaryViewModel> UpcomingReservations { get; set; } = new();
    public List<GuestReservationSummaryViewModel> PastReservations { get; set; } = new();
    public int LoyaltyPoints { get; set; }
    public string LoyaltyTier { get; set; } = "Standard";
}

// ─── Reservation ─────────────────────────────────────────────────────────────

public class GuestReservationSummaryViewModel
{
    public int Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int Nights => (CheckOutDate - CheckInDate).Days;
    public int NumberOfGuests { get; set; }
    public decimal TotalAmount { get; set; }
    public ReservationStatus Status { get; set; }
    public string? SpecialRequests { get; set; }
    public bool CanModify => Status == ReservationStatus.Confirmed || Status == ReservationStatus.Pending;
    public bool CanCancel => Status == ReservationStatus.Confirmed || Status == ReservationStatus.Pending;
    public bool IsUpcoming => CheckInDate > DateTime.UtcNow;
}

public class GuestReservationDetailViewModel
{
    public int Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string HotelAddress { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int Nights => (CheckOutDate - CheckInDate).Days;
    public int NumberOfGuests { get; set; }
    public decimal TotalAmount { get; set; }
    public ReservationStatus Status { get; set; }
    public string? SpecialRequests { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool CanModify => Status == ReservationStatus.Confirmed || Status == ReservationStatus.Pending;
    public bool CanCancel => Status == ReservationStatus.Confirmed || Status == ReservationStatus.Pending;
}

public class GuestModifyReservationViewModel
{
    public int ReservationId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Check-in date is required")]
    public DateTime CheckInDate { get; set; }

    [Required(ErrorMessage = "Check-out date is required")]
    public DateTime CheckOutDate { get; set; }
}

public class GuestCancelReservationViewModel
{
    public int ReservationId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public decimal TotalAmount { get; set; }

    [Required(ErrorMessage = "Please provide a reason for cancellation")]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class GuestSpecialRequestViewModel
{
    public int ReservationId { get; set; }
    public string BookingReference { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your special request")]
    [StringLength(1000, ErrorMessage = "Special request cannot exceed 1000 characters")]
    public string SpecialRequests { get; set; } = string.Empty;
}

// ─── Profile ──────────────────────────────────────────────────────────────────

public class GuestPortalProfileViewModel
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
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

public class GuestUpdateProfileViewModel
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? Nationality { get; set; }

    [StringLength(10)]
    public string? PreferredLanguage { get; set; }

    public bool MarketingOptIn { get; set; }
    public bool EmailNotifications { get; set; }
    public bool SmsNotifications { get; set; }
}

// ─── Notifications ────────────────────────────────────────────────────────────

public class GuestNotificationPreferencesViewModel
{
    public bool BookingConfirmations { get; set; } = true;
    public bool CheckInReminders { get; set; } = true;
    public bool CheckOutReminders { get; set; } = true;
    public bool ModificationConfirmations { get; set; } = true;
    public bool PromotionalOffers { get; set; }
    public bool EmailChannel { get; set; } = true;
    public bool SmsChannel { get; set; }
}
