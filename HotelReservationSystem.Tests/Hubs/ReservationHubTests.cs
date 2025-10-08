using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HotelReservationSystem.Hubs;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace HotelReservationSystem.Tests.Hubs;

public class ReservationHubTests
{
    private readonly Mock<ILogger<ReservationHub>> _mockLogger;
    private readonly Mock<HubCallerContext> _mockContext;
    private readonly Mock<IGroupManager> _mockGroups;
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly ReservationHub _hub;

    public ReservationHubTests()
    {
        _mockLogger = new Mock<ILogger<ReservationHub>>();
        _mockContext = new Mock<HubCallerContext>();
        _mockGroups = new Mock<IGroupManager>();
        _mockClients = new Mock<IHubCallerClients>();
        _mockClientProxy = new Mock<IClientProxy>();

        // Setup hub context
        _mockContext.Setup(c => c.ConnectionId).Returns("test-connection-id");
        _mockContext.Setup(c => c.User).Returns(CreateTestUser("test-user-id", "TestUser", "Staff"));

        // Setup hub clients
        _mockClients.Setup(c => c.Caller).Returns(_mockClientProxy.Object);

        _hub = new ReservationHub(_mockLogger.Object)
        {
            Context = _mockContext.Object,
            Groups = _mockGroups.Object,
            Clients = _mockClients.Object
        };
    }

    [Fact]
    public async Task JoinCalendarGroup_ShouldAddConnectionToCalendarGroup()
    {
        // Act
        await _hub.JoinCalendarGroup();

        // Assert
        _mockGroups.Verify(
            g => g.AddToGroupAsync("test-connection-id", "CalendarUsers", default),
            Times.Once);
    }

    [Fact]
    public async Task LeaveCalendarGroup_ShouldRemoveConnectionFromCalendarGroup()
    {
        // Act
        await _hub.LeaveCalendarGroup();

        // Assert
        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync("test-connection-id", "CalendarUsers", default),
            Times.Once);
    }

    [Fact]
    public async Task JoinHotelGroup_ShouldAddConnectionToHotelSpecificGroup()
    {
        // Arrange
        var hotelId = "123";

        // Act
        await _hub.JoinHotelGroup(hotelId);

        // Assert
        _mockGroups.Verify(
            g => g.AddToGroupAsync("test-connection-id", "Hotel_123", default),
            Times.Once);
    }

    [Fact]
    public async Task LeaveHotelGroup_ShouldRemoveConnectionFromHotelSpecificGroup()
    {
        // Arrange
        var hotelId = "123";

        // Act
        await _hub.LeaveHotelGroup(hotelId);

        // Assert
        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync("test-connection-id", "Hotel_123", default),
            Times.Once);
    }

    [Fact]
    public async Task JoinNotificationGroup_ShouldAddConnectionToNotificationGroup()
    {
        // Act
        await _hub.JoinNotificationGroup();

        // Assert
        _mockGroups.Verify(
            g => g.AddToGroupAsync("test-connection-id", "NotificationUsers", default),
            Times.Once);
    }

    [Fact]
    public async Task LeaveNotificationGroup_ShouldRemoveConnectionFromNotificationGroup()
    {
        // Act
        await _hub.LeaveNotificationGroup();

        // Assert
        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync("test-connection-id", "NotificationUsers", default),
            Times.Once);
    }

    [Fact]
    public async Task JoinUserGroup_ShouldAddConnectionToUserSpecificGroup()
    {
        // Act
        await _hub.JoinUserGroup();

        // Assert
        _mockGroups.Verify(
            g => g.AddToGroupAsync("test-connection-id", "User_test-user-id", default),
            Times.Once);
    }

    [Fact]
    public async Task LeaveUserGroup_ShouldRemoveConnectionFromUserSpecificGroup()
    {
        // Act
        await _hub.LeaveUserGroup();

        // Assert
        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync("test-connection-id", "User_test-user-id", default),
            Times.Once);
    }

    [Fact]
    public async Task JoinAdminGroup_ShouldAddConnectionToAdminGroup_WhenUserIsAdmin()
    {
        // Arrange
        _mockContext.Setup(c => c.User).Returns(CreateTestUser("admin-user-id", "AdminUser", "Admin"));

        // Act
        await _hub.JoinAdminGroup();

        // Assert
        _mockGroups.Verify(
            g => g.AddToGroupAsync("test-connection-id", "AdminUsers", default),
            Times.Once);
    }

    [Fact]
    public async Task JoinAdminGroup_ShouldNotAddConnectionToAdminGroup_WhenUserIsNotAdmin()
    {
        // Arrange - user is already set up as Staff in constructor

        // Act
        await _hub.JoinAdminGroup();

        // Assert
        _mockGroups.Verify(
            g => g.AddToGroupAsync("test-connection-id", "AdminUsers", default),
            Times.Never);
    }

    [Fact]
    public async Task LeaveAdminGroup_ShouldRemoveConnectionFromAdminGroup()
    {
        // Act
        await _hub.LeaveAdminGroup();

        // Assert
        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync("test-connection-id", "AdminUsers", default),
            Times.Once);
    }

    [Fact]
    public async Task AcknowledgeNotification_ShouldSendAcknowledgmentToClient()
    {
        // Arrange
        var notificationId = 123;

        // Act
        await _hub.AcknowledgeNotification(notificationId);

        // Assert
        _mockClientProxy.Verify(
            c => c.SendCoreAsync("NotificationAcknowledged", It.Is<object[]>(args => args[0].Equals(notificationId)), default),
            Times.Once);
    }

    [Fact]
    public async Task RequestNotificationHistory_ShouldSendRequestToClient()
    {
        // Arrange
        var count = 10;

        // Act
        await _hub.RequestNotificationHistory(count);

        // Assert
        _mockClientProxy.Verify(
            c => c.SendCoreAsync("NotificationHistoryRequested", It.Is<object[]>(args => args[0].Equals(count)), default),
            Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_ShouldJoinDefaultGroups()
    {
        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _mockGroups.Verify(
            g => g.AddToGroupAsync("test-connection-id", "CalendarUsers", default),
            Times.Once);
        _mockGroups.Verify(
            g => g.AddToGroupAsync("test-connection-id", "NotificationUsers", default),
            Times.Once);
        _mockGroups.Verify(
            g => g.AddToGroupAsync("test-connection-id", "User_test-user-id", default),
            Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_ShouldJoinAdminGroup_WhenUserIsAdmin()
    {
        // Arrange
        _mockContext.Setup(c => c.User).Returns(CreateTestUser("admin-user-id", "AdminUser", "Admin"));

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _mockGroups.Verify(
            g => g.AddToGroupAsync("test-connection-id", "AdminUsers", default),
            Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_ShouldNotJoinAdminGroup_WhenUserIsNotAdmin()
    {
        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _mockGroups.Verify(
            g => g.AddToGroupAsync("test-connection-id", "AdminUsers", default),
            Times.Never);
    }

    [Fact]
    public async Task OnConnectedAsync_ShouldSendConnectedMessageToClient()
    {
        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _mockClientProxy.Verify(
            c => c.SendCoreAsync("Connected", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_ShouldRemoveFromDefaultGroups()
    {
        // Act
        await _hub.OnDisconnectedAsync(null);

        // Assert
        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync("test-connection-id", "CalendarUsers", default),
            Times.Once);
        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync("test-connection-id", "NotificationUsers", default),
            Times.Once);
        _mockGroups.Verify(
            g => g.RemoveFromGroupAsync("test-connection-id", "User_test-user-id", default),
            Times.Once);
    }

    private static ClaimsPrincipal CreateTestUser(string userId, string userName, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }
}