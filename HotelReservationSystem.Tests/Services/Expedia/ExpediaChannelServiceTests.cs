using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;
using HotelReservationSystem.Configuration;
using HotelReservationSystem.Data;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Models.Expedia;
using HotelReservationSystem.Services.Expedia;

namespace HotelReservationSystem.Tests.Services.Expedia;

public class ExpediaChannelServiceTests : IDisposable
{
    private readonly MockHttpMessageHandler _authMockHandler;
    private readonly MockHttpMessageHandler _apiMockHandler;
    private readonly HotelReservationContext _dbContext;
    private readonly IOptions<ExpediaOptions> _options;

    private const string TestBaseUrl = "https://test.ean.com";
    private const string TestToken = "test-bearer-token";

    public ExpediaChannelServiceTests()
    {
        _options = Options.Create(new ExpediaOptions
        {
            Enabled = true,
            ApiKey = "test-key",
            Secret = "test-secret",
            BaseUrl = TestBaseUrl
        });

        // Auth service mock handler
        _authMockHandler = new MockHttpMessageHandler();
        _authMockHandler
            .When(HttpMethod.Post, $"{TestBaseUrl}/v3/auth/token")
            .Respond("application/json", JsonSerializer.Serialize(new ExpediaAuthResponseDto
            {
                AccessToken = TestToken,
                TokenType = "Bearer",
                ExpiresIn = 3600
            }));

        // API HTTP client mock handler
        _apiMockHandler = new MockHttpMessageHandler();

        // In-memory database
        var dbOptions = new DbContextOptionsBuilder<HotelReservationContext>()
            .UseInMemoryDatabase($"ExpediaChannelServiceTests_{Guid.NewGuid()}")
            .Options;
        _dbContext = new HotelReservationContext(dbOptions);
    }

    private (ExpediaAuthenticationService authService, ExpediaHttpClient httpClient) CreateClients()
    {
        var authHttpClient = new HttpClient(_authMockHandler) { BaseAddress = new Uri(TestBaseUrl) };
        var authService = new ExpediaAuthenticationService(authHttpClient, _options,
            new Mock<ILogger<ExpediaAuthenticationService>>().Object);

        var apiHttpClient = new HttpClient(_apiMockHandler) { BaseAddress = new Uri(TestBaseUrl) };
        var expediaHttpClient = new ExpediaHttpClient(apiHttpClient, authService,
            new Mock<ILogger<ExpediaHttpClient>>().Object);

        return (authService, expediaHttpClient);
    }

    private ExpediaChannelService CreateService()
    {
        var (_, httpClient) = CreateClients();
        return new ExpediaChannelService(
            CreateClients().Item1,
            httpClient,
            _dbContext,
            _options,
            new Mock<ILogger<ExpediaChannelService>>().Object);
    }

    // ── Scenario: GetReservationsAsync sets Source = Expedia ─────────────────

    [Fact]
    public async Task GetReservationsAsync_ReturnsReservations_WithExpediaSource()
    {
        // Arrange
        var expediaReservations = new ExpediaReservationsResponseDto
        {
            Reservations = new List<ExpediaReservationDto>
            {
                new()
                {
                    BookingId = "EXP-001",
                    CheckInDate = "2026-04-01",
                    CheckOutDate = "2026-04-05",
                    NumberOfGuests = 2,
                    TotalAmount = 400m,
                    Status = "confirmed",
                    PrimaryGuest = new ExpediaGuestDto
                    {
                        FirstName = "John",
                        LastName = "Doe",
                        Email = "john@example.com"
                    }
                }
            },
            Cursor = null
        };

        _apiMockHandler
            .When(HttpMethod.Get, $"{TestBaseUrl}/v3/properties/1/reservations")
            .Respond("application/json", JsonSerializer.Serialize(expediaReservations));

        var service = CreateService();

        // Act
        var reservations = (await service.GetReservationsAsync(hotelId: 1)).ToList();

        // Assert
        reservations.Should().HaveCount(1);
        reservations[0].Source.Should().Be(ReservationSource.Expedia,
            "all reservations from Expedia must carry Source = Expedia");
        reservations[0].BookingReference.Should().Be("EXP-001");
        reservations[0].GuestName.Should().Be("John Doe");
    }

