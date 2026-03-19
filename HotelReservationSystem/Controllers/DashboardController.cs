using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Data;

namespace HotelReservationSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService              _dashboardService;
        private readonly INotificationService           _notificationService;
        private readonly IDashboardCustomizationService _customizationService;
        private readonly IWidgetRegistry                _widgetRegistry;
        private readonly ILogger<DashboardController>   _logger;

        public DashboardController(
            IDashboardService dashboardService,
            INotificationService notificationService,
            IDashboardCustomizationService customizationService,
            IWidgetRegistry widgetRegistry,
            ILogger<DashboardController> logger)
        {
            _dashboardService     = dashboardService;
            _notificationService  = notificationService;
            _customizationService = customizationService;
            _widgetRegistry       = widgetRegistry;
            _logger               = logger;
        }

        // ── Existing KPI / operational endpoints (unchanged) ─────────────

        [HttpGet("kpi")]
        public async Task<ActionResult<DashboardKpiDto>> GetKpiData([FromQuery] int? hotelId = null)
        {
            try
            {
                var occupancy   = await _dashboardService.GetOccupancyRatesAsync(hotelId);
                var revenue     = await _dashboardService.GetRevenueTrackingAsync(hotelId);
                var dailyOps    = await _dashboardService.GetDailyOperationsAsync(DateTime.Today, hotelId);
                var notifPanel  = await _dashboardService.GetNotificationPanelAsync(hotelId);

                return Ok(new DashboardKpiDto
                {
                    OccupancyRate  = occupancy,
                    RevenueTracking = revenue,
                    DailyOperations = dailyOps,
                    Notifications   = notifPanel
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving KPI data for hotel {HotelId}", hotelId);
                return StatusCode(500, new { message = "Error retrieving KPI data", error = ex.Message });
            }
        }

        [HttpGet("daily-operations")]
        public async Task<ActionResult<DailyOperationsDto>> GetDailyOperations([FromQuery] int? hotelId = null)
        {
            try
            {
                return Ok(await _dashboardService.GetDailyOperationsAsync(DateTime.Today, hotelId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving daily operations for hotel {HotelId}", hotelId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("notifications")]
        public async Task<ActionResult<DashboardNotificationsDto>> GetNotifications([FromQuery] int? hotelId = null)
        {
            try
            {
                var notifications = await _notificationService.GetNotificationsAsync(hotelId, unreadOnly: false);
                var stats         = await _notificationService.GetNotificationStatsAsync(hotelId);

                return Ok(new DashboardNotificationsDto
                {
                    Notifications  = notifications.Take(10).ToList(),
                    TotalCount     = stats.UnreadCount,
                    CriticalCount  = stats.CountByPriority.GetValueOrDefault(NotificationPriority.Critical, 0),
                    WarningCount   = stats.CountByPriority.GetValueOrDefault(NotificationPriority.High, 0),
                    InfoCount      = stats.CountByPriority.GetValueOrDefault(NotificationPriority.Normal, 0)
                                   + stats.CountByPriority.GetValueOrDefault(NotificationPriority.Low, 0)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for hotel {HotelId}", hotelId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("recent-reservations")]
        public async Task<ActionResult<List<RecentReservationDto>>> GetRecentReservations([FromQuery] int? hotelId = null)
        {
            try
            {
                return Ok(await _dashboardService.GetRecentReservationsAsync(hotelId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent reservations for hotel {HotelId}", hotelId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("notifications/{id}/read")]
        public async Task<ActionResult> MarkNotificationAsRead(int id)
        {
            try
            {
                var result = await _notificationService.MarkNotificationAsReadAsync(id);
                return result ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("notifications/read-all")]
        public async Task<ActionResult> MarkAllNotificationsAsRead([FromQuery] int? hotelId = null)
        {
            try
            {
                await _notificationService.MarkAllNotificationsAsReadAsync(hotelId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for hotel {HotelId}", hotelId);
                return StatusCode(500, "Internal server error");
            }
        }

        // ── Widget registry ───────────────────────────────────────────────

        /// <summary>Returns the list of all available widget descriptors.</summary>
        [HttpGet("widgets")]
        public ActionResult<IReadOnlyList<WidgetDescriptor>> GetAvailableWidgets()
            => Ok(_widgetRegistry.GetDescriptors());

        // ── Layout management ─────────────────────────────────────────────

        /// <summary>Returns the current user's saved dashboard layout.</summary>
        [HttpGet("layout")]
        public async Task<ActionResult<DashboardLayoutDto>> GetLayout()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                return Ok(await _customizationService.GetLayoutAsync(userId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving layout for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>Saves the current user's dashboard layout.</summary>
        [HttpPost("layout")]
        public async Task<ActionResult<DashboardLayoutDto>> SaveLayout([FromBody] SaveLayoutRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var result = await _customizationService.SaveLayoutAsync(userId.Value, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving layout for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>Resets the current user's layout to the default.</summary>
        [HttpDelete("layout")]
        public async Task<ActionResult<DashboardLayoutDto>> ResetLayout()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                return Ok(await _customizationService.ResetLayoutAsync(userId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting layout for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        // ── Widget data ───────────────────────────────────────────────────

        /// <summary>Returns data for a single widget by its ID.</summary>
        [HttpGet("widget-data/{widgetId}")]
        public async Task<ActionResult<WidgetDataResponse>> GetWidgetData(
            string widgetId,
            [FromQuery] int?      hotelId   = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate   = null)
        {
            try
            {
                var result = await _customizationService.GetWidgetDataAsync(
                    widgetId, hotelId, startDate, endDate);

                if (!result.Success && result.Error?.Contains("not found") == true)
                    return NotFound(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading widget data for {WidgetId}", widgetId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>Returns data for all visible widgets in the user's layout.</summary>
        [HttpGet("all-widget-data")]
        public async Task<ActionResult<List<WidgetDataResponse>>> GetAllWidgetData(
            [FromQuery] int?      hotelId   = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate   = null)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var results = await _customizationService.GetAllWidgetDataAsync(
                    userId.Value, hotelId, startDate, endDate);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading all widget data for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }
    }

    // ── Shared DTO (kept in same file to avoid breaking existing references) ──

    public class DashboardNotificationsDto
    {
        public List<SystemNotificationDto> Notifications { get; set; } = new();
        public int TotalCount    { get; set; }
        public int CriticalCount { get; set; }
        public int WarningCount  { get; set; }
        public int InfoCount     { get; set; }
    }
}
