using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Controllers
{
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

        [HttpGet]
        public async Task<ActionResult<List<SystemNotificationDto>>> GetNotifications(
            [FromQuery] int? hotelId = null,
            [FromQuery] bool unreadOnly = false,
            [FromQuery] int limit = 20)
        {
            try
            {
                var notifications = await _notificationService.GetNotificationsAsync(hotelId, unreadOnly);
                return Ok(notifications.Take(limit).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                return StatusCode(500, "Internal server error");
            }
        }

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
                _logger.LogError(ex, "Error retrieving unread notification count");
                return StatusCode(500, "Internal server error");
            }
        }

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
                _logger.LogError(ex, "Error retrieving notification statistics");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}/read")]
        public async Task<ActionResult> MarkAsRead(int id)
        {
            try
            {
                var result = await _notificationService.MarkNotificationAsReadAsync(id);
                if (result)
                {
                    return Ok();
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
                return StatusCode(500, "Internal server error");
            }
        }

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

        [HttpPost]
        public async Task<ActionResult<SystemNotificationDto>> CreateNotification(
            [FromBody] CreateNotificationRequest request)
        {
            try
            {
                var notification = await _notificationService.CreateNotificationAsync(
                    request.Type,
                    request.Title,
                    request.Message,
                    request.RelatedEntityType,
                    request.RelatedEntityId);

                return CreatedAtAction(nameof(GetNotifications), new { id = notification.Id }, notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("test")]
        public ActionResult TestConnection()
        {
            return Ok(new { message = "Notification API is working", timestamp = DateTime.UtcNow });
        }
    }

    public class CreateNotificationRequest
    {
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
    }
}