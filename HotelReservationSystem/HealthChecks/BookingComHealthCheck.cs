using Microsoft.Extensions.Diagnostics.HealthChecks;
using HotelReservationSystem.Services.BookingCom;

namespace HotelReservationSystem.HealthChecks;

public class BookingComHealthCheck : IHealthCheck
{
    private readonly IBookingComHttpClient _httpClient;
    private readonly ILogger<BookingComHealthCheck> _logger;

    public BookingComHealthCheck(IBookingComHttpClient httpClient, ILogger<BookingComHealthCheck> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple ping to check if the Booking.com API is reachable
            // We use a light endpoint if available, or just check connectivity
            var isReachable = await _httpClient.TestConnectionAsync(cancellationToken);

            if (isReachable)
            {
                return HealthCheckResult.Healthy("Booking.com API is reachable.");
            }

            return new HealthCheckResult(
                context.Registration.FailureStatus, 
                "Booking.com API is not reachable.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for Booking.com API");
            return new HealthCheckResult(
                context.Registration.FailureStatus, 
                "Error connecting to Booking.com API.", 
                ex);
        }
    }
}