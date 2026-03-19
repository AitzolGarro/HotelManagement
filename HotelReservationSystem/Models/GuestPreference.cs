namespace HotelReservationSystem.Models;

public class GuestPreference
{
    public int Id { get; set; }
    public int GuestId { get; set; }
    public string Category { get; set; } = string.Empty; // e.g., "Room", "Food", "General"
    public string Preference { get; set; } = string.Empty; // e.g., "High Floor", "Vegetarian"
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guest? Guest { get; set; }
}