using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using HotelReservationSystem.Models.DTOs;
using System.Security.Claims;

namespace HotelReservationSystem.Hubs
{
    [Authorize]
    public class ReservationHub : Hub
    {
        private readonly ILogger<ReservationHub> _logger;

        public ReservationHub(ILogger<ReservationHub> logger)
        {
            _logger = logger;
        }

        // Calendar-related groups
        public async Task JoinCalendarGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "CalendarUsers");
            _logger.LogDebug("User {UserId} joined calendar group", GetUserId());
        }

        public async Task LeaveCalendarGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "CalendarUsers");
            _logger.LogDebug("User {UserId} left calendar group", GetUserId());
        }

        // Hotel-specific groups
        public async Task JoinHotelGroup(string hotelId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Hotel_{hotelId}");
            _logger.LogDebug("User {UserId} joined hotel group {HotelId}", GetUserId(), hotelId);
        }

        public async Task LeaveHotelGroup(string hotelId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Hotel_{hotelId}");
            _logger.LogDebug("User {UserId} left hotel group {HotelId}", GetUserId(), hotelId);
        }

        // Notification-specific groups
        public async Task JoinNotificationGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "NotificationUsers");
            _logger.LogDebug("User {UserId} joined notification group", GetUserId());
        }

        public async Task LeaveNotificationGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "NotificationUsers");
            _logger.LogDebug("User {UserId} left notification group", GetUserId());
        }

        // User-specific notifications
        public async Task JoinUserGroup()
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                _logger.LogDebug("User {UserId} joined personal notification group", userId);
            }
        }

        public async Task LeaveUserGroup()
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
                _logger.LogDebug("User {UserId} left personal notification group", userId);
            }
        }

        // Admin-only groups for system alerts
        public async Task JoinAdminGroup()
        {
            if (IsAdmin())
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "AdminUsers");
                _logger.LogDebug("Admin user {UserId} joined admin group", GetUserId());
            }
        }

        public async Task LeaveAdminGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AdminUsers");
            _logger.LogDebug("User {UserId} left admin group", GetUserId());
        }

        // Client methods to acknowledge notifications
        public async Task AcknowledgeNotification(int notificationId)
        {
            _logger.LogDebug("User {UserId} acknowledged notification {NotificationId}", GetUserId(), notificationId);
            // This could trigger server-side logic to mark notification as read
            await Clients.Caller.SendAsync("NotificationAcknowledged", notificationId);
        }

        // Request notification history
        public async Task RequestNotificationHistory(int count = 10)
        {
            _logger.LogDebug("User {UserId} requested {Count} recent notifications", GetUserId(), count);
            // This would typically fetch from a service and send back to caller
            await Clients.Caller.SendAsync("NotificationHistoryRequested", count);
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} connected to SignalR hub", userId);

            // Automatically join common groups
            await Groups.AddToGroupAsync(Context.ConnectionId, "CalendarUsers");
            await Groups.AddToGroupAsync(Context.ConnectionId, "NotificationUsers");
            
            // Join user-specific group
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            // Join admin group if user is admin
            if (IsAdmin())
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "AdminUsers");
            }

            // Notify client of successful connection
            await Clients.Caller.SendAsync("Connected", new { UserId = userId, Timestamp = DateTime.UtcNow });

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} disconnected from SignalR hub. Exception: {Exception}", 
                userId, exception?.Message);

            // Groups are automatically cleaned up, but we can do explicit cleanup if needed
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "CalendarUsers");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "NotificationUsers");
            
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        private string? GetUserId()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private bool IsAdmin()
        {
            return Context.User?.IsInRole("Admin") ?? false;
        }

        private string? GetUserRole()
        {
            return Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        }
    }
}