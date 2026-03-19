namespace HotelReservationSystem.Models;

public class GuestNote
{
    public int Id { get; set; }
    public int GuestId { get; set; }
    public string Note { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guest? Guest { get; set; }
    public User? CreatedByUser { get; set; }
}