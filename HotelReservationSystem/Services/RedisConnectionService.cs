using StackExchange.Redis;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services;

public class RedisConnectionService : IRedisConnectionService
{
    private readonly Lazy<IConnectionMultiplexer>? _lazyConnection;
    private readonly ILogger<RedisConnectionService> _logger;

    public RedisConnectionService(IConfiguration configuration, ILogger<RedisConnectionService> logger)
    {
        _logger = logger;
        
        var redisConnectionString = configuration.GetConnectionString("Redis");
        var isDemoMode = configuration.GetValue<bool>("UseSqlite");

        if (!isDemoMode && !string.IsNullOrEmpty(redisConnectionString))
        {
            try
            {
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.AbortOnConnectFail = false;
                
                _lazyConnection = new Lazy<IConnectionMultiplexer>(() => 
                {
                    var multiplexer = ConnectionMultiplexer.Connect(options);
                    
                    multiplexer.ConnectionRestored += (sender, args) => 
                        _logger.LogInformation("Redis connection restored");
                        
                    multiplexer.ConnectionFailed += (sender, args) => 
                        _logger.LogError("Redis connection failed: {Error}", args.Exception?.Message);
                        
                    return multiplexer;
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Redis connection configuration");
            }
        }
    }

    public IConnectionMultiplexer? GetConnection()
    {
        if (_lazyConnection == null)
            return null;

        try
        {
            return _lazyConnection.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Redis connection");
            return null;
        }
    }

    public bool IsConnected()
    {
        var connection = GetConnection();
        return connection?.IsConnected ?? false;
    }
}