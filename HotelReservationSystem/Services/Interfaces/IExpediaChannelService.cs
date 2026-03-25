using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Models.Expedia;

namespace HotelReservationSystem.Services.Interfaces;

public interface IExpediaChannelService
{
    Task<bool> AuthenticateAsync(string username, string password);
    Task<bool> SyncInventoryAsync(int hotelId, DateTime startDate, DateTime endDate);
    Task<bool> SyncRatesAsync(int hotelId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<ReservationDto>> GetReservationsAsync(int hotelId, DateTime? since = null);

    /// <summary>
    /// Processes a validated Expedia webhook envelope (reservation or cancellation event).
    /// Returns true on successful persistence, false otherwise.
    /// </summary>
    Task<bool> HandleWebhookAsync(ExpediaWebhookEnvelopeDto payload);
}