using Microsoft.AspNetCore.Identity;

namespace HotelReservationSystem.Models;

public class User : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties for property-level access control
    public ICollection<UserHotelAccess> HotelAccess { get; set; } = new List<UserHotelAccess>();
}

public enum UserRole
{
    Staff = 1,
    Manager = 2,
    Admin = 3
}

public class UserHotelAccess
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int HotelId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Hotel Hotel { get; set; } = null!;
}