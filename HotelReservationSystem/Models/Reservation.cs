namespace HotelReservationSystem.Models;

public class Reservation
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
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public string? SpecialRequests { get; set; }
    public string? InternalNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Hotel Hotel { get; set; } = null!;
    public Room Room { get; set; } = null!;
    public Guest Guest { get; set; } = null!;
}

public enum ReservationSource
{
    Manual = 1,
    BookingCom = 2,
    Direct = 3,
    Other = 4
}

public enum ReservationStatus
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3,
    CheckedIn = 4,
    CheckedOut = 5,
    NoShow = 6
}