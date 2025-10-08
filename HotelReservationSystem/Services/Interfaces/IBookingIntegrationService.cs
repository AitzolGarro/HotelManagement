using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces;

public interface IBookingIntegrationService
{
    Task SyncReservationsAsync();
    Task PushAvailabilityUpdateAsync(int roomId, DateTime date, int availableCount);
    Task<BookingReservationDto> FetchReservationAsync(string bookingReference);
    Task HandleWebhookAsync(string xmlPayload);
}

public class BookingReservationDto
{
    public string BookingReference { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int NumberOfGuests { get; set; }
    public decimal TotalAmount { get; set; }
}