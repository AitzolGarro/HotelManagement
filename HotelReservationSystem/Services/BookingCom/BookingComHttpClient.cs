using System.Net;
using System.Text;
using Polly;
using Polly.Extensions.Http;
using HotelReservationSystem.Models.BookingCom;

namespace HotelReservationSystem.Services.BookingCom;

public interface IBookingComHttpClient
{
    Task<string> SendRequestAsync(string endpoint, string xmlContent, CancellationToken cancellationToken = default);
    Task<T> SendRequestAsync<T>(string endpoint, string xmlContent, CancellationToken cancellationToken = default) where T : class;
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

public class BookingComHttpClient : IBookingComHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly IXmlSerializationService _xmlSerializer;
    private readonly ILogger<BookingComHttpClient> _logger;
    private readonly BookingComConfiguration _configuration;

    public BookingComHttpClient(
        HttpClient httpClient,
        IXmlSerializationService xmlSerializer,
        ILogger<BookingComHttpClient> logger,
        BookingComConfiguration configuration)
    {
        _httpClient = httpClient;
        _xmlSerializer = xmlSerializer;
        _logger = logger;
        _configuration = configuration;

        ConfigureHttpClient();
    }

    public async Task<string> SendRequestAsync(string endpoint, string xmlContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));
        
        if (string.IsNullOrWhiteSpace(xmlContent))
            throw new ArgumentException("XML content cannot be null or empty", nameof(xmlContent));

        try
        {
            _logger.LogDebug("Sending request to Booking.com endpoint: {Endpoint}", endpoint);
            
            var content = new StringContent(xmlContent, Encoding.UTF8, "application/xml");
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Booking.com API request failed. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, responseContent);
                
                throw new BookingComApiException(
                    $"API request failed with status {response.StatusCode}",
                    response.StatusCode,
                    responseContent);
            }

            _logger.LogDebug("Successfully received response from Booking.com endpoint: {Endpoint}", endpoint);
            return responseContent;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for endpoint: {Endpoint}", endpoint);
            throw new BookingComApiException("HTTP request failed", HttpStatusCode.ServiceUnavailable, ex.Message, ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Request timeout for endpoint: {Endpoint}", endpoint);
            throw new BookingComApiException("Request timeout", HttpStatusCode.RequestTimeout, "The request timed out", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request was cancelled for endpoint: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T> SendRequestAsync<T>(string endpoint, string xmlContent, CancellationToken cancellationToken = default) where T : class
    {
        var responseXml = await SendRequestAsync(endpoint, xmlContent, cancellationToken);
        
        try
        {
            var response = _xmlSerializer.Deserialize<T>(responseXml);
            
            // Check for API-level errors in the response
            if (response is BookingComResponse baseResponse && baseResponse.Fault != null)
            {
                _logger.LogError("Booking.com API returned fault: {Code} - {Message}", 
                    baseResponse.Fault.Code, baseResponse.Fault.Message);
                
                throw new BookingComApiException(
                    $"API fault: {baseResponse.Fault.Message}",
                    HttpStatusCode.BadRequest,
                    baseResponse.Fault.Code,
                    null);
            }
            
            return response;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to deserialize response from endpoint: {Endpoint}", endpoint);
            throw new BookingComApiException("Failed to parse API response", HttpStatusCode.BadGateway, responseXml, ex);
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Testing connection to Booking.com API");
            
            // Create a simple test request
            var testRequest = new BookingComRequest
            {
                Authentication = new BookingComAuthentication
                {
                    Username = _configuration.Username,
                    Password = _configuration.Password
                }
            };

            var xmlContent = _xmlSerializer.Serialize(testRequest);
            var response = await SendRequestAsync("test", xmlContent, cancellationToken);
            
            _logger.LogInformation("Connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection test failed");
            return false;
        }
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_configuration.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds);
        
        // Add default headers
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "HotelReservationSystem/1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/xml");
    }
}

public class BookingComConfiguration
{
    public string BaseUrl { get; set; } = "https://secure-supply-xml.booking.com/";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 2;
    public string WebhookSecret { get; set; } = string.Empty;
    public int BulkPushDelayMs { get; set; } = 200;
}

public class BookingComApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? ResponseContent { get; }
    public string? ErrorCode { get; }

    public BookingComApiException(string message) : base(message)
    {
        StatusCode = HttpStatusCode.InternalServerError;
    }

    public BookingComApiException(string message, HttpStatusCode statusCode, string? responseContent = null) : base(message)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }

    public BookingComApiException(string message, HttpStatusCode statusCode, string? errorCode, Exception? innerException = null) : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
