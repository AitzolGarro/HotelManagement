using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.Models.DTOs;

public class UpdateReservationDatesRequest
{
    [Required]
    public DateTime CheckInDate { get; set; }
    
    [Required]
    public DateTime CheckOutDate { get; set; }
    
    public int? RoomId { get; set; } // Optional: allow moving to a different room via drag-and-drop
}