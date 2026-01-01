using System.ComponentModel.DataAnnotations;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.Models.DTOs;

public class CreateReservationRequest
{
    [Required(ErrorMessage = "Hotel ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Hotel ID must be a positive number")]
    public int HotelId { get; set; }

    [Required(ErrorMessage = "Room ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Room ID must be a positive number")]
    public int RoomId { get; set; }

    [Required(ErrorMessage = "Guest ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Guest ID must be a positive number")]
    public int GuestId { get; set; }

    [StringLength(50, ErrorMessage = "Booking reference cannot exceed 50 characters")]
    public string? BookingReference { get; set; }

    [Required(ErrorMessage = "Reservation source is required")]
    public ReservationSource Source { get; set; }

    [Required(ErrorMessage = "Check-in date is required")]
    public DateTime CheckInDate { get; set; }

    [Required(ErrorMessage = "Check-out date is required")]
    public DateTime CheckOutDate { get; set; }

    [Required(ErrorMessage = "Number of guests is required")]
    [Range(1, 20, ErrorMessage = "Number of guests must be between 1 and 20")]
    public int NumberOfGuests { get; set; }

    [Required(ErrorMessage = "Total amount is required")]
    [Range(0.01, 100000.00, ErrorMessage = "Total amount must be between 0.01 and 100000.00")]
    public decimal TotalAmount { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    [StringLength(1000, ErrorMessage = "Special requests cannot exceed 1000 characters")]
    public string? SpecialRequests { get; set; }

    [StringLength(1000, ErrorMessage = "Internal notes cannot exceed 1000 characters")]
    public string? InternalNotes { get; set; }
}

public class UpdateReservationRequest
{
    [Required(ErrorMessage = "Check-in date is required")]
    public DateTime CheckInDate { get; set; }

    [Required(ErrorMessage = "Check-out date is required")]
    public DateTime CheckOutDate { get; set; }

    [Required(ErrorMessage = "Number of guests is required")]
    [Range(1, 20, ErrorMessage = "Number of guests must be between 1 and 20")]
    public int NumberOfGuests { get; set; }

    [Required(ErrorMessage = "Total amount is required")]
    [Range(0.01, 100000.00, ErrorMessage = "Total amount must be between 0.01 and 100000.00")]
    public decimal TotalAmount { get; set; }

    [Required(ErrorMessage = "Status is required")]
    public ReservationStatus Status { get; set; }

    [StringLength(1000, ErrorMessage = "Special requests cannot exceed 1000 characters")]
    public string? SpecialRequests { get; set; }

    [StringLength(1000, ErrorMessage = "Internal notes cannot exceed 1000 characters")]
    public string? InternalNotes { get; set; }
}

public class CancelReservationRequest
{
    [Required(ErrorMessage = "Cancellation reason is required")]
    [StringLength(500, ErrorMessage = "Cancellation reason cannot exceed 500 characters")]
    public string Reason { get; set; } = string.Empty;
}

public class ReservationDto
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public int RoomId { get; set; }
    public int GuestId { get; set; }
    public string? BookingReference { get; set; }
    public ReservationSource Source { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int NumberOfGuests { get; set; }
    public decimal TotalAmount { get; set; }
    public ReservationStatus Status { get; set; }
    public string? SpecialRequests { get; set; }
    public string? InternalNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public string HotelName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
}

public class ConflictDto
{
    public int ReservationId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public ReservationStatus Status { get; set; }
    public string ConflictType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateReservationStatusRequest
{
    [Required(ErrorMessage = "Reservation status is required")]
    public ReservationStatus Status { get; set; }
}

public class AvailabilityCheckRequest
{
    [Required(ErrorMessage = "Room ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Room ID must be a positive number")]
    public int RoomId { get; set; }

    [Required(ErrorMessage = "Check-in date is required")]
    public DateTime CheckInDate { get; set; }

    [Required(ErrorMessage = "Check-out date is required")]
    public DateTime CheckOutDate { get; set; }

    public int? ExcludeReservationId { get; set; }
}

public class CreateManualReservationRequest
{
    [Required(ErrorMessage = "Hotel ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Hotel ID must be a positive number")]
    public int HotelId { get; set; }

    [Required(ErrorMessage = "Room ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Room ID must be a positive number")]
    public int RoomId { get; set; }

    [StringLength(50, ErrorMessage = "Booking reference cannot exceed 50 characters")]
    public string? BookingReference { get; set; }

    [Required(ErrorMessage = "Check-in date is required")]
    public DateTime CheckInDate { get; set; }

    [Required(ErrorMessage = "Check-out date is required")]
    public DateTime CheckOutDate { get; set; }

    [Required(ErrorMessage = "Number of guests is required")]
    [Range(1, 20, ErrorMessage = "Number of guests must be between 1 and 20")]
    public int NumberOfGuests { get; set; }

    [Required(ErrorMessage = "Total amount is required")]
    [Range(0.01, 100000.00, ErrorMessage = "Total amount must be between 0.01 and 100000.00")]
    public decimal TotalAmount { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    [StringLength(1000, ErrorMessage = "Special requests cannot exceed 1000 characters")]
    public string? SpecialRequests { get; set; }

    [StringLength(1000, ErrorMessage = "Internal notes cannot exceed 1000 characters")]
    public string? InternalNotes { get; set; }

    // Guest information
    [Required(ErrorMessage = "Guest first name is required")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string GuestFirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Guest last name is required")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string GuestLastName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public string? GuestEmail { get; set; }

    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string? GuestPhone { get; set; }

    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string? GuestAddress { get; set; }

    [StringLength(50, ErrorMessage = "Document number cannot exceed 50 characters")]
    public string? GuestDocumentNumber { get; set; }
}