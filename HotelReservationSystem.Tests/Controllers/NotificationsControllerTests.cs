using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using HotelReservationSystem.Models.DTOs;
using System.Net;

namespace HotelReservationSystem.Tests.Controllers;

public class NotificationsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public NotificationsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task GetNotifications_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/api/notifications");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetNotificationStats_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/api/notifications/stats");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUnreadCount_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/api/notifications/unread-count");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateNotification_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var request = new CreateNotificationRequest
        {
            Type = NotificationType.Info,
            Title = "Test Notification",
            Message = "Test message"
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/notifications", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MarkAsRead_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.PutAsync("/api/notifications/1/read", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllAsRead_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.PutAsync("/api/notifications/read-all", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SendEmailNotification_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var request = new EmailNotificationRequest
        {
            Email = "test@example.com",
            Subject = "Test Subject",
            Message = "Test message"
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/notifications/email", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SendBrowserNotification_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var request = new BrowserNotificationRequest
        {
            Title = "Test Browser Notification",
            Body = "Test body"
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/notifications/browser", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SendSystemAlert_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var request = new CreateNotificationRequest
        {
            Type = NotificationType.SystemAlert,
            Title = "System Alert",
            Message = "Critical system issue"
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/notifications/system-alert", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SendReservationUpdate_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var details = "Reservation updated";
        var content = new StringContent(JsonSerializer.Serialize(details), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/notifications/reservation-update?reservationId=1&updateType=Modified", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SendConflictNotification_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var details = "Conflict detected";
        var content = new StringContent(JsonSerializer.Serialize(details), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/notifications/conflict?conflictType=Overbooking", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Note: For authenticated tests, we would need to set up authentication in the test client
    // This would typically involve creating a test user, generating a JWT token, and adding it to the Authorization header
    // Example structure for authenticated tests:

    /*
    private async Task<string> GetAuthTokenAsync()
    {
        // Create test user and get JWT token
        // This would require setting up test authentication
        return "test-jwt-token";
    }

    [Fact]
    public async Task GetNotifications_ShouldReturnNotifications_WhenAuthenticated()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/notifications");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var json = await response.Content.ReadAsStringAsync();
        var notifications = JsonSerializer.Deserialize<List<SystemNotificationDto>>(json, _jsonOptions);
        Assert.NotNull(notifications);
    }
    */
}

// Additional test class for testing with authentication
public class AuthenticatedNotificationsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuthenticatedNotificationsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    // These tests would require proper authentication setup
    // For now, they serve as documentation of expected behavior

    [Fact(Skip = "Requires authentication setup")]
    public async Task CreateNotification_ShouldReturnBadRequest_WhenModelIsInvalid()
    {
        // This test would verify that invalid notification requests return BadRequest
        // with proper model validation errors
    }

    [Fact(Skip = "Requires authentication setup")]
    public async Task CreateNotification_ShouldCreateNotification_WhenModelIsValid()
    {
        // This test would verify that valid notification requests create notifications
        // and return the created notification with a 201 status code
    }

    [Fact(Skip = "Requires authentication setup")]
    public async Task MarkAsRead_ShouldReturnNotFound_WhenNotificationDoesNotExist()
    {
        // This test would verify that attempting to mark a non-existent notification
        // as read returns a 404 Not Found response
    }

    [Fact(Skip = "Requires authentication setup")]
    public async Task MarkAsRead_ShouldReturnNoContent_WhenNotificationExists()
    {
        // This test would verify that marking an existing notification as read
        // returns a 204 No Content response
    }

    [Fact(Skip = "Requires authentication setup")]
    public async Task SendEmailNotification_ShouldReturnBadRequest_WhenEmailIsInvalid()
    {
        // This test would verify that invalid email addresses are rejected
        // with appropriate validation errors
    }

    [Fact(Skip = "Requires authentication setup")]
    public async Task SendSystemAlert_ShouldRequireAdminRole()
    {
        // This test would verify that only users with Admin role can send system alerts
        // Non-admin users should receive a 403 Forbidden response
    }

    [Fact(Skip = "Requires authentication setup")]
    public async Task GetNotificationStats_ShouldReturnCorrectStats()
    {
        // This test would verify that notification statistics are calculated correctly
        // and returned in the expected format
    }
}