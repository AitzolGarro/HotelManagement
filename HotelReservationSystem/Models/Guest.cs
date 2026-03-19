namespace HotelReservationSystem.Models;

public class Guest
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Nationality { get; set; }
    public bool IsVip { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<GuestPreference> Preferences { get; set; } = new List<GuestPreference>();
    public ICollection<GuestNote> Notes { get; set; } = new List<GuestNote>();
}