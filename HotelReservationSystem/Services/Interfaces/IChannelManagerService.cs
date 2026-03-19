using HotelReservationSystem.Models;

namespace HotelReservationSystem.Services.Interfaces;

public interface IChannelManagerService
{
    Task<HotelChannel> ConnectChannelAsync(int hotelId, int channelId, string channelHotelId, string username, string password);
    Task<IEnumerable<HotelChannel>> GetConnectedChannelsAsync(int hotelId);
    Task<bool> DisconnectChannelAsync(int hotelChannelId);
    
    Task<bool> SyncInventoryToChannelAsync(int hotelChannelId, DateTime startDate, DateTime endDate);
    Task<bool> SyncRatesToChannelAsync(int hotelChannelId, DateTime startDate, DateTime endDate);
    Task<bool> ImportReservationsFromChannelAsync(int hotelChannelId, DateTime? since = null);
}