    // ── Scenario: GetReservationsAsync — API error returns empty list ─────────

    [Fact]
    public async Task GetReservationsAsync_ApiError_ReturnsEmptyList_DoesNotThrow()
    {
        // Arrange
        _apiMockHandler
            .When(HttpMethod.Get, $"{TestBaseUrl}/v3/properties/99/reservations")
            .Respond(HttpStatusCode.ServiceUnavailable);

        var service = CreateService();

        // Act
        var act = async () => await service.GetReservationsAsync(hotelId: 99);
        var result = await act.Should().NotThrowAsync();

        result.Subject.Should().BeEmpty("API errors must not propagate to caller");
    }

    // ── Scenario: SyncInventoryAsync returns true on 2xx ─────────────────────

    [Fact]
    public async Task SyncInventoryAsync_Returns_True_On_Success()
    {
        // Arrange
        _apiMockHandler
            .When(HttpMethod.Put, $"{TestBaseUrl}/v3/properties/1/availability")
            .Respond(HttpStatusCode.OK, "application/json", "{}");

        var service = CreateService();

        // Act
        var result = await service.SyncInventoryAsync(hotelId: 1, DateTime.Today, DateTime.Today.AddMonths(1));

        // Assert
        result.Should().BeTrue();
    }

    // ── Scenario: SyncInventoryAsync returns false on 4xx ────────────────────

    [Fact]
    public async Task SyncInventoryAsync_Returns_False_On_ApiError()
    {
        // Arrange
        _apiMockHandler
            .When(HttpMethod.Put, $"{TestBaseUrl}/v3/properties/1/availability")
            .Respond(HttpStatusCode.BadRequest);

        var service = CreateService();

        // Act
        var result = await service.SyncInventoryAsync(hotelId: 1, DateTime.Today, DateTime.Today.AddMonths(1));

        // Assert
        result.Should().BeFalse();
    }

    // ── Scenario: Feature flag disabled skips sync ────────────────────────────

    [Fact]
    public async Task SyncInventoryAsync_WhenDisabled_ReturnsTrueWithoutHttpCall()
    {
        // Arrange — no mock setup; any HTTP call would throw from the mock handler
        var disabledOptions = Options.Create(new ExpediaOptions
        {
            Enabled = false,
            ApiKey = "key",
            Secret = "secret",
            BaseUrl = TestBaseUrl
        });

        var authHttpClient = new HttpClient(_authMockHandler) { BaseAddress = new Uri(TestBaseUrl) };
        var authService = new ExpediaAuthenticationService(authHttpClient, disabledOptions,
            new Mock<ILogger<ExpediaAuthenticationService>>().Object);

        var apiHttpClient = new HttpClient(_apiMockHandler) { BaseAddress = new Uri(TestBaseUrl) };
        var httpClient = new ExpediaHttpClient(apiHttpClient, authService,
            new Mock<ILogger<ExpediaHttpClient>>().Object);

        var service = new ExpediaChannelService(authService, httpClient, _dbContext, disabledOptions,
            new Mock<ILogger<ExpediaChannelService>>().Object);

        // Act
        var result = await service.SyncInventoryAsync(hotelId: 1, DateTime.Today, DateTime.Today.AddMonths(1));

        // Assert
        result.Should().BeTrue("disabled path should be a no-op success");
    }

    // ── Scenario: AuthenticateAsync returns true on valid token ───────────────

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.AuthenticateAsync("ignored", "ignored");

        // Assert
        result.Should().BeTrue();
    }

    public void Dispose()
    {
        _authMockHandler.Dispose();
        _apiMockHandler.Dispose();
        _dbContext.Dispose();
    }
}
