using Xunit;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using HotelReservationSystem.Services.BookingCom;
using HotelReservationSystem.Models.BookingCom;

namespace HotelReservationSystem.Tests.Services.BookingCom;

public class BookingComHttpClientTests : IDisposable
{
    private readonly Mock<IXmlSerializationService> _xmlSerializerMock;
    private readonly Mock<ILogger<BookingComHttpClient>> _loggerMock;
    private readonly BookingComConfiguration _configuration;
    private readonly MockHttpMessageHandler _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly BookingComHttpClient _service;

    public BookingComHttpClientTests()
    {
        _xmlSerializerMock = new Mock<IXmlSerializationService>();
        _loggerMock = new Mock<ILogger<BookingComHttpClient>>();
        _configuration = new BookingComConfiguration
        {
            BaseUrl = "https://test.booking.com/",
            Username = "testuser",
            Password = "testpass",
            TimeoutSeconds = 30,
            MaxRetryAttempts = 3,
            RetryDelaySeconds = 2
        };

        _mockHttpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHttpHandler);
        _service = new BookingComHttpClient(_httpClient, _xmlSerializerMock.Object, _loggerMock.Object, _configuration);
    }

    [Fact]
    public async Task SendRequestAsync_ValidRequest_ReturnsResponseContent()
    {
        // Arrange
        var endpoint = "test/endpoint";
        var requestXml = "<request>test</request>";
        var responseXml = "<response>success</response>";

        _mockHttpHandler
            .When(HttpMethod.Post, $"{_configuration.BaseUrl}{endpoint}")
            .Respond("application/xml", responseXml);

        // Act
        var result = await _service.SendRequestAsync(endpoint, requestXml);

        // Assert
        result.Should().Be(responseXml);
    }

    [Fact]
    public async Task SendRequestAsync_HttpError_ThrowsBookingComApiException()
    {
        // Arrange
        var endpoint = "test/endpoint";
        var requestXml = "<request>test</request>";

        _mockHttpHandler
            .When(HttpMethod.Post, $"{_configuration.BaseUrl}{endpoint}")
            .Respond(HttpStatusCode.BadRequest, "application/xml", "<error>Bad Request</error>");

        // Act & Assert
        var action = async () => await _service.SendRequestAsync(endpoint, requestXml);
        var exception = await action.Should().ThrowAsync<BookingComApiException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendRequestAsync_NetworkError_ThrowsBookingComApiException()
    {
        // Arrange
        var endpoint = "test/endpoint";
        var requestXml = "<request>test</request>";

        _mockHttpHandler
            .When(HttpMethod.Post, $"{_configuration.BaseUrl}{endpoint}")
            .Throw(new HttpRequestException("Network error"));

        // Act & Assert
        var action = async () => await _service.SendRequestAsync(endpoint, requestXml);
        var exception = await action.Should().ThrowAsync<BookingComApiException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task SendRequestAsync_Timeout_ThrowsBookingComApiException()
    {
        // Arrange
        var endpoint = "test/endpoint";
        var requestXml = "<request>test</request>";

        _mockHttpHandler
            .When(HttpMethod.Post, $"{_configuration.BaseUrl}{endpoint}")
            .Throw(new TaskCanceledException("Timeout", new TimeoutException()));

        // Act & Assert
        var action = async () => await _service.SendRequestAsync(endpoint, requestXml);
        var exception = await action.Should().ThrowAsync<BookingComApiException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.RequestTimeout);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendRequestAsync_InvalidEndpoint_ThrowsArgumentException(string endpoint)
    {
        // Arrange
        var requestXml = "<request>test</request>";

        // Act & Assert
        var action = async () => await _service.SendRequestAsync(endpoint, requestXml);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendRequestAsync_InvalidXmlContent_ThrowsArgumentException(string xmlContent)
    {
        // Arrange
        var endpoint = "test/endpoint";

        // Act & Assert
        var action = async () => await _service.SendRequestAsync(endpoint, xmlContent);
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SendRequestAsync_Generic_SuccessfulDeserialization_ReturnsTypedObject()
    {
        // Arrange
        var endpoint = "test/endpoint";
        var requestXml = "<request>test</request>";
        var responseXml = "<response><ok /></response>";
        var expectedResponse = new BookingComResponse { Ok = "" };

        _mockHttpHandler
            .When(HttpMethod.Post, $"{_configuration.BaseUrl}{endpoint}")
            .Respond("application/xml", responseXml);

        _xmlSerializerMock
            .Setup(x => x.Deserialize<BookingComResponse>(responseXml))
            .Returns(expectedResponse);

        // Act
        var result = await _service.SendRequestAsync<BookingComResponse>(endpoint, requestXml);

        // Assert
        result.Should().Be(expectedResponse);
        _xmlSerializerMock.Verify(x => x.Deserialize<BookingComResponse>(responseXml), Times.Once);
    }

    [Fact]
    public async Task SendRequestAsync_Generic_ResponseWithFault_ThrowsBookingComApiException()
    {
        // Arrange
        var endpoint = "test/endpoint";
        var requestXml = "<request>test</request>";
        var responseXml = "<response><fault code=\"AUTH_ERROR\">Authentication failed</fault></response>";
        var faultResponse = new BookingComResponse 
        { 
            Fault = new FaultObject 
            { 
                Code = "AUTH_ERROR", 
                Message = "Authentication failed" 
            } 
        };

        _mockHttpHandler
            .When(HttpMethod.Post, $"{_configuration.BaseUrl}{endpoint}")
            .Respond("application/xml", responseXml);

        _xmlSerializerMock
            .Setup(x => x.Deserialize<BookingComResponse>(responseXml))
            .Returns(faultResponse);

        // Act & Assert
        var action = async () => await _service.SendRequestAsync<BookingComResponse>(endpoint, requestXml);
        var exception = await action.Should().ThrowAsync<BookingComApiException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.ErrorCode.Should().Be("AUTH_ERROR");
    }

    [Fact]
    public async Task SendRequestAsync_Generic_DeserializationFails_ThrowsBookingComApiException()
    {
        // Arrange
        var endpoint = "test/endpoint";
        var requestXml = "<request>test</request>";
        var responseXml = "<response>invalid</response>";

        _mockHttpHandler
            .When(HttpMethod.Post, $"{_configuration.BaseUrl}{endpoint}")
            .Respond("application/xml", responseXml);

        _xmlSerializerMock
            .Setup(x => x.Deserialize<BookingComResponse>(responseXml))
            .Throws(new InvalidOperationException("Deserialization failed"));

        // Act & Assert
        var action = async () => await _service.SendRequestAsync<BookingComResponse>(endpoint, requestXml);
        var exception = await action.Should().ThrowAsync<BookingComApiException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task TestConnectionAsync_SuccessfulConnection_ReturnsTrue()
    {
        // Arrange
        var testXml = "<request><authentication><username>testuser</username><password>testpass</password></authentication></request>";
        var responseXml = "<response><ok /></response>";

        _xmlSerializerMock
            .Setup(x => x.Serialize(It.IsAny<BookingComRequest>()))
            .Returns(testXml);

        _mockHttpHandler
            .When(HttpMethod.Post, $"{_configuration.BaseUrl}test")
            .Respond("application/xml", responseXml);

        // Act
        var result = await _service.TestConnectionAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_ConnectionFails_ReturnsFalse()
    {
        // Arrange
        var testXml = "<request><authentication><username>testuser</username><password>testpass</password></authentication></request>";

        _xmlSerializerMock
            .Setup(x => x.Serialize(It.IsAny<BookingComRequest>()))
            .Returns(testXml);

        _mockHttpHandler
            .When(HttpMethod.Post, $"{_configuration.BaseUrl}test")
            .Respond(HttpStatusCode.Unauthorized);

        // Act
        var result = await _service.TestConnectionAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendRequestAsync_CancellationRequested_ThrowsTaskCanceledException()
    {
        // Arrange
        var endpoint = "test/endpoint";
        var requestXml = "<request>test</request>";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var action = async () => await _service.SendRequestAsync(endpoint, requestXml, cts.Token);
        await action.Should().ThrowAsync<TaskCanceledException>();
    }

    public void Dispose()
    {
        _mockHttpHandler?.Dispose();
        _httpClient?.Dispose();
    }
}
