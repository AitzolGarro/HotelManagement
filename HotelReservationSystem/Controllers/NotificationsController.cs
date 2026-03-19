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

    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    // GET /api/notifications?page=1&pageSize=20&unreadOnly=false&type=5&hotelId=1
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<SystemNotificationDto>>> GetNotifications(
        [FromQuery] int? hotelId = null,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] NotificationType? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _notificationService.GetUserNotificationsAsync(
                userId, hotelId, unreadOnly, type, page, Math.Clamp(pageSize, 1, 100));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications");
            return StatusCode(500, "Internal server error");
        }
    }

    // GET /api/notifications/unread-count
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount([FromQuery] int? hotelId = null)
    {
        try
        {
            return Ok(await _notificationService.GetUnreadCountAsync(hotelId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread count");
            return StatusCode(500, "Internal server error");
        }
    }

    // GET /api/notifications/stats
    [HttpGet("stats")]
    public async Task<ActionResult<NotificationStatsDto>> GetStats([FromQuery] int? hotelId = null)
    {
        try
        {
            return Ok(await _notificationService.GetNotificationStatsAsync(hotelId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification stats");
            return StatusCode(500, "Internal server error");
        }
    }

    // POST /api/notifications
    [HttpPost]
    public async Task<ActionResult<SystemNotificationDto>> CreateNotification(
        [FromBody] CreateNotificationRequest request)
    {
        try
        {
            var notification = await _notificationService.CreateNotificationAsync(
                request.Type, request.Title, request.Message,
                request.RelatedEntityType, request.RelatedEntityId);
            return CreatedAtAction(nameof(GetNotifications), new { id = notification.Id }, notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification");
            return StatusCode(500, "Internal server error");
        }
    }

    // PUT /api/notifications/{id}/read
    [HttpPut("{id:int}/read")]
    public async Task<ActionResult> MarkAsRead(int id)
    {
        try
        {
            var result = await _notificationService.MarkNotificationAsReadAsync(id);
            return result ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {Id} as read", id);
            return StatusCode(500, "Internal server error");
        }
    }

    // PUT /api/notifications/read-all
    [HttpPut("read-all")]
    public async Task<ActionResult> MarkAllAsRead([FromQuery] int? hotelId = null)
    {
        try
        {
            await _notificationService.MarkAllNotificationsAsReadAsync(hotelId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, "Internal server error");
        }
    }

    // DELETE /api/notifications/{id}
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteNotification(int id)
    {
        try
        {
            var result = await _notificationService.DeleteNotificationAsync(id);
            return result ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    // GET /api/notifications/preferences
    [HttpGet("preferences")]
    public async Task<ActionResult<NotificationPreferenceDto>> GetPreferences([FromQuery] int? guestId = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var prefs = await _notificationService.GetUserPreferencesAsync(userId, guestId);
            return Ok(prefs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification preferences");
            return StatusCode(500, "Internal server error");
        }
    }

    // PUT /api/notifications/preferences
    [HttpPut("preferences")]
    public async Task<ActionResult<NotificationPreferenceDto>> UpdatePreferences(
        [FromBody] UpdateNotificationPreferencesRequest request,
        [FromQuery] int? guestId = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            var prefs = await _notificationService.UpdateUserPreferencesAsync(userId, guestId, request);
            return Ok(prefs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences");
            return StatusCode(500, "Internal server error");
        }
    }

    // POST /api/notifications/send-email
    [HttpPost("send-email")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> SendEmail([FromBody] EmailNotificationRequest request)
    {
        try
        {
            await _notificationService.SendEmailNotificationAsync(request.Email, request.Subject, request.Message);
            return Ok(new { message = "Email queued successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email notification");
            return StatusCode(500, "Internal server error");
        }
    }

    // POST /api/notifications/send-sms
    [HttpPost("send-sms")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> SendSms([FromBody] SmsNotificationRequest request)
    {
        try
        {
            await _notificationService.SendSmsNotificationAsync(request.PhoneNumber, request.Message);
            return Ok(new { message = "SMS queued successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS notification");
            return StatusCode(500, "Internal server error");
        }
    }

    // GET /api/notifications/test
    [HttpGet("test")]
    [AllowAnonymous]
    public ActionResult TestConnection() =>
        Ok(new { message = "Notification API is working", timestamp = DateTime.UtcNow });

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}
