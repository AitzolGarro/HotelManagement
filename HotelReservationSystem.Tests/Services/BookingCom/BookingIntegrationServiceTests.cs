using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HotelReservationSystem.Services.BookingCom;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Models.BookingCom;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.Tests.Services.BookingCom;

public class BookingIntegrationServiceTests
{
    private readonly Mock<IBookingComHttpClient> _httpClientMock;
    private readonly Mock<IXmlSerializationService> _xmlSerializerMock;
    private readonly Mock<IBookingComAuthenticationService> _authServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IReservationService> _reservationServiceMock;
    private readonly Mock<IPropertyService> _propertyServiceMock;
    private readonly Mock<ILogger<BookingIntegrationService>> _loggerMock;
    private readonly BookingIntegrationService _service;

    public BookingIntegrationServiceTests()
    {
        _httpClientMock = new Mock<IBookingComHttpClient>();
        _xmlSerializerMock = new Mock<IXmlSerializationService>();
        _authServiceMock = new Mock<IBookingComAuthenticationService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _reservationServiceMock = new Mock<IReservationService>();
        _propertyServiceMock = new Mock<IPropertyService>();
        _loggerMock = new Mock<ILogger<BookingIntegrationService>>();

        _service = new BookingIntegrationService(
            _httpClientMock.Object,
            _xmlSerializerMock.Object,
            _authServiceMock.Object,
            _unitOfWorkMock.Object,
            _reservationServiceMock.Object,
            _propertyServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Assert
        Assert.NotNull(_service);
    }

    [Fact]
    public async Task HandleWebhookAsync_EmptyPayload_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.HandleWebhookAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.HandleWebhookAsync("   "));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.HandleWebhookAsync(null!));
    }
}