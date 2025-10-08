using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;
using HotelReservationSystem.Controllers;
using HotelReservationSystem.Services.BookingCom;

namespace HotelReservationSystem.Tests.Controllers;

public class BookingComWebhookControllerTests
{
    private readonly Mock<IBookingIntegrationService> _bookingIntegrationServiceMock;
    private readonly Mock<ILogger<BookingComWebhookController>> _loggerMock;
    private readonly BookingComWebhookController _controller;

    public BookingComWebhookControllerTests()
    {
        _bookingIntegrationServiceMock = new Mock<IBookingIntegrationService>();
        _loggerMock = new Mock<ILogger<BookingComWebhookController>>();
        _controller = new BookingComWebhookController(_bookingIntegrationServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleWebhook_ValidPayload_ShouldReturnOk()
    {
        // Arrange
        var xmlPayload = @"<?xml version=""1.0"" encoding=""utf-8""?>
            <notification type=""reservation_created"" timestamp=""2024-11-01T10:00:00Z"">
                <reservation id=""BK123456"" status=""confirmed"">
                    <hotel_id>1</hotel_id>
                    <room_id>1</room_id>
                    <checkin>2024-12-01</checkin>
                    <checkout>2024-12-03</checkout>
                    <guest_name>John Doe</guest_name>
                    <guest_email>john.doe@email.com</guest_email>
                    <number_of_guests>2</number_of_guests>
                    <total_amount>200.00</total_amount>
                </reservation>
            </notification>";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlPayload));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.ContentType = "application/xml";
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _bookingIntegrationServiceMock.Setup(x => x.HandleWebhookAsync(xmlPayload, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.HandleWebhook();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as dynamic;
        Assert.NotNull(response);
        Assert.Equal("Webhook processed successfully", response!.message);

        _bookingIntegrationServiceMock.Verify(x => x.HandleWebhookAsync(xmlPayload, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_EmptyPayload_ShouldReturnBadRequest()
    {
        // Arrange
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.ContentType = "application/xml";
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.HandleWebhook();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Empty payload", badRequestResult.Value);

        _bookingIntegrationServiceMock.Verify(x => x.HandleWebhookAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhook_InvalidPayload_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidXml = "invalid xml content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidXml));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.ContentType = "application/xml";
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _bookingIntegrationServiceMock.Setup(x => x.HandleWebhookAsync(invalidXml, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid XML format"));

        // Act
        var result = await _controller.HandleWebhook();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = badRequestResult.Value as dynamic;
        Assert.NotNull(response);
        Assert.Equal("Invalid payload", response!.error);
        Assert.Equal("Invalid XML format", response.details);
    }

    [Fact]
    public async Task HandleWebhook_ServiceException_ShouldReturnInternalServerError()
    {
        // Arrange
        var xmlPayload = @"<?xml version=""1.0"" encoding=""utf-8""?>
            <notification type=""reservation_created"" timestamp=""2024-11-01T10:00:00Z"">
                <reservation id=""BK123456"" status=""confirmed"">
                    <hotel_id>1</hotel_id>
                </reservation>
            </notification>";

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlPayload));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.ContentType = "application/xml";
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _bookingIntegrationServiceMock.Setup(x => x.HandleWebhookAsync(xmlPayload, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await _controller.HandleWebhook();

        // Assert
        var serverErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, serverErrorResult.StatusCode);
        
        var response = serverErrorResult.Value as dynamic;
        Assert.NotNull(response);
        Assert.Equal("Internal server error", response!.error);
        Assert.Equal("Failed to process webhook", response.details);
    }

    [Fact]
    public void HealthCheck_ShouldReturnHealthyStatus()
    {
        // Act
        var result = _controller.HealthCheck();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as dynamic;
        Assert.NotNull(response);
        Assert.Equal("healthy", response!.status);
        Assert.Equal("booking-com-webhook", response.service);
        Assert.NotNull(response.timestamp);
    }

    [Fact]
    public void ValidateWebhook_WithChallenge_ShouldReturnChallenge()
    {
        // Arrange
        var challenge = "test-challenge-123";

        // Act
        var result = _controller.ValidateWebhook(challenge);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as dynamic;
        Assert.NotNull(response);
        Assert.Equal(challenge, response!.challenge);
    }

    [Fact]
    public void ValidateWebhook_WithoutChallenge_ShouldReturnBadRequest()
    {
        // Act
        var result = _controller.ValidateWebhook(null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = badRequestResult.Value as dynamic;
        Assert.NotNull(response);
        Assert.Equal("Challenge parameter is required", response!.error);
    }

    [Fact]
    public void ValidateWebhook_WithEmptyChallenge_ShouldReturnBadRequest()
    {
        // Act
        var result = _controller.ValidateWebhook("");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = badRequestResult.Value as dynamic;
        Assert.NotNull(response);
        Assert.Equal("Challenge parameter is required", response!.error);
    }
}