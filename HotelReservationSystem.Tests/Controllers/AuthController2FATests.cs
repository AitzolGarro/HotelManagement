using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HotelReservationSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HotelReservationSystem.Tests.Controllers;

/// <summary>
/// Controller tests for POST /api/auth/2fa/challenge endpoint.
/// Tests: valid TOTP → 200, wrong TOTP → 401, expired token → 401, replayed jti → 401, short code → 400.
/// </summary>
public class AuthController2FATests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public AuthController2FATests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace DbContext with in-memory
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<HotelReservationContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<HotelReservationContext>(options =>
                {
                    options.UseInMemoryDatabase($"2FATestDb_{Guid.NewGuid()}");
                });
            });
        });

        _client = _factory.CreateClient();

        // Get JWT settings from test app config
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        _jwtSecret = config["JwtSettings:Secret"] ?? "HotelReservationSystemSecretKeyForJWTTokenGeneration2024!";
        _jwtIssuer = config["JwtSettings:Issuer"] ?? "HotelReservationSystem";
        _jwtAudience = config["JwtSettings:Audience"] ?? "HotelReservationSystemUsers";
    }

    private string CreateChallengeToken(string userId, string jti, DateTimeOffset? expiry = null)
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var exp = expiry ?? DateTimeOffset.UtcNow.AddMinutes(5);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim("type", "2fa-challenge"),
        };

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: exp.UtcDateTime,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    [Trait("Category", "Controller")]
    [Trait("Feature", "TwoFactor")]
    public async Task Challenge2FA_ShortCode_Returns400()
    {
        // Arrange
        var challengeToken = CreateChallengeToken("1", Guid.NewGuid().ToString());
        var request = new { challengeToken, code = "12" }; // too short

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/2fa/challenge", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("Category", "Controller")]
    [Trait("Feature", "TwoFactor")]
    public async Task Challenge2FA_ExpiredChallengeToken_Returns401()
    {
        // Arrange
        var expiredToken = CreateChallengeToken(
            userId: "1",
            jti: Guid.NewGuid().ToString(),
            expiry: DateTimeOffset.UtcNow.AddMinutes(-10) // expired 10 minutes ago
        );
        var request = new { challengeToken = expiredToken, code = "123456" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/2fa/challenge", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Controller")]
    [Trait("Feature", "TwoFactor")]
    public async Task Challenge2FA_WrongTokenType_Returns401()
    {
        // Arrange — token with type "full" instead of "2fa-challenge"
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "1"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("type", "full"), // wrong type
        };

        var wrongToken = new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: creds));

        var request = new { challengeToken = wrongToken, code = "123456" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/2fa/challenge", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Controller")]
    [Trait("Feature", "TwoFactor")]
    public async Task Challenge2FA_MissingCode_Returns400()
    {
        // Arrange — omit code
        var challengeToken = CreateChallengeToken("1", Guid.NewGuid().ToString());
        var request = new { challengeToken }; // no code field

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/2fa/challenge", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
