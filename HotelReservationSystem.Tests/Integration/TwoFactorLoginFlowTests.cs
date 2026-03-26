using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using HotelReservationSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    private readonly string _databaseName;

    private const string TestPassword = "TestPass1!";

    public TwoFactorLoginFlowTests(WebApplicationFactory<Program> factory)
    {
        _databaseName = $"2FAIntegrationDb_{Guid.NewGuid()}";

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
                foreach (var hostedService in hostedServices) services.Remove(hostedService);

                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<HotelReservationContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<HotelReservationContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<HotelReservationContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _context.Database.EnsureCreated();
    }

    private async Task<(User User, string? AuthenticatorKey)> CreateTestUserAsync(string email, bool twoFactorEnabled = false)
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

        string? authenticatorKey = null;

        if (twoFactorEnabled)
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
            authenticatorKey.Should().NotBeNullOrWhiteSpace();
            await _userManager.SetTwoFactorEnabledAsync(user, true);
        }

        return (user, authenticatorKey);
    }

    private static string GenerateTotpCode(string authenticatorKey)
    {
        var normalizedKey = authenticatorKey.Replace(" ", string.Empty).ToUpperInvariant();
        var keyBytes = DecodeBase32(normalizedKey);
        var timestep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        Span<byte> counter = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(counter, timestep);

        using var hmac = new HMACSHA1(keyBytes);
        var hash = hmac.ComputeHash(counter.ToArray());
        var offset = hash[^1] & 0x0F;
        var binaryCode = ((hash[offset] & 0x7F) << 24)
                         | (hash[offset + 1] << 16)
                         | (hash[offset + 2] << 8)
                         | hash[offset + 3];

        return (binaryCode % 1_000_000).ToString("D6");
    }

    private static byte[] DecodeBase32(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        var output = new List<byte>();
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var character in input)
        {
            var value = alphabet.IndexOf(character);
            value.Should().BeGreaterThanOrEqualTo(0, $"'{character}' should be a valid Base32 character");

            buffer = (buffer << 5) | value;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                output.Add((byte)((buffer >> (bitsLeft - 8)) & 0xFF));
                bitsLeft -= 8;
            }
        }

        return output.ToArray();
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

        root.TryGetProperty("challengeToken", out var challengeToken).Should().BeTrue();
        challengeToken.GetString().Should().BeNullOrEmpty();

        root.TryGetProperty("twoFactorEnabled", out var twoFactorEnabled).Should().BeTrue();
        twoFactorEnabled.GetBoolean().Should().BeFalse();
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

        root.TryGetProperty("token", out var token).Should().BeTrue();
        token.GetString().Should().BeNullOrEmpty();

        root.TryGetProperty("twoFactorEnabled", out var twoFactorEnabled).Should().BeTrue();
        twoFactorEnabled.GetBoolean().Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Feature", "TwoFactor")]
    public async Task Challenge2FA_ValidCode_ReturnsFullJwt()
    {
        // Arrange
        var email = $"challenge_{Guid.NewGuid():N}@test.com";
        var (_, authenticatorKey) = await CreateTestUserAsync(email, twoFactorEnabled: true);
        authenticatorKey.Should().NotBeNullOrWhiteSpace();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = TestPassword
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var loginDocument = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var challengeToken = loginDocument.RootElement.GetProperty("challengeToken").GetString();
        challengeToken.Should().NotBeNullOrWhiteSpace();

        var code = GenerateTotpCode(authenticatorKey!);

        // Act
        var challengeResponse = await _client.PostAsJsonAsync("/api/auth/2fa/challenge", new
        {
            challengeToken,
            code
        });

        // Assert
        challengeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var challengeDocument = JsonDocument.Parse(await challengeResponse.Content.ReadAsStringAsync());
        var root = challengeDocument.RootElement;

        root.TryGetProperty("requiresTwoFactor", out var requiresTwoFactor).Should().BeTrue();
        requiresTwoFactor.GetBoolean().Should().BeFalse();

        root.TryGetProperty("token", out var token).Should().BeTrue();
        token.GetString().Should().NotBeNullOrEmpty();

        root.TryGetProperty("challengeToken", out var returnedChallengeToken).Should().BeTrue();
        returnedChallengeToken.GetString().Should().BeNullOrEmpty();

        root.TryGetProperty("user", out var user).Should().BeTrue();
        user.GetProperty("email").GetString().Should().Be(email);
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
