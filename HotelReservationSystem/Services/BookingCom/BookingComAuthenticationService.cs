using HotelReservationSystem.Models.BookingCom;

namespace HotelReservationSystem.Services.BookingCom;

public interface IBookingComAuthenticationService
{
    BookingComAuthentication GetAuthentication();
    bool ValidateCredentials();
    Task<bool> TestAuthenticationAsync(CancellationToken cancellationToken = default);
}

public class BookingComAuthenticationService : IBookingComAuthenticationService
{
    private readonly BookingComConfiguration _configuration;
    private readonly IBookingComHttpClient _httpClient;
    private readonly IXmlSerializationService _xmlSerializer;
    private readonly ILogger<BookingComAuthenticationService> _logger;

    public BookingComAuthenticationService(
        BookingComConfiguration configuration,
        IBookingComHttpClient httpClient,
        IXmlSerializationService xmlSerializer,
        ILogger<BookingComAuthenticationService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _xmlSerializer = xmlSerializer;
        _logger = logger;
    }

    public BookingComAuthentication GetAuthentication()
    {
        if (!ValidateCredentials())
        {
            throw new InvalidOperationException("Booking.com credentials are not properly configured");
        }

        return new BookingComAuthentication
        {
            Username = _configuration.Username,
            Password = _configuration.Password
        };
    }

    public bool ValidateCredentials()
    {
        if (string.IsNullOrWhiteSpace(_configuration.Username))
        {
            _logger.LogError("Booking.com username is not configured");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_configuration.Password))
        {
            _logger.LogError("Booking.com password is not configured");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_configuration.BaseUrl))
        {
            _logger.LogError("Booking.com base URL is not configured");
            return false;
        }

        return true;
    }

    public async Task<bool> TestAuthenticationAsync(CancellationToken cancellationToken = default)
    {
        if (!ValidateCredentials())
        {
            _logger.LogWarning("Cannot test authentication - credentials not properly configured");
            return false;
        }

        try
        {
            _logger.LogInformation("Testing Booking.com authentication");

            // Create a simple authentication test request
            var testRequest = new AuthenticationTestRequest
            {
                Authentication = GetAuthentication()
            };

            var xmlContent = _xmlSerializer.Serialize(testRequest);
            var response = await _httpClient.SendRequestAsync<AuthenticationTestResponse>(
                "auth/test", xmlContent, cancellationToken);

            if (response.Fault != null)
            {
                _logger.LogError("Authentication test failed: {Code} - {Message}", 
                    response.Fault.Code, response.Fault.Message);
                return false;
            }

            _logger.LogInformation("Authentication test successful");
            return true;
        }
        catch (BookingComApiException ex)
        {
            _logger.LogError(ex, "Authentication test failed with API exception: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication test failed with unexpected exception");
            return false;
        }
    }
}

// Authentication test specific models
[System.Xml.Serialization.XmlRoot("request")]
public class AuthenticationTestRequest : BookingComRequest
{
    [System.Xml.Serialization.XmlElement("test")]
    public string Test { get; set; } = "auth";
}

[System.Xml.Serialization.XmlRoot("response")]
public class AuthenticationTestResponse : BookingComResponse
{
    [System.Xml.Serialization.XmlElement("authenticated")]
    public bool Authenticated { get; set; }
    
    [System.Xml.Serialization.XmlElement("user_info")]
    public UserInfo? UserInfo { get; set; }
}

public class UserInfo
{
    [System.Xml.Serialization.XmlElement("username")]
    public string Username { get; set; } = string.Empty;
    
    [System.Xml.Serialization.XmlElement("permissions")]
    public List<string> Permissions { get; set; } = new();
}