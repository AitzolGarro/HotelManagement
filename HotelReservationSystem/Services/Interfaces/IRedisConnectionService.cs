using StackExchange.Redis;

namespace HotelReservationSystem.Services.Interfaces;

public interface IRedisConnectionService
{
    IConnectionMultiplexer? GetConnection();
    bool IsConnected();
}