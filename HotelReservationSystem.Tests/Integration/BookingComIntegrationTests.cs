using Xunit;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;
using HotelReservationSystem.Services.BookingCom;
using HotelReservationSystem.Models.BookingCom;

namespace HotelReservationSystem.Tests.Integration;

public class BookingComIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly MockHttpMessageHandler _mockHttpHandler;

    public BookingComIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Configure logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Configure Booking.com services
        var configuration = new BookingComConfiguration
        {
            BaseUrl = "https://test.booking.com/",
            Username = "testuser",
            Password = "testpass",
            TimeoutSeconds = 30,
            MaxRetryAttempts = 3,
            RetryDelaySeconds = 2
        };
        
        services.AddSingleton(configuration);
        services.AddScoped<IXmlSerializationService, XmlSerializationService>();
        services.AddScoped<IBookingComAuthenticationService, BookingComAuthenticationService>();
        
        // Mock HTTP client
        _mockHttpHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(_mockHttpHandler);
        services.AddScoped<IBookingComHttpClient>(provider => 
            new BookingComHttpClient(
                httpClient,
                provider.GetRequiredService<IXmlSerializationService>(),
                provider.GetRequiredService<ILogger<BookingComHttpClient>>(),
                configuration));

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task EndToEnd_ReservationSync_SerializesRequestAndDeserializesResponse()
    {
        // Arrange
        var xmlSerializer = _serviceProvider.GetRequiredService<IXmlSerializationService>();
        var httpClient = _serviceProvider.GetRequiredService<IBookingComHttpClient>();

        var request = new ReservationSyncRequest
        {
            Authentication = new BookingComAuthentication
            {
                Username = "testuser",
                Password = "testpass"
            },
            ReservationData = new ReservationSyncData
            {
                HotelId = 1,
                FromDate = "2024-01-01",
                ToDate = "2024-01-31"
            }
        };

        var responseXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<response version=""1.0"">
  <ok />
  <reservations>
    <reservation id=""12345"" status=""confirmed"">
      <hotel_id>1</hotel_id>
      <room_id>101</room_id>
      <checkin>2024-01-15</checkin>
      <checkout>2024-01-18</checkout>
      <guest_name>John Doe</guest_name>
      <guest_email>john.doe@example.com</guest_email>
      <guest_phone>+1234567890</guest_phone>
      <number_of_guests>2</number_of_guests>
      <total_amount>450.00</total_amount>
      <currency>USD</currency>
      <special_requests>Late check-in</special_requests>
    </reservation>
  </reservations>
</response>";

        _mockHttpHandler
            .When(HttpMethod.Post, "https://test.booking.com/reservations/sync")
            .Respond("application/xml", responseXml);

        // Act
        var requestXml = xmlSerializer.Serialize(request);
        var response = await httpClient.SendRequestAsync<ReservationSyncResponse>("reservations/sync", requestXml);

        // Assert
        requestXml.Should().NotBeNullOrEmpty();
        requestXml.Should().Contain("<authentication>");
        requestXml.Should().Contain("<username>testuser</username>");
        requestXml.Should().Contain("<password>testpass</password>");
        requestXml.Should().Contain("hotel_id=\"1\"");
        requestXml.Should().Contain("from_date=\"2024-01-01\"");
        requestXml.Should().Contain("to_date=\"2024-01-31\"");

        response.Should().NotBeNull();
        response.Reservations.Should().HaveCount(1);
        
        var reservation = response.Reservations[0];
        reservation.Id.Should().Be("12345");
        reservation.Status.Should().Be("confirmed");
        reservation.HotelId.Should().Be(1);
        reservation.RoomId.Should().Be(101);
        reservation.GuestName.Should().Be("John Doe");
        reservation.GuestEmail.Should().Be("john.doe@example.com");
        reservation.GuestPhone.Should().Be("+1234567890");
        reservation.NumberOfGuests.Should().Be(2);
        reservation.TotalAmount.Should().Be(450.00m);
        reservation.Currency.Should().Be("USD");
        reservation.SpecialRequests.Should().Be("Late check-in");
    }

    [Fact]
    public async Task EndToEnd_AvailabilityUpdate_SerializesAndSendsCorrectly()
    {
        // Arrange
        var xmlSerializer = _serviceProvider.GetRequiredService<IXmlSerializationService>();
        var httpClient = _serviceProvider.GetRequiredService<IBookingComHttpClient>();

        var request = new AvailabilityUpdateRequest
        {
            Authentication = new BookingComAuthentication
            {
                Username = "testuser",
                Password = "testpass"
            },
            AvailabilityData = new AvailabilityUpdateData
            {
                HotelId = 1,
                Rooms = new List<RoomAvailability>
                {
                    new RoomAvailability
                    {
                        Id = 101,
                        Date = "2024-01-15",
                        Available = 1,
                        Price = 150.00m
                    },
                    new RoomAvailability
                    {
                        Id = 102,
                        Date = "2024-01-15",
                        Available = 0,
                        Price = 200.00m
                    }
                }
            }
        };

        var responseXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<response version=""1.0"">
  <ok />
</response>";

        _mockHttpHandler
            .When(HttpMethod.Post, "https://test.booking.com/availability/update")
            .Respond("application/xml", responseXml);

        // Act
        var requestXml = xmlSerializer.Serialize(request);
        var response = await httpClient.SendRequestAsync<BookingComResponse>("availability/update", requestXml);
        
        // Assert
        requestXml.Should().NotBeNullOrEmpty();
        requestXml.Should().Contain("<availability hotel_id=\"1\">");
        requestXml.Should().Contain("hotel_id=\"1\"");
        requestXml.Should().Contain("<room id=\"101\">");
        requestXml.Should().Contain("<date>2024-01-15</date>");
        requestXml.Should().Contain("<available>1</available>");
        requestXml.Should().Contain("<price>150.00</price>");
        requestXml.Should().Contain("<room id=\"102\">");
        requestXml.Should().Contain("<available>0</available>");

        response.Should().NotBeNull();
        response.Ok.Should().NotBeNull();
    }

    [Fact]
    public async Task EndToEnd_WebhookNotification_DeserializesCorrectly()
    {
        // Arrange
        var xmlSerializer = _serviceProvider.GetRequiredService<IXmlSerializationService>();

        var webhookXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<notification type=""reservation_created"" timestamp=""2024-01-01T10:00:00Z"">
  <reservation id=""67890"" status=""confirmed"">
    <hotel_id>1</hotel_id>
    <room_id>103</room_id>
    <checkin>2024-02-01</checkin>
    <checkout>2024-02-03</checkout>
    <guest_name>Jane Smith</guest_name>
    <guest_email>jane.smith@example.com</guest_email>
    <guest_phone>+9876543210</guest_phone>
    <number_of_guests>1</number_of_guests>
    <total_amount>300.00</total_amount>
    <currency>EUR</currency>
    <created_at>2024-01-01T10:00:00Z</created_at>
    <updated_at>2024-01-01T10:00:00Z</updated_at>
  </reservation>
</notification>";

        // Act
        var notification = xmlSerializer.Deserialize<BookingComWebhookNotification>(webhookXml);

        // Assert
        notification.Should().NotBeNull();
        notification.Type.Should().Be("reservation_created");
        notification.Timestamp.Should().Be("2024-01-01T10:00:00Z");
        
        notification.Reservation.Should().NotBeNull();
        notification.Reservation!.Id.Should().Be("67890");
        notification.Reservation.Status.Should().Be("confirmed");
        notification.Reservation.HotelId.Should().Be(1);
        notification.Reservation.RoomId.Should().Be(103);
        notification.Reservation.GuestName.Should().Be("Jane Smith");
        notification.Reservation.GuestEmail.Should().Be("jane.smith@example.com");
        notification.Reservation.NumberOfGuests.Should().Be(1);
        notification.Reservation.TotalAmount.Should().Be(300.00m);
        notification.Reservation.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task EndToEnd_AuthenticationTest_WorksCorrectly()
    {
        // Arrange
        var authService = _serviceProvider.GetRequiredService<IBookingComAuthenticationService>();

        var responseXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<response version=""1.0"">
  <ok />
  <authenticated>true</authenticated>
  <user_info>
    <username>testuser</username>
    <permissions>read</permissions>
    <permissions>write</permissions>
  </user_info>
</response>";

        _mockHttpHandler
            .When(HttpMethod.Post, "https://test.booking.com/auth/test")
            .Respond("application/xml", responseXml);

        // Act
        var isValid = authService.ValidateCredentials();
        var authResult = await authService.TestAuthenticationAsync();

        // Assert
        isValid.Should().BeTrue();
        authResult.Should().BeTrue();
    }

    [Fact]
    public void Configuration_Validation_WorksCorrectly()
    {
        // Arrange
        var authService = _serviceProvider.GetRequiredService<IBookingComAuthenticationService>();

        // Act
        var isValid = authService.ValidateCredentials();
        var auth = authService.GetAuthentication();

        // Assert
        isValid.Should().BeTrue();
        auth.Should().NotBeNull();
        auth.Username.Should().Be("testuser");
        auth.Password.Should().Be("testpass");
    }

    [Fact]
    public void Debug_AvailabilityXml_CheckQuoteChar()
    {
        var xmlSerializer = _serviceProvider.GetRequiredService<IXmlSerializationService>();
        var request = new AvailabilityUpdateRequest
        {
            AvailabilityData = new AvailabilityUpdateData
            {
                HotelId = 1,
                Rooms = new List<RoomAvailability>
                {
                    new RoomAvailability { Id = 101, Date = "2024-01-15", Available = 1, Price = 150m }
                }
            }
        };
        var xml = xmlSerializer.Serialize(request);
        var idx = xml.IndexOf("hotel_id");
        var snippet = xml.Substring(idx, 15);
        var bytes = System.Text.Encoding.UTF8.GetBytes(snippet);
        var hex = string.Join(" ", bytes.Select(b => b.ToString("X2")));
        // This will show in the test output
        System.Console.WriteLine($"Snippet: '{snippet}' | Hex: {hex}");
        // Don't fail the test, just output information
    }

    public void Dispose()
    {
        _mockHttpHandler?.Dispose();
        _serviceProvider?.Dispose();
    }
}