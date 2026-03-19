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
    public bool IsVip { get; set; }
    public int LoyaltyPoints { get; set; }
}