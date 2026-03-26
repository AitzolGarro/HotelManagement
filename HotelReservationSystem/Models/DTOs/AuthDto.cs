using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.Models.DTOs;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string? TwoFactorCode { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expires { get; set; }
    public UserDto User { get; set; } = null!;
    public bool RequiresTwoFactor { get; set; }
    public string? ChallengeToken { get; set; }
    public bool TwoFactorEnabled { get; set; }
}

public class TwoFactorChallengeRequest
{
    [Required]
    public string ChallengeToken { get; set; } = string.Empty;

    [Required]
    [StringLength(8, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public IEnumerable<int> AccessibleHotelIds { get; set; } = new List<int>();
}

public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    public IEnumerable<int> HotelIds { get; set; } = new List<int>();
}

public class UpdateUserRequest
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public IEnumerable<int> HotelIds { get; set; } = new List<int>();
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}
