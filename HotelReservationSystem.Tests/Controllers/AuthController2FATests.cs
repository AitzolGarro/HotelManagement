using System.Text;
using FluentAssertions;
using HotelReservationSystem.Controllers;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace HotelReservationSystem.Tests.Controllers;

public class AuthController2FATests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ITwoFactorService> _twoFactorServiceMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthController2FATests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _twoFactorServiceMock = new Mock<ITwoFactorService>();
        var storeMock = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            storeMock.Object,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<AuthController>>();

        _controller = new AuthController(
            _authServiceMock.Object,
            _twoFactorServiceMock.Object,
            _userManagerMock.Object,
            _memoryCache,
            _loggerMock.Object);
    }

    private static string CreateChallengeToken(string userId, string jti, DateTimeOffset? expiry = null, string type = "2fa-challenge")
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("test-signing-key-1234567890123456"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var exp = expiry ?? DateTimeOffset.UtcNow.AddMinutes(5);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim("type", type),
        };

        var token = new JwtSecurityToken(
            issuer: "tests",
            audience: "tests",
            claims: claims,
            expires: exp.UtcDateTime,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static User CreateUser(int id = 1)
    {
        return new User
        {
            Id = id,
            Email = "test@example.com",
            UserName = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            IsActive = true
        };
    }

    private static int? GetStatusCode(IActionResult? result)
    {
        return result switch
        {
            ObjectResult objectResult => objectResult.StatusCode ?? 200,
            StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
            ContentResult contentResult => contentResult.StatusCode,
            EmptyResult => null,
            _ => null
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task Complete2FAChallenge_ValidChallengeTokenAndCode_Returns200AndAccessToken()
    {
        var user = CreateUser();
        var request = new TwoFactorChallengeRequest
        {
            ChallengeToken = CreateChallengeToken(user.Id.ToString(), Guid.NewGuid().ToString()),
            Code = "123456"
        };
        var expectedResponse = new LoginResponse
        {
            Token = "access-token",
            Expires = DateTime.UtcNow.AddHours(1),
            User = new UserDto { Id = user.Id, Email = user.Email! }
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _twoFactorServiceMock.Setup(x => x.VerifyCodeAsync(user, request.Code)).ReturnsAsync(true);
        _authServiceMock.Setup(x => x.IssueAuthenticatedResponseAsync(user.Id)).ReturnsAsync(expectedResponse);

        var actionResult = await _controller.Complete2FAChallenge(request);

        var contentResult = actionResult.Result.Should().BeOfType<ContentResult>().Subject;
        contentResult.StatusCode.Should().Be(200);
        var response = JsonSerializer.Deserialize<LoginResponse>(contentResult.Content ?? "{}", JsonOptions);
        response.Should().NotBeNull();
        response.Token.Should().Be("access-token");
        _authServiceMock.Verify(x => x.IssueAuthenticatedResponseAsync(user.Id), Times.Once);
    }

    [Fact]
    public async Task Complete2FAChallenge_WrongCode_Returns401()
    {
        var user = CreateUser();
        var request = new TwoFactorChallengeRequest
        {
            ChallengeToken = CreateChallengeToken(user.Id.ToString(), Guid.NewGuid().ToString()),
            Code = "000000"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _twoFactorServiceMock.Setup(x => x.VerifyCodeAsync(user, request.Code)).ReturnsAsync(false);

        var actionResult = await _controller.Complete2FAChallenge(request);

        GetStatusCode(actionResult.Result).Should().Be(401);
        _authServiceMock.Verify(x => x.IssueAuthenticatedResponseAsync(It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Complete2FAChallenge_ExpiredOrInvalidChallengeToken_Returns401(bool expiredToken)
    {
        var request = new TwoFactorChallengeRequest
        {
            ChallengeToken = expiredToken
                ? CreateChallengeToken("1", Guid.NewGuid().ToString(), DateTimeOffset.UtcNow.AddMinutes(-1))
                : "not-a-jwt",
            Code = "123456"
        };

        var actionResult = await _controller.Complete2FAChallenge(request);

        GetStatusCode(actionResult.Result).Should().Be(401);
    }

    [Fact]
    public async Task Complete2FAChallenge_ReplayedJti_Returns401()
    {
        var jti = Guid.NewGuid().ToString();
        var request = new TwoFactorChallengeRequest
        {
            ChallengeToken = CreateChallengeToken("1", jti),
            Code = "123456"
        };

        _memoryCache.Set($"auth:2fa:challenge:consumed:{jti}", true, TimeSpan.FromMinutes(6));

        var actionResult = await _controller.Complete2FAChallenge(request);

        GetStatusCode(actionResult.Result).Should().Be(401);
        _userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Complete2FAChallenge_InvalidModel_Returns400()
    {
        var request = new TwoFactorChallengeRequest
        {
            ChallengeToken = CreateChallengeToken("1", Guid.NewGuid().ToString()),
            Code = "12"
        };

        _controller.ModelState.AddModelError(nameof(TwoFactorChallengeRequest.Code), "The field Code must be a string with a minimum length of 6 and a maximum length of 8.");

        var actionResult = await _controller.Complete2FAChallenge(request);

        var badRequest = actionResult.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }
}
