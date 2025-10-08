using System.ComponentModel.DataAnnotations;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.Models.DTOs;

public class CreateRoomRequest
{
    [Required(ErrorMessage = "Hotel ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Hotel ID must be a positive number")]
    public int HotelId { get; set; }

    [Required(ErrorMessage = "Room number is required")]
    [StringLength(10, ErrorMessage = "Room number cannot exceed 10 characters")]
    public string RoomNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Room type is required")]
    public RoomType Type { get; set; }

    [Required(ErrorMessage = "Capacity is required")]
    [Range(1, 20, ErrorMessage = "Capacity must be between 1 and 20")]
    public int Capacity { get; set; }

    [Required(ErrorMessage = "Base rate is required")]
    [Range(0.01, 10000.00, ErrorMessage = "Base rate must be between 0.01 and 10000.00")]
    public decimal BaseRate { get; set; }

    public RoomStatus Status { get; set; } = RoomStatus.Available;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
}

public class UpdateRoomRequest
{
    [Required(ErrorMessage = "Room number is required")]
    [StringLength(10, ErrorMessage = "Room number cannot exceed 10 characters")]
    public string RoomNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Room type is required")]
    public RoomType Type { get; set; }

    [Required(ErrorMessage = "Capacity is required")]
    [Range(1, 20, ErrorMessage = "Capacity must be between 1 and 20")]
    public int Capacity { get; set; }

    [Required(ErrorMessage = "Base rate is required")]
    [Range(0.01, 10000.00, ErrorMessage = "Base rate must be between 0.01 and 10000.00")]
    public decimal BaseRate { get; set; }

    public RoomStatus Status { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
}

public class UpdateRoomStatusRequest
{
    [Required(ErrorMessage = "Room status is required")]
    public RoomStatus Status { get; set; }
}

public class RoomDto
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public RoomType Type { get; set; }
    public int Capacity { get; set; }
    public decimal BaseRate { get; set; }
    public RoomStatus Status { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string HotelName { get; set; } = string.Empty;
}