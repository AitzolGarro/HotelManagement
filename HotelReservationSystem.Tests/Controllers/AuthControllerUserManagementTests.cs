using FluentAssertions;
using HotelReservationSystem.Controllers;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace HotelReservationSystem.Tests.Controllers;

public class AuthControllerUserManagementTests
{
    private readonly Mock<IAuthService> _authServiceMock = new();
    private readonly Mock<ITwoFactorService> _twoFactorServiceMock = new();
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
    private readonly Mock<ILogger<AuthController>> _loggerMock = new();
    private readonly AuthController _controller;

    public AuthControllerUserManagementTests()
    {
        var storeMock = new Mock<IUserPasswordStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            storeMock.Object,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<User>>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<User>>>());

        _controller = new AuthController(
            _authServiceMock.Object,
            _twoFactorServiceMock.Object,
            _userManagerMock.Object,
            _memoryCache,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateUser_ReturnsCreatedAtAction_WhenValidRequest()
    {
        var request = new CreateUserRequest
        {
            Email = "staff@test.com",
            Password = "Staff123!",
            FirstName = "Test",
            LastName = "Staff",
            Role = UserRole.Staff,
            HotelIds = new[] { 1, 2 }
        };

        var created = new UserDto
        {
            Id = 10,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            IsActive = true,
            AccessibleHotelIds = request.HotelIds
        };

        _authServiceMock.Setup(x => x.CreateUserAsync(request)).ReturnsAsync(created);

        var result = await _controller.CreateUser(request);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(AuthController.GetUser));
        createdResult.RouteValues!["id"].Should().Be(created.Id);
        createdResult.Value.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task CreateUser_ReturnsBadRequest_WhenServiceRejectsRequest()
    {
        var request = new CreateUserRequest
        {
            Email = "dup@test.com",
            Password = "Staff123!",
            FirstName = "Dup",
            LastName = "User",
            Role = UserRole.Staff
        };

        _authServiceMock
            .Setup(x => x.CreateUserAsync(request))
            .ThrowsAsync(new InvalidOperationException("User with this email already exists"));

        var result = await _controller.CreateUser(request);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUsers_ReturnsOk_WithUsers()
    {
        var users = new[]
        {
            new UserDto { Id = 1, Email = "admin@test.com", FirstName = "Admin", LastName = "User", Role = UserRole.Admin, IsActive = true },
            new UserDto { Id = 2, Email = "manager@test.com", FirstName = "Manager", LastName = "User", Role = UserRole.Manager, IsActive = true }
        };

        _authServiceMock.Setup(x => x.GetUsersAsync()).ReturnsAsync(users);

        var result = await _controller.GetUsers();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(users);
    }

    [Fact]
    public async Task DeactivateUser_ReturnsOk_WhenSuccessful()
    {
        _authServiceMock.Setup(x => x.DeactivateUserAsync(7)).ReturnsAsync(true);

        var result = await _controller.DeactivateUser(7);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeactivateUser_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _authServiceMock.Setup(x => x.DeactivateUserAsync(99)).ReturnsAsync(false);

        var result = await _controller.DeactivateUser(99);

        result.Should().BeOfType<NotFoundResult>();
    }
}
