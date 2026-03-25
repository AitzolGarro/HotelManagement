using HotelReservationSystem.Models;

namespace HotelReservationSystem.Services.Interfaces;

public interface IBookingIntegrationService
{
    Task AuthenticateAsync(CancellationToken cancellationToken = default);
    Task PushBulkAvailabilityAsync(int hotelId, DateRange dateRange, CancellationToken cancellationToken = default);
    Task SyncRatesToChannelAsync(int hotelId, int channelId, CancellationToken cancellationToken = default);
    Task<IEnumerable<object>> FetchReservationsAsync(int hotelId, CancellationToken cancellationToken = default);
}