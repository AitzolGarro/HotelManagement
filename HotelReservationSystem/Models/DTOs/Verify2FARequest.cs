using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.Models.DTOs;

public class Verify2FARequest
{
    [Required]
    [StringLength(8, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}
