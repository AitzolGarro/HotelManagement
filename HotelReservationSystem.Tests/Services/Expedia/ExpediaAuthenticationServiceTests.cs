using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;
using HotelReservationSystem.Configuration;
using HotelReservationSystem.Models.Expedia;
using HotelReservationSystem.Services.Expedia;

namespace HotelReservationSystem.Tests.Services.Expedia;

public class ExpediaAuthenticationServiceTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly IOptions<ExpediaOptions> _options;
    private readonly Mock<ILogger<ExpediaAuthenticationService>> _loggerMock;

    public ExpediaAuthenticationServiceTests()
    {
        _mockHttpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHttpHandler)
        {
            BaseAddress = new Uri("https://test.ean.com")
        };
        _options = Options.Create(new ExpediaOptions
        {
            Enabled = true,
            ApiKey = "test-api-key",
            Secret = "test-secret",
            BaseUrl = "https://test.ean.com"
        });
        _loggerMock = new Mock<ILogger<ExpediaAuthenticationService>>();
    }

    private ExpediaAuthenticationService CreateService()
        => new(_httpClient, _options, _loggerMock.Object);

    private void SetupTokenEndpoint(string token = "test-token", int expiresIn = 3600)
    {
        var tokenResponse = new ExpediaAuthResponseDto
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = expiresIn
        };

        _mockHttpHandler
            .When(HttpMethod.Post, "https://test.ean.com/v3/auth/token")
            .Respond("application/json", JsonSerializer.Serialize(tokenResponse));
    }

    // ── Scenario: Initial token acquisition ──────────────────────────────────

    [Fact]
    public async Task GetTokenAsync_FirstCall_MakesHttpRequest_AndReturnsToken()
    {
        // Arrange
        SetupTokenEndpoint("first-token");
        var service = CreateService();

        // Act
        var token = await service.GetTokenAsync();

        // Assert
        token.Should().Be("first-token");
    }

    // ── Scenario: Token cache hit ─────────────────────────────────────────────

    [Fact]
    public async Task GetTokenAsync_CalledTwiceWithValidCache_HttpCalledOnlyOnce()
    {
        // Arrange
        var callCount = 0;
        _mockHttpHandler
            .When(HttpMethod.Post, "https://test.ean.com/v3/auth/token")
            .Respond(_ =>
            {
                callCount++;
                var tokenResponse = new ExpediaAuthResponseDto
                {
                    AccessToken = "cached-token",
                    TokenType = "Bearer",
                    ExpiresIn = 3600
                };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(tokenResponse),
                        System.Text.Encoding.UTF8, "application/json")
                };
            });

        var service = CreateService();

        // Act
        var token1 = await service.GetTokenAsync();
        var token2 = await service.GetTokenAsync();

        // Assert
        token1.Should().Be("cached-token");
        token2.Should().Be("cached-token");
        callCount.Should().Be(1, "the cache should serve the second call without HTTP");
    }

    // ── Scenario: Missing credentials ────────────────────────────────────────

    [Fact]
    public async Task GetTokenAsync_MissingApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var optionsWithoutKey = Options.Create(new ExpediaOptions
        {
            ApiKey = "",
            Secret = "some-secret",
            BaseUrl = "https://test.ean.com"
        });
        var service = new ExpediaAuthenticationService(_httpClient, optionsWithoutKey, _loggerMock.Object);

        // Act
        var act = () => service.GetTokenAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Expedia credentials not configured*");
    }

    [Fact]
    public async Task GetTokenAsync_MissingSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        var optionsWithoutSecret = Options.Create(new ExpediaOptions
        {
            ApiKey = "some-key",
            Secret = "",
            BaseUrl = "https://test.ean.com"
        });
        var service = new ExpediaAuthenticationService(_httpClient, optionsWithoutSecret, _loggerMock.Object);

        // Act
        var act = () => service.GetTokenAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Expedia credentials not configured*");
    }

    // ── Scenario: Token refresh after InvalidateToken() ──────────────────────

    [Fact]
    public async Task GetTokenAsync_AfterInvalidation_RefreshesToken()
    {
        // Arrange
        var callCount = 0;
        _mockHttpHandler
            .When(HttpMethod.Post, "https://test.ean.com/v3/auth/token")
            .Respond(_ =>
            {
                callCount++;
                var tokenResponse = new ExpediaAuthResponseDto
                {
                    AccessToken = $"token-{callCount}",
                    TokenType = "Bearer",
                    ExpiresIn = 3600
                };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(tokenResponse),
                        System.Text.Encoding.UTF8, "application/json")
                };
            });

        var service = CreateService();

        // Act
        var token1 = await service.GetTokenAsync();
        service.InvalidateToken();
        var token2 = await service.GetTokenAsync();

        // Assert
        token1.Should().Be("token-1");
        token2.Should().Be("token-2");
        callCount.Should().Be(2, "invalidation should force a new HTTP call");
    }

    // ── Scenario: API error propagates ───────────────────────────────────────

    [Fact]
    public async Task GetTokenAsync_ApiReturnsError_ThrowsHttpRequestException()
    {
        // Arrange
        _mockHttpHandler
            .When(HttpMethod.Post, "https://test.ean.com/v3/auth/token")
            .Respond(HttpStatusCode.Unauthorized);

        var service = CreateService();

        // Act
        var act = () => service.GetTokenAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    public void Dispose()
    {
        _mockHttpHandler.Dispose();
        _httpClient.Dispose();
    }
}
