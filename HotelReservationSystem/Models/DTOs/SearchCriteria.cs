using HotelReservationSystem.Models;

namespace HotelReservationSystem.Models.DTOs;

public class ReservationSearchCriteria
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? HotelId { get; set; }
    public List<ReservationStatus>? Statuses { get; set; }
    public List<ReservationSource>? Sources { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? GuestName { get; set; }
    public string? BookingReference { get; set; }
}

public class RoomSearchCriteria
{
    public int? HotelId { get; set; }
    public RoomType? RoomType { get; set; }
    public RoomStatus? Status { get; set; }
    public int? MinCapacity { get; set; }
    public decimal? MaxBaseRate { get; set; }
}