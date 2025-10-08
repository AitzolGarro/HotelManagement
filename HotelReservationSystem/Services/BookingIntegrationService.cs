using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services;

public class BookingIntegrationService : IBookingIntegrationService
{
    private readonly ILogger<BookingIntegrationService> _logger;

    public BookingIntegrationService(ILogger<BookingIntegrationService> logger)
    {
        _logger = logger;
    }

    public Task SyncReservationsAsync()
    {
        _logger.LogInformation("Booking.com synchronization not yet implemented");
        return Task.CompletedTask;
    }

    public Task PushAvailabilityUpdateAsync(int roomId, DateTime date, int availableCount)
    {
        _logger.LogInformation("Availability push to Booking.com not yet implemented for room {RoomId}", roomId);
        return Task.CompletedTask;
    }

    public Task<BookingReservationDto> FetchReservationAsync(string bookingReference)
    {
        _logger.LogInformation("Fetching reservation from Booking.com not yet implemented for reference {BookingReference}", bookingReference);
        return Task.FromResult(new BookingReservationDto());
    }

    public Task HandleWebhookAsync(string xmlPayload)
    {
        _logger.LogInformation("Webhook handling not yet implemented");
        return Task.CompletedTask;
    }
}