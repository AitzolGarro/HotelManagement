using Microsoft.Extensions.Diagnostics.HealthChecks;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IRedisConnectionService _redisService;

    public RedisHealthCheck(IRedisConnectionService redisService)
    {
        _redisService = redisService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isConnected = _redisService.IsConnected();

            if (isConnected)
            {
                return Task.FromResult(HealthCheckResult.Healthy("Redis connection is healthy."));
            }

            return Task.FromResult(new HealthCheckResult(
                context.Registration.FailureStatus, 
                "Redis connection is not available."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new HealthCheckResult(
                context.Registration.FailureStatus, 
                "Error connecting to Redis.", 
                ex));
        }
    }
}