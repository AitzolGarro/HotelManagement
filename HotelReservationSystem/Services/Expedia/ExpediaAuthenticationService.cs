using System.Net.Http.Json;
using System.Text.Json;
using HotelReservationSystem.Configuration;
using HotelReservationSystem.Models.Expedia;
using Microsoft.Extensions.Options;

namespace HotelReservationSystem.Services.Expedia;

/// <summary>
/// Manages OAuth2 client_credentials token exchange with the EPS Rapid API.
/// Token is cached in-memory with a 60-second early-expiry buffer to avoid
/// race conditions where the token expires mid-request.
/// </summary>
public class ExpediaAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<ExpediaOptions> _options;
    private readonly ILogger<ExpediaAuthenticationService> _logger;

    private string? _cachedToken;
    private DateTime _tokenExpiresAt = DateTime.MinValue;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public ExpediaAuthenticationService(
        HttpClient httpClient,
        IOptions<ExpediaOptions> options,
        ILogger<ExpediaAuthenticationService> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Returns a valid bearer token, either from cache or freshly exchanged.
    /// Thread-safe: only one refresh happens at a time.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when ApiKey or Secret are not configured.
    /// </exception>
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;

        if (string.IsNullOrWhiteSpace(opts.ApiKey) || string.IsNullOrWhiteSpace(opts.Secret))
        {
            throw new InvalidOperationException("Expedia credentials not configured");
        }

        // Fast path — return cached token if still valid (with 60s buffer)
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiresAt.AddSeconds(-60))
        {
            return _cachedToken;
        }

        // Slow path — one caller refreshes at a time
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_cachedToken != null && DateTime.UtcNow < _tokenExpiresAt.AddSeconds(-60))
            {
                return _cachedToken;
            }

            _logger.LogInformation("Refreshing Expedia OAuth2 bearer token");

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", opts.ApiKey),
                new KeyValuePair<string, string>("client_secret", opts.Secret),
            });

            var response = await _httpClient.PostAsync("/v3/auth/token", formContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<ExpediaAuthResponseDto>(
                cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Empty token response from Expedia API");

            if (string.IsNullOrWhiteSpace(dto.AccessToken))
            {
                throw new InvalidOperationException("Expedia API returned an empty access_token");
            }

            _cachedToken = dto.AccessToken;
            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(dto.ExpiresIn > 0 ? dto.ExpiresIn : 3600);

            _logger.LogInformation("Expedia bearer token refreshed, expires at {ExpiresAt:O}", _tokenExpiresAt);
            return _cachedToken;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to obtain Expedia bearer token");
            throw;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>Invalidates the cached token, forcing the next call to re-exchange.</summary>
    public void InvalidateToken()
    {
        _cachedToken = null;
        _tokenExpiresAt = DateTime.MinValue;
    }
}
