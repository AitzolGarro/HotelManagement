namespace HotelReservationSystem.Models;

public class Room
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public RoomType Type { get; set; }
    public int Capacity { get; set; }
    public decimal BaseRate { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Hotel Hotel { get; set; } = null!;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}

public enum RoomType
{
    Single = 1,
    Double = 2,
    Suite = 3,
    Family = 4,
    Deluxe = 5,
    Twin = 6,
    Triple = 7,
    Quad = 8,
    Standard = 9
}

public enum RoomStatus
{
    Available = 1,
    Maintenance = 2,
    Blocked = 3,
    OutOfOrder = 4,
    Occupied = 5,
    Cleaning = 6
}