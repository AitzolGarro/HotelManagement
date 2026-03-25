using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace HotelReservationSystem.Services.Expedia;

/// <summary>
/// Typed HttpClient for the EPS Rapid API.
/// Injects a valid OAuth2 bearer token into every outgoing request.
/// On 401 responses, refreshes the token and retries once.
/// Polly retry + circuit-breaker policies are wired in Program.cs (AddHttpClient).
/// </summary>
public class ExpediaHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ExpediaAuthenticationService _authService;
    private readonly ILogger<ExpediaHttpClient> _logger;

    public ExpediaHttpClient(
        HttpClient httpClient,
        ExpediaAuthenticationService authService,
        ILogger<ExpediaHttpClient> logger)
    {
        _httpClient = httpClient;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Sends an authenticated GET request to the specified path.
    /// Retries once with a refreshed token on HTTP 401.
    /// </summary>
    public async Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        var request = await BuildAuthenticatedRequestAsync(HttpMethod.Get, requestUri, null, cancellationToken);
        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Expedia API returned 401 — refreshing token and retrying");
            _authService.InvalidateToken();
            request = await BuildAuthenticatedRequestAsync(HttpMethod.Get, requestUri, null, cancellationToken);
            response = await _httpClient.SendAsync(request, cancellationToken);
        }

        return response;
    }

    /// <summary>
    /// Sends an authenticated PUT request with a JSON body.
    /// Retries once with a refreshed token on HTTP 401.
    /// </summary>
    public async Task<HttpResponseMessage> PutAsJsonAsync<T>(string requestUri, T payload, CancellationToken cancellationToken = default)
    {
        var content = JsonContent.Create(payload);
        var request = await BuildAuthenticatedRequestAsync(HttpMethod.Put, requestUri, content, cancellationToken);
        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Expedia API returned 401 — refreshing token and retrying");
            _authService.InvalidateToken();
            content = JsonContent.Create(payload);
            request = await BuildAuthenticatedRequestAsync(HttpMethod.Put, requestUri, content, cancellationToken);
            response = await _httpClient.SendAsync(request, cancellationToken);
        }

        return response;
    }

    /// <summary>
    /// Sends an authenticated POST request with a JSON body.
    /// Retries once with a refreshed token on HTTP 401.
    /// </summary>
    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T payload, CancellationToken cancellationToken = default)
    {
        var content = JsonContent.Create(payload);
        var request = await BuildAuthenticatedRequestAsync(HttpMethod.Post, requestUri, content, cancellationToken);
        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Expedia API returned 401 — refreshing token and retrying");
            _authService.InvalidateToken();
            content = JsonContent.Create(payload);
            request = await BuildAuthenticatedRequestAsync(HttpMethod.Post, requestUri, content, cancellationToken);
            response = await _httpClient.SendAsync(request, cancellationToken);
        }

        return response;
    }

    private async Task<HttpRequestMessage> BuildAuthenticatedRequestAsync(
        HttpMethod method,
        string requestUri,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        var token = await _authService.GetTokenAsync(cancellationToken);

        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (content != null)
        {
            request.Content = content;
        }

        return request;
    }
}
