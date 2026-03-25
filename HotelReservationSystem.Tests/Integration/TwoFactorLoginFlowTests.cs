using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using HotelReservationSystem.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelReservationSystem.Tests.Integration;

/// <summary>
/// End-to-end integration tests for 2FA login flow:
/// - Non-2FA user gets full JWT directly
/// - 2FA-enabled user gets challenge token, then exchanges for full JWT
/// </summary>
public class TwoFactorLoginFlowTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly HotelReservationContext _context;
    private readonly UserManager<User> _userManager;

    private const string TestPassword = "TestPass1!";

    public TwoFactorLoginFlowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<HotelReservationContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<HotelReservationContext>(options =>
                {
                    options.UseInMemoryDatabase($"2FAIntegrationDb_{Guid.NewGuid()}");
                });
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<HotelReservationContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _context.Database.EnsureCreated();
    }

    private async Task<User> CreateTestUserAsync(string email, bool twoFactorEnabled = false)
    {
        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Staff,
            IsActive = true,
            PasswordChangedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, TestPassword);
        result.Succeeded.Should().BeTrue($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        if (twoFactorEnabled)
        {
            await _userManager.SetTwoFactorEnabledAsync(user, true);
        }

        return user;
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Feature", "TwoFactor")]
    public async Task Login_NonTwoFactorUser_ReturnsFullJwt()
    {
        // Arrange
        var email = $"no2fa_{Guid.NewGuid():N}@test.com";
        await CreateTestUserAsync(email, twoFactorEnabled: false);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = TestPassword
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Should have full token and requiresTwoFactor = false
        root.TryGetProperty("requiresTwoFactor", out var requiresTwoFactor).Should().BeTrue();
        requiresTwoFactor.GetBoolean().Should().BeFalse();

        root.TryGetProperty("token", out var token).Should().BeTrue();
        token.GetString().Should().NotBeNullOrEmpty();

        // challengeToken should be null or absent
        if (root.TryGetProperty("challengeToken", out var challengeToken))
        {
            challengeToken.ValueKind.Should().BeOneOf(JsonValueKind.Null, JsonValueKind.Undefined);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Feature", "TwoFactor")]
    public async Task Login_TwoFactorUser_ReturnsChallengeToken()
    {
        // Arrange
        var email = $"with2fa_{Guid.NewGuid():N}@test.com";
        await CreateTestUserAsync(email, twoFactorEnabled: true);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = TestPassword
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Should have requiresTwoFactor = true and a challengeToken
        root.TryGetProperty("requiresTwoFactor", out var requiresTwoFactor).Should().BeTrue();
        requiresTwoFactor.GetBoolean().Should().BeTrue();

        root.TryGetProperty("challengeToken", out var challengeToken).Should().BeTrue();
        challengeToken.GetString().Should().NotBeNullOrEmpty();

        // Full accessToken should be absent/empty
        if (root.TryGetProperty("token", out var token))
        {
            token.GetString().Should().BeNullOrEmpty();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Feature", "TwoFactor")]
    public async Task Challenge2FA_ExpiredToken_Returns401()
    {
        // This test verifies the challenge endpoint rejects expired tokens
        // (using a well-formed but expired challenge token)
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwianRpIjoiZmFrZSIsInR5cGUiOiIyZmEtY2hhbGxlbmdlIiwiZXhwIjoxMDAwfQ.invalid";

        var response = await _client.PostAsJsonAsync("/api/auth/2fa/challenge", new
        {
            challengeToken = expiredToken,
            code = "123456"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Feature", "TwoFactor")]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange — regression: wrong password still returns 401 regardless of 2FA status
        var email = $"badpass_{Guid.NewGuid():N}@test.com";
        await CreateTestUserAsync(email, twoFactorEnabled: false);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPassword123!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _client?.Dispose();
    }
}
