using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using HotelReservationSystem.Services.BookingCom;
using HotelReservationSystem.Models.BookingCom;
using ServiceAuthRequest = HotelReservationSystem.Services.BookingCom.AuthenticationTestRequest;
using ServiceAuthResponse = HotelReservationSystem.Services.BookingCom.AuthenticationTestResponse;

namespace HotelReservationSystem.Tests.Services.BookingCom;

public class BookingComAuthenticationServiceTests
{
    private readonly Mock<IBookingComHttpClient> _httpClientMock;
    private readonly Mock<IXmlSerializationService> _xmlSerializerMock;
    private readonly Mock<ILogger<BookingComAuthenticationService>> _loggerMock;
    private readonly BookingComConfiguration _validConfiguration;
    private readonly BookingComAuthenticationService _service;

    public BookingComAuthenticationServiceTests()
    {
        _httpClientMock = new Mock<IBookingComHttpClient>();
        _xmlSerializerMock = new Mock<IXmlSerializationService>();
        _loggerMock = new Mock<ILogger<BookingComAuthenticationService>>();
        
        _validConfiguration = new BookingComConfiguration
        {
            BaseUrl = "https://test.booking.com/",
            Username = "testuser",
            Password = "testpass",
            TimeoutSeconds = 30
        };

        _service = new BookingComAuthenticationService(
            _validConfiguration,
            _httpClientMock.Object,
            _xmlSerializerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void GetAuthentication_ValidConfiguration_ReturnsAuthenticationObject()
    {
        // Act
        var result = _service.GetAuthentication();

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("testuser");
        result.Password.Should().Be("testpass");
    }

    [Fact]
    public void GetAuthentication_InvalidConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidConfig = new BookingComConfiguration
        {
            BaseUrl = "https://test.booking.com/",
            Username = "", // Invalid
            Password = "testpass"
        };

        var service = new BookingComAuthenticationService(
            invalidConfig,
            _httpClientMock.Object,
            _xmlSerializerMock.Object,
            _loggerMock.Object);

        // Act & Assert
        var action = () => service.GetAuthentication();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Booking.com credentials are not properly configured");
    }

    [Fact]
    public void ValidateCredentials_ValidConfiguration_ReturnsTrue()
    {
        // Act
        var result = _service.ValidateCredentials();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "testpass", "https://test.booking.com/")]
    [InlineData("testuser", "", "https://test.booking.com/")]
    [InlineData("testuser", "testpass", "")]
    [InlineData(null, "testpass", "https://test.booking.com/")]
    [InlineData("testuser", null, "https://test.booking.com/")]
    [InlineData("testuser", "testpass", null)]
    public void ValidateCredentials_InvalidConfiguration_ReturnsFalse(string username, string password, string baseUrl)
    {
        // Arrange
        var invalidConfig = new BookingComConfiguration
        {
            BaseUrl = baseUrl,
            Username = username,
            Password = password
        };

        var service = new BookingComAuthenticationService(
            invalidConfig,
            _httpClientMock.Object,
            _xmlSerializerMock.Object,
            _loggerMock.Object);

        // Act
        var result = service.ValidateCredentials();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestAuthenticationAsync_ValidCredentials_ReturnsTrue()
    {
        // Arrange
        var testXml = "<request><authentication><username>testuser</username><password>testpass</password></authentication><test>auth</test></request>";
        var successResponse = new ServiceAuthResponse
        {
            Authenticated = true,
            UserInfo = new UserInfo
            {
                Username = "testuser",
                Permissions = new List<string> { "read", "write" }
            }
        };

        _xmlSerializerMock
            .Setup(x => x.Serialize(It.IsAny<ServiceAuthRequest>()))
            .Returns(testXml);

        _httpClientMock
            .Setup(x => x.SendRequestAsync<ServiceAuthResponse>("auth/test", testXml, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResponse);

        // Act
        var result = await _service.TestAuthenticationAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TestAuthenticationAsync_InvalidCredentials_ReturnsFalse()
    {
        // Arrange
        var testXml = "<request><authentication><username>testuser</username><password>testpass</password></authentication><test>auth</test></request>";
        var faultResponse = new ServiceAuthResponse
        {
            Fault = new FaultObject
            {
                Code = "AUTH_ERROR",
                Message = "Invalid credentials"
            }
        };

        _xmlSerializerMock
            .Setup(x => x.Serialize(It.IsAny<ServiceAuthRequest>()))
            .Returns(testXml);

        _httpClientMock
            .Setup(x => x.SendRequestAsync<ServiceAuthResponse>("auth/test", testXml, It.IsAny<CancellationToken>()))
            .ReturnsAsync(faultResponse);

        // Act
        var result = await _service.TestAuthenticationAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestAuthenticationAsync_HttpException_ReturnsFalse()
    {
        // Arrange
        var testXml = "<request><authentication><username>testuser</username><password>testpass</password></authentication><test>auth</test></request>";

        _xmlSerializerMock
            .Setup(x => x.Serialize(It.IsAny<ServiceAuthRequest>()))
            .Returns(testXml);

        _httpClientMock
            .Setup(x => x.SendRequestAsync<ServiceAuthResponse>("auth/test", testXml, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BookingComApiException("Network error"));

        // Act
        var result = await _service.TestAuthenticationAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestAuthenticationAsync_InvalidConfiguration_ReturnsFalse()
    {
        // Arrange
        var invalidConfig = new BookingComConfiguration
        {
            BaseUrl = "https://test.booking.com/",
            Username = "", // Invalid
            Password = "testpass"
        };

        var service = new BookingComAuthenticationService(
            invalidConfig,
            _httpClientMock.Object,
            _xmlSerializerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await service.TestAuthenticationAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestAuthenticationAsync_UnexpectedException_ReturnsFalse()
    {
        // Arrange
        var testXml = "<request><authentication><username>testuser</username><password>testpass</password></authentication><test>auth</test></request>";

        _xmlSerializerMock
            .Setup(x => x.Serialize(It.IsAny<ServiceAuthRequest>()))
            .Returns(testXml);

        _httpClientMock
            .Setup(x => x.SendRequestAsync<ServiceAuthResponse>("auth/test", testXml, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act
        var result = await _service.TestAuthenticationAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestAuthenticationAsync_CancellationRequested_ThrowsTaskCanceledException()
    {
        // Arrange
        var testXml = "<request><authentication><username>testuser</username><password>testpass</password></authentication><test>auth</test></request>";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _xmlSerializerMock
            .Setup(x => x.Serialize(It.IsAny<ServiceAuthRequest>()))
            .Returns(testXml);

        _httpClientMock
            .Setup(x => x.SendRequestAsync<ServiceAuthResponse>("auth/test", testXml, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException());

        // Act & Assert
        var action = async () => await _service.TestAuthenticationAsync(cts.Token);
        await action.Should().ThrowAsync<TaskCanceledException>();
    }
}
