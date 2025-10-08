using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HotelReservationSystem.Services.BookingCom;
using HotelReservationSystem.Models.BookingCom;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Tests.Integration;

public class BookingComSyncIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<IBookingComHttpClient> _httpClientMock;
    private readonly Mock<IXmlSerializationService> _xmlSerializerMock;
    private readonly Mock<IBookingComAuthenticationService> _authServiceMock;

    public BookingComSyncIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Mock external dependencies
        _httpClientMock = new Mock<IBookingComHttpClient>();
        _xmlSerializerMock = new Mock<IXmlSerializationService>();
        _authServiceMock = new Mock<IBookingComAuthenticationService>();
        
        // Mock internal dependencies
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var reservationServiceMock = new Mock<IReservationService>();
        var propertyServiceMock = new Mock<IPropertyService>();
        
        // Register mocks
        services.AddSingleton(_httpClientMock.Object);
        services.AddSingleton(_xmlSerializerMock.Object);
        services.AddSingleton(_authServiceMock.Object);
        services.AddSingleton(unitOfWorkMock.Object);
        services.AddSingleton(reservationServiceMock.Object);
        services.AddSingleton(propertyServiceMock.Object);
        
        // Register the service under test
        services.AddScoped<IBookingIntegrationService, BookingIntegrationService>();
        
        _serviceProvider = services.BuildServiceProvider();
        
        SetupMockDefaults();
    }

    private void SetupMockDefaults()
    {
        _authServiceMock.Setup(x => x.GetAuthentication())
            .Returns(new BookingComAuthentication { Username = "test", Password = "test" });
        
        _xmlSerializerMock.Setup(x => x.Serialize(It.IsAny<object>()))
            .Returns("<xml>test</xml>");
    }

    [Fact]
    public async Task SyncReservationsForHotelAsync_ShouldProcessReservationsSuccessfully()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<IBookingIntegrationService>();
        var hotelId = 1;
        var fromDate = DateTime.Today.AddDays(-30);
        var toDate = DateTime.Today.AddDays(30);

        var mockResponse = new ReservationSyncResponse
        {
            Reservations = new List<BookingComReservation>
            {
                new BookingComReservation
                {
                    Id = "BK123456",
                    Status = "confirmed",
                    HotelId = hotelId,
                    RoomId = 1,
                    CheckIn = "2024-12-01",
                    CheckOut = "2024-12-03",
                    GuestName = "John Doe",
                    GuestEmail = "john.doe@email.com",
                    GuestPhone = "+1234567890",
                    NumberOfGuests = 2,
                    TotalAmount = 200.00m,
                    Currency = "USD",
                    SpecialRequests = "Late check-in",
                    CreatedAt = "2024-11-01T10:00:00Z",
                    UpdatedAt = "2024-11-01T10:00:00Z"
                }
            }
        };

        _httpClientMock.Setup(x => x.SendRequestAsync<ReservationSyncResponse>(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        var propertyService = _serviceProvider.GetRequiredService<IPropertyService>();
        Mock.Get(propertyService).Setup(x => x.ValidateHotelExistsAsync(hotelId))
            .Returns(Task.CompletedTask);

        var reservationService = _serviceProvider.GetRequiredService<IReservationService>();
        Mock.Get(reservationService).Setup(x => x.GetReservationByBookingReferenceAsync("BK123456"))
            .ReturnsAsync((ReservationDto?)null);

        var unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();
        var guestRepoMock = new Mock<IGuestRepository>();
        guestRepoMock.Setup(x => x.GetGuestByEmailAsync("john.doe@email.com"))
            .ReturnsAsync((Guest?)null);
        guestRepoMock.Setup(x => x.AddAsync(It.IsAny<Guest>()))
            .Returns(Task.CompletedTask);
        Mock.Get(unitOfWork).Setup(x => x.Guests).Returns(guestRepoMock.Object);
        Mock.Get(unitOfWork).Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        Mock.Get(propertyService).Setup(x => x.ValidateRoomExistsAsync(1))
            .Returns(Task.CompletedTask);

        Mock.Get(reservationService).Setup(x => x.CheckAvailabilityAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(true);

        Mock.Get(reservationService).Setup(x => x.CreateReservationAsync(It.IsAny<CreateReservationRequest>()))
            .ReturnsAsync(new ReservationDto { Id = 1, BookingReference = "BK123456" });

        // Act
        await service.SyncReservationsForHotelAsync(hotelId, fromDate, toDate);

        // Assert
        _httpClientMock.Verify(x => x.SendRequestAsync<ReservationSyncResponse>(
            "reservations/sync", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        
        Mock.Get(reservationService).Verify(x => x.CreateReservationAsync(
            It.Is<CreateReservationRequest>(r => 
                r.BookingReference == "BK123456" && 
                r.Source == ReservationSource.Booking)), Times.Once);
    }

    [Fact]
    public async Task HandleWebhookAsync_ReservationCreated_ShouldProcessSuccessfully()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<IBookingIntegrationService>();
        var webhookXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
            <notification type=""reservation_created"" timestamp=""2024-11-01T10:00:00Z"">
                <reservation id=""BK789012"" status=""confirmed"">
                    <hotel_id>1</hotel_id>
                    <room_id>2</room_id>
                    <checkin>2024-12-05</checkin>
                    <checkout>2024-12-07</checkout>
                    <guest_name>Jane Smith</guest_name>
                    <guest_email>jane.smith@email.com</guest_email>
                    <guest_phone>+1987654321</guest_phone>
                    <number_of_guests>1</number_of_guests>
                    <total_amount>150.00</total_amount>
                    <currency>USD</currency>
                    <special_requests>Early check-in</special_requests>
                    <created_at>2024-11-01T10:00:00Z</created_at>
                    <updated_at>2024-11-01T10:00:00Z</updated_at>
                </reservation>
            </notification>";

        var mockNotification = new BookingComWebhookNotification
        {
            Type = "reservation_created",
            Timestamp = "2024-11-01T10:00:00Z",
            Reservation = new BookingComReservation
            {
                Id = "BK789012",
                Status = "confirmed",
                HotelId = 1,
                RoomId = 2,
                CheckIn = "2024-12-05",
                CheckOut = "2024-12-07",
                GuestName = "Jane Smith",
                GuestEmail = "jane.smith@email.com",
                GuestPhone = "+1987654321",
                NumberOfGuests = 1,
                TotalAmount = 150.00m,
                Currency = "USD",
                SpecialRequests = "Early check-in"
            }
        };

        _xmlSerializerMock.Setup(x => x.Deserialize<BookingComWebhookNotification>(webhookXml))
            .Returns(mockNotification);

        var reservationService = _serviceProvider.GetRequiredService<IReservationService>();
        Mock.Get(reservationService).Setup(x => x.GetReservationByBookingReferenceAsync("BK789012"))
            .ReturnsAsync((ReservationDto?)null);

        var unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();
        var guestRepoMock = new Mock<IGuestRepository>();
        guestRepoMock.Setup(x => x.GetGuestByEmailAsync("jane.smith@email.com"))
            .ReturnsAsync((Guest?)null);
        guestRepoMock.Setup(x => x.AddAsync(It.IsAny<Guest>()))
            .Returns(Task.CompletedTask);
        Mock.Get(unitOfWork).Setup(x => x.Guests).Returns(guestRepoMock.Object);
        Mock.Get(unitOfWork).Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        var propertyService = _serviceProvider.GetRequiredService<IPropertyService>();
        Mock.Get(propertyService).Setup(x => x.ValidateRoomExistsAsync(2))
            .Returns(Task.CompletedTask);

        Mock.Get(reservationService).Setup(x => x.CheckAvailabilityAsync(2, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(true);

        Mock.Get(reservationService).Setup(x => x.CreateReservationAsync(It.IsAny<CreateReservationRequest>()))
            .ReturnsAsync(new ReservationDto { Id = 2, BookingReference = "BK789012" });

        // Act
        await service.HandleWebhookAsync(webhookXml);

        // Assert
        _xmlSerializerMock.Verify(x => x.Deserialize<BookingComWebhookNotification>(webhookXml), Times.Once);
        Mock.Get(reservationService).Verify(x => x.CreateReservationAsync(
            It.Is<CreateReservationRequest>(r => 
                r.BookingReference == "BK789012" && 
                r.Source == ReservationSource.Booking)), Times.Once);
    }

    [Fact]
    public async Task HandleWebhookAsync_ReservationCancelled_ShouldCancelReservation()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<IBookingIntegrationService>();
        var webhookXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
            <notification type=""reservation_cancelled"" timestamp=""2024-11-01T12:00:00Z"">
                <cancellation reservation_id=""BK123456"">
                    <reason>Guest cancelled</reason>
                    <cancelled_at>2024-11-01T12:00:00Z</cancelled_at>
                </cancellation>
            </notification>";

        var mockNotification = new BookingComWebhookNotification
        {
            Type = "reservation_cancelled",
            Timestamp = "2024-11-01T12:00:00Z",
            Cancellation = new BookingComCancellation
            {
                ReservationId = "BK123456",
                Reason = "Guest cancelled",
                CancelledAt = "2024-11-01T12:00:00Z"
            }
        };

        _xmlSerializerMock.Setup(x => x.Deserialize<BookingComWebhookNotification>(webhookXml))
            .Returns(mockNotification);

        var existingReservation = new ReservationDto
        {
            Id = 1,
            BookingReference = "BK123456",
            Status = ReservationStatus.Confirmed
        };

        var reservationService = _serviceProvider.GetRequiredService<IReservationService>();
        Mock.Get(reservationService).Setup(x => x.GetReservationByBookingReferenceAsync("BK123456"))
            .ReturnsAsync(existingReservation);

        Mock.Get(reservationService).Setup(x => x.CancelReservationAsync(1, It.IsAny<CancelReservationRequest>()))
            .ReturnsAsync(true);

        // Act
        await service.HandleWebhookAsync(webhookXml);

        // Assert
        _xmlSerializerMock.Verify(x => x.Deserialize<BookingComWebhookNotification>(webhookXml), Times.Once);
        Mock.Get(reservationService).Verify(x => x.CancelReservationAsync(1, 
            It.Is<CancelReservationRequest>(r => r.Reason.Contains("Guest cancelled"))), Times.Once);
    }

    [Fact]
    public async Task PushAvailabilityUpdateAsync_ShouldSendUpdateToBookingCom()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<IBookingIntegrationService>();
        var roomId = 1;
        var date = DateTime.Today.AddDays(1);
        var availableCount = 1;

        var mockRoom = new Room
        {
            Id = roomId,
            HotelId = 1,
            RoomNumber = "101",
            BaseRate = 100.00m
        };

        var unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();
        var roomRepoMock = new Mock<IRoomRepository>();
        roomRepoMock.Setup(x => x.GetByIdAsync(roomId))
            .ReturnsAsync(mockRoom);
        Mock.Get(unitOfWork).Setup(x => x.Rooms).Returns(roomRepoMock.Object);

        var mockResponse = new BookingComResponse { Ok = "success" };
        _httpClientMock.Setup(x => x.SendRequestAsync<BookingComResponse>(
                "availability/update", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        await service.PushAvailabilityUpdateAsync(roomId, date, availableCount);

        // Assert
        _httpClientMock.Verify(x => x.SendRequestAsync<BookingComResponse>(
            "availability/update", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        
        _xmlSerializerMock.Verify(x => x.Serialize(It.Is<AvailabilityUpdateRequest>(r => 
            r.AvailabilityData.HotelId == 1 && 
            r.AvailabilityData.Rooms.First().Id == roomId &&
            r.AvailabilityData.Rooms.First().Available == availableCount)), Times.Once);
    }

    [Fact]
    public async Task ProcessExternalReservationAsync_ExistingReservation_ShouldUpdateReservation()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<IBookingIntegrationService>();
        var externalReservation = new BookingComReservation
        {
            Id = "BK123456",
            Status = "confirmed",
            HotelId = 1,
            RoomId = 1,
            CheckIn = "2024-12-01",
            CheckOut = "2024-12-03",
            GuestName = "John Doe",
            GuestEmail = "john.doe@email.com",
            NumberOfGuests = 2,
            TotalAmount = 250.00m, // Changed amount
            SpecialRequests = "Updated requests"
        };

        var existingReservation = new ReservationDto
        {
            Id = 1,
            BookingReference = "BK123456",
            CheckInDate = DateTime.Parse("2024-12-01"),
            CheckOutDate = DateTime.Parse("2024-12-03"),
            NumberOfGuests = 2,
            TotalAmount = 200.00m, // Original amount
            Status = ReservationStatus.Confirmed,
            SpecialRequests = "Original requests"
        };

        var reservationService = _serviceProvider.GetRequiredService<IReservationService>();
        Mock.Get(reservationService).Setup(x => x.GetReservationByBookingReferenceAsync("BK123456"))
            .ReturnsAsync(existingReservation);

        var updatedReservation = new ReservationDto
        {
            Id = 1,
            BookingReference = "BK123456",
            TotalAmount = 250.00m,
            SpecialRequests = "Updated requests"
        };

        Mock.Get(reservationService).Setup(x => x.UpdateReservationAsync(1, It.IsAny<UpdateReservationRequest>()))
            .ReturnsAsync(updatedReservation);

        // Act
        var result = await service.ProcessExternalReservationAsync(externalReservation);

        // Assert
        Assert.NotNull(result);
        Mock.Get(reservationService).Verify(x => x.UpdateReservationAsync(1, 
            It.Is<UpdateReservationRequest>(r => 
                r.TotalAmount == 250.00m && 
                r.SpecialRequests == "Updated requests")), Times.Once);
    }

    [Fact]
    public async Task FetchReservationAsync_ShouldReturnReservationFromBookingCom()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<IBookingIntegrationService>();
        var bookingReference = "BK123456";

        var mockResponse = new ReservationSyncResponse
        {
            Reservations = new List<BookingComReservation>
            {
                new BookingComReservation
                {
                    Id = bookingReference,
                    Status = "confirmed",
                    HotelId = 1,
                    RoomId = 1,
                    CheckIn = "2024-12-01",
                    CheckOut = "2024-12-03",
                    GuestName = "John Doe",
                    GuestEmail = "john.doe@email.com",
                    NumberOfGuests = 2,
                    TotalAmount = 200.00m
                }
            }
        };

        _httpClientMock.Setup(x => x.SendRequestAsync<ReservationSyncResponse>(
                $"reservations/{bookingReference}", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await service.FetchReservationAsync(bookingReference);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(bookingReference, result.Id);
        Assert.Equal("confirmed", result.Status);
        _httpClientMock.Verify(x => x.SendRequestAsync<ReservationSyncResponse>(
            $"reservations/{bookingReference}", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleWebhookAsync_EmptyPayload_ShouldThrowArgumentException()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<IBookingIntegrationService>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.HandleWebhookAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => service.HandleWebhookAsync("   "));
        await Assert.ThrowsAsync<ArgumentException>(() => service.HandleWebhookAsync(null!));
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}