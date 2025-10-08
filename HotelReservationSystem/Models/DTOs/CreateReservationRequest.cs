namespace HotelReservationSystem.Models.DTOs
{
    public class CreateReservationRequest
    {
        public int HotelId { get; set; }
        public int RoomId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; }
        public decimal TotalAmount { get; set; }
        public GuestDto Guest { get; set; } = new();
        public string? SpecialRequests { get; set; }
        public string? InternalNotes { get; set; }
    }

    public class UpdateReservationRequest
    {
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public int? NumberOfGuests { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? SpecialRequests { get; set; }
        public string? InternalNotes { get; set; }
        public ReservationStatus? Status { get; set; }
    }

    public class CancelReservationRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}