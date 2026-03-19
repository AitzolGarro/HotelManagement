using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces;

public interface IExpediaChannelService
{
    Task<bool> AuthenticateAsync(string username, string password);
    Task<bool> SyncInventoryAsync(int hotelId, DateTime startDate, DateTime endDate);
    Task<bool> SyncRatesAsync(int hotelId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<ReservationDto>> GetReservationsAsync(int hotelId, DateTime? since = null);
}