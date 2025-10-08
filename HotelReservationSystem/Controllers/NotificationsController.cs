using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Models.DTOs;
using System.Security.Claims;

namespace HotelReservationSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get notifications for the current user/hotel
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SystemNotificationDto>>> GetNotifications(
        [FromQuery] int? hotelId = null,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int limit = 50)
    {
        try
        {
            var notifications = await _notificationService.GetNotificationsAsync(hotelId, unreadOnly);
            
            // Apply limit
            if (limit > 0)
            {
                notifications = notifications.Take(limit).ToList();
            }

            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for hotel {HotelId}", hotelId);
            return StatusCode(500, "An error occurred while retrieving notifications");
        }
    }

    /// <summary>
    /// Get notification statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<NotificationStatsDto>> GetNotificationStats([FromQuery] int? hotelId = null)
    {
        try
        {
            var stats = await _notificationService.GetNotificationStatsAsync(hotelId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification stats for hotel {HotelId}", hotelId);
            return StatusCode(500, "An error occurred while retrieving notification statistics");
        }
    }

    /// <summary>
    /// Get unread notification count
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount([FromQuery] int? hotelId = null)
    {
        try
        {
            var count = await _notificationService.GetUnreadCountAsync(hotelId);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread count for hotel {HotelId}", hotelId);
            return StatusCode(500, "An error occurred while retrieving unread count");
        }
    }

    /// <summary>
    /// Create a new notification
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<SystemNotificationDto>> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var notification = await _notificationService.CreateNotificationAsync(
                request.Type,
                request.Title,
                request.Message,
                request.RelatedEntityType,
                request.RelatedEntityId);

            return CreatedAtAction(nameof(GetNotifications), new { }, notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification: {Title}", request.Title);
            return StatusCode(500, "An error occurred while creating the notification");
        }
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    [HttpPut("{id}/read")]
    public async Task<ActionResult> MarkAsRead(int id)
    {
        try
        {
            var success = await _notificationService.MarkNotificationAsReadAsync(id);
            
            if (!success)
            {
                return NotFound($"Notification with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
            return StatusCode(500, "An error occurred while updating the notification");
        }
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPut("read-all")]
    public async Task<ActionResult> MarkAllAsRead([FromQuery] int? hotelId = null)
    {
        try
        {
            await _notificationService.MarkAllNotificationsAsReadAsync(hotelId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for hotel {HotelId}", hotelId);
            return StatusCode(500, "An error occurred while updating notifications");
        }
    }

    /// <summary>
    /// Send an email notification
    /// </summary>
    [HttpPost("email")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> SendEmailNotification([FromBody] EmailNotificationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _notificationService.SendEmailNotificationAsync(request.Email, request.Subject, request.Message);
            
            _logger.LogInformation("Email notification sent to {Email} by user {UserId}", 
                request.Email, GetCurrentUserId());

            return Ok(new { Message = "Email notification sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email notification to {Email}", request.Email);
            return StatusCode(500, "An error occurred while sending the email notification");
        }
    }

    /// <summary>
    /// Send a browser notification
    /// </summary>
    [HttpPost("browser")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> SendBrowserNotification([FromBody] BrowserNotificationRequest request, [FromQuery] string? userId = null, [FromQuery] int? hotelId = null)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _notificationService.SendBrowserNotificationAsync(request, userId, hotelId);
            
            _logger.LogInformation("Browser notification sent: {Title} by user {UserId}", 
                request.Title, GetCurrentUserId());

            return Ok(new { Message = "Browser notification sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending browser notification: {Title}", request.Title);
            return StatusCode(500, "An error occurred while sending the browser notification");
        }
    }

    /// <summary>
    /// Send a system alert (Admin only)
    /// </summary>
    [HttpPost("system-alert")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SendSystemAlert([FromBody] CreateNotificationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _notificationService.SendSystemAlertAsync(
                request.Type,
                request.Title,
                request.Message,
                request.HotelId);
            
            _logger.LogInformation("System alert sent: {Title} by admin {UserId}", 
                request.Title, GetCurrentUserId());

            return Ok(new { Message = "System alert sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending system alert: {Title}", request.Title);
            return StatusCode(500, "An error occurred while sending the system alert");
        }
    }

    /// <summary>
    /// Send a reservation update notification
    /// </summary>
    [HttpPost("reservation-update")]
    public async Task<ActionResult> SendReservationUpdate(
        [FromQuery] int reservationId,
        [FromQuery] string updateType,
        [FromBody] string details,
        [FromQuery] int? hotelId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(updateType) || string.IsNullOrWhiteSpace(details))
            {
                return BadRequest("UpdateType and details are required");
            }

            await _notificationService.SendReservationUpdateNotificationAsync(
                reservationId, updateType, details, hotelId);
            
            return Ok(new { Message = "Reservation update notification sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reservation update notification for reservation {ReservationId}", reservationId);
            return StatusCode(500, "An error occurred while sending the reservation update notification");
        }
    }

    /// <summary>
    /// Send a conflict notification
    /// </summary>
    [HttpPost("conflict")]
    public async Task<ActionResult> SendConflictNotification(
        [FromQuery] string conflictType,
        [FromBody] string details,
        [FromQuery] int? hotelId = null,
        [FromQuery] int? reservationId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(conflictType) || string.IsNullOrWhiteSpace(details))
            {
                return BadRequest("ConflictType and details are required");
            }

            await _notificationService.SendConflictNotificationAsync(
                conflictType, details, hotelId, reservationId);
            
            return Ok(new { Message = "Conflict notification sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending conflict notification: {ConflictType}", conflictType);
            return StatusCode(500, "An error occurred while sending the conflict notification");
        }
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}