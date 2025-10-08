namespace HotelReservationSystem.Models.DTOs
{
    public class CreateHotelRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class UpdateHotelRequest
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; }
    }

    public class CreateRoomRequest
    {
        public int HotelId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public RoomType Type { get; set; }
        public int Capacity { get; set; }
        public decimal BaseRate { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateRoomRequest
    {
        public string? RoomNumber { get; set; }
        public RoomType? Type { get; set; }
        public int? Capacity { get; set; }
        public decimal? BaseRate { get; set; }
        public RoomStatus? Status { get; set; }
        public string? Description { get; set; }
    }
}