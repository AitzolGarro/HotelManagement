using HotelReservationSystem.Services.Expedia;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HotelReservationSystem.HealthChecks;

/// <summary>
/// Reports Expedia API health by attempting to obtain a bearer token.
/// Returns Healthy when the EPS Rapid token endpoint responds successfully.
/// Returns Unhealthy with a descriptive message on auth failure or connectivity issues.
/// </summary>
public class ExpediaHealthCheck : IHealthCheck
{
    private readonly ExpediaAuthenticationService _authService;
    private readonly ILogger<ExpediaHealthCheck> _logger;

    public ExpediaHealthCheck(
        ExpediaAuthenticationService authService,
        ILogger<ExpediaHealthCheck> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await _authService.GetTokenAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(token))
            {
                return HealthCheckResult.Healthy("Expedia EPS Rapid API is reachable and credentials are valid.");
            }

            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "Expedia EPS Rapid API returned an empty token.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not configured"))
        {
            _logger.LogWarning("Expedia health check: credentials not configured");
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "Expedia credentials (ApiKey / Secret) are not configured.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Expedia health check: HTTP request failed");
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "Expedia EPS Rapid API is unreachable.",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Expedia health check: unexpected failure");
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "Expedia health check failed with an unexpected error.",
                ex);
        }
    }
}
