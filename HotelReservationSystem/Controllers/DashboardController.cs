using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Authorization;

namespace HotelReservationSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get complete dashboard KPI data
    /// </summary>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Dashboard KPI data</returns>
    [HttpGet("kpi")]
    [ProducesResponseType(typeof(DashboardKpiDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardKpiDto>> GetDashboardKpi([FromQuery] int? hotelId = null)
    {
        _logger.LogInformation("Getting dashboard KPI data for hotel {HotelId}", hotelId);
        
        try
        {
            var kpiData = await _dashboardService.GetDashboardKpiAsync(hotelId);
            return Ok(kpiData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard KPI data for hotel {HotelId}", hotelId);
            return StatusCode(500, "An error occurred while retrieving dashboard data");
        }
    }

    /// <summary>
    /// Get occupancy rate data
    /// </summary>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Occupancy rate data</returns>
    [HttpGet("occupancy")]
    [ProducesResponseType(typeof(OccupancyRateDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OccupancyRateDto>> GetOccupancyRates([FromQuery] int? hotelId = null)
    {
        _logger.LogInformation("Getting occupancy rates for hotel {HotelId}", hotelId);
        
        try
        {
            var occupancyData = await _dashboardService.GetOccupancyRatesAsync(hotelId);
            return Ok(occupancyData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting occupancy rates for hotel {HotelId}", hotelId);
            return StatusCode(500, "An error occurred while retrieving occupancy data");
        }
    }

    /// <summary>
    /// Get revenue tracking data
    /// </summary>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <param name="startDate">Optional start date for custom range</param>
    /// <param name="endDate">Optional end date for custom range</param>
    /// <returns>Revenue tracking data</returns>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(RevenueTrackingDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RevenueTrackingDto>> GetRevenueTracking(
        [FromQuery] int? hotelId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        _logger.LogInformation("Getting revenue tracking for hotel {HotelId} from {StartDate} to {EndDate}", 
            hotelId, startDate, endDate);
        
        try
        {
            var revenueData = await _dashboardService.GetRevenueTrackingAsync(hotelId, startDate, endDate);
            return Ok(revenueData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue tracking for hotel {HotelId}", hotelId);
            return StatusCode(500, "An error occurred while retrieving revenue data");
        }
    }

    /// <summary>
    /// Get daily operations data (check-ins and check-outs)
    /// </summary>
    /// <param name="date">Optional date (defaults to today)</param>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Daily operations data</returns>
    [HttpGet("daily-operations")]
    [ProducesResponseType(typeof(DailyOperationsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DailyOperationsDto>> GetDailyOperations(
        [FromQuery] DateTime? date = null,
        [FromQuery] int? hotelId = null)
    {
        var targetDate = date ?? DateTime.Today;
        _logger.LogInformation("Getting daily operations for {Date} and hotel {HotelId}", targetDate, hotelId);
        
        try
        {
            var operationsData = await _dashboardService.GetDailyOperationsAsync(targetDate, hotelId);
            return Ok(operationsData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily operations for {Date} and hotel {HotelId}", targetDate, hotelId);
            return StatusCode(500, "An error occurred while retrieving daily operations data");
        }
    }

    /// <summary>
    /// Get notification panel data
    /// </summary>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Notification panel data</returns>
    [HttpGet("notifications")]
    [ProducesResponseType(typeof(NotificationPanelDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationPanelDto>> GetNotifications([FromQuery] int? hotelId = null)
    {
        _logger.LogInformation("Getting notifications for hotel {HotelId}", hotelId);
        
        try
        {
            var notificationData = await _dashboardService.GetNotificationPanelAsync(hotelId);
            return Ok(notificationData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for hotel {HotelId}", hotelId);
            return StatusCode(500, "An error occurred while retrieving notifications");
        }
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <returns>Success status</returns>
    [HttpPut("notifications/{notificationId}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MarkNotificationAsRead(int notificationId)
    {
        _logger.LogInformation("Marking notification {NotificationId} as read", notificationId);
        
        try
        {
            var result = await _dashboardService.MarkNotificationAsReadAsync(notificationId);
            
            if (!result)
            {
                return NotFound($"Notification with ID {notificationId} not found");
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            return StatusCode(500, "An error occurred while updating the notification");
        }
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    /// <param name="hotelId">Optional hotel ID to filter notifications</param>
    /// <returns>Success status</returns>
    [HttpPut("notifications/read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> MarkAllNotificationsAsRead([FromQuery] int? hotelId = null)
    {
        _logger.LogInformation("Marking all notifications as read for hotel {HotelId}", hotelId);
        
        try
        {
            await _dashboardService.MarkAllNotificationsAsReadAsync(hotelId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for hotel {HotelId}", hotelId);
            return StatusCode(500, "An error occurred while updating notifications");
        }
    }

    /// <summary>
    /// Get daily revenue breakdown for a date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Daily revenue breakdown</returns>
    [HttpGet("revenue/daily")]
    [ProducesResponseType(typeof(List<DailyRevenueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<DailyRevenueDto>>> GetDailyRevenueBreakdown(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? hotelId = null)
    {
        if (startDate >= endDate)
        {
            return BadRequest("Start date must be before end date");
        }

        if ((endDate - startDate).Days > 365)
        {
            return BadRequest("Date range cannot exceed 365 days");
        }

        _logger.LogInformation("Getting daily revenue breakdown from {StartDate} to {EndDate} for hotel {HotelId}", 
            startDate, endDate, hotelId);
        
        try
        {
            var revenueData = await _dashboardService.GetDailyRevenueBreakdownAsync(startDate, endDate, hotelId);
            return Ok(revenueData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily revenue breakdown for hotel {HotelId}", hotelId);
            return StatusCode(500, "An error occurred while retrieving revenue breakdown");
        }
    }

    /// <summary>
    /// Get weekly revenue breakdown for a date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Weekly revenue breakdown</returns>
    [HttpGet("revenue/weekly")]
    [ProducesResponseType(typeof(List<WeeklyRevenueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<WeeklyRevenueDto>>> GetWeeklyRevenueBreakdown(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? hotelId = null)
    {
        if (startDate >= endDate)
        {
            return BadRequest("Start date must be before end date");
        }

        if ((endDate - startDate).Days > 730)
        {
            return BadRequest("Date range cannot exceed 2 years");
        }

        _logger.LogInformation("Getting weekly revenue breakdown from {StartDate} to {EndDate} for hotel {HotelId}", 
            startDate, endDate, hotelId);
        
        try
        {
            var revenueData = await _dashboardService.GetWeeklyRevenueBreakdownAsync(startDate, endDate, hotelId);
            return Ok(revenueData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weekly revenue breakdown for hotel {HotelId}", hotelId);
            return StatusCode(500, "An error occurred while retrieving revenue breakdown");
        }
    }

    /// <summary>
    /// Calculate occupancy rate for a specific date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Occupancy rate percentage</returns>
    [HttpGet("occupancy/calculate")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<decimal>> CalculateOccupancyRate(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? hotelId = null)
    {
        if (startDate >= endDate)
        {
            return BadRequest("Start date must be before end date");
        }

        if ((endDate - startDate).Days > 365)
        {
            return BadRequest("Date range cannot exceed 365 days");
        }

        _logger.LogInformation("Calculating occupancy rate from {StartDate} to {EndDate} for hotel {HotelId}", 
            startDate, endDate, hotelId);
        
        try
        {
            var occupancyRate = await _dashboardService.CalculateOccupancyRateAsync(startDate, endDate, hotelId);
            return Ok(Math.Round(occupancyRate, 2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating occupancy rate for hotel {HotelId}", hotelId);
            return StatusCode(500, "An error occurred while calculating occupancy rate");
        }
    }

    /// <summary>
    /// Calculate total revenue for a specific date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Total revenue amount</returns>
    [HttpGet("revenue/calculate")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<decimal>> CalculateRevenue(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? hotelId = null)
    {
        if (startDate >= endDate)
        {
            return BadRequest("Start date must be before end date");
        }

        if ((endDate - startDate).Days > 365)
        {
            return BadRequest("Date range cannot exceed 365 days");
        }

        _logger.LogInformation("Calculating revenue from {StartDate} to {EndDate} for hotel {HotelId}", 
            startDate, endDate, hotelId);
        
        try
        {
            var revenue = await _dashboardService.CalculateRevenueAsync(startDate, endDate, hotelId);
            return Ok(revenue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating revenue for hotel {HotelId}", hotelId);
            return StatusCode(500, "An error occurred while calculating revenue");
        }
    }

    /// <summary>
    /// Get recent reservations
    /// </summary>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <param name="limit">Number of reservations to return (default: 10)</param>
    /// <returns>Recent reservations</returns>
    [HttpGet("recent-reservations")]
    [ProducesResponseType(typeof(List<RecentReservationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RecentReservationDto>>> GetRecentReservations(
        [FromQuery] int? hotelId = null,
        [FromQuery] int limit = 10)
    {
        if (limit <= 0 || limit > 50)
        {
            return BadRequest("Limit must be between 1 and 50");
        }

        _logger.LogInformation("Getting recent reservations for hotel {HotelId}, limit {Limit}", hotelId, limit);
        
        try
        {
            var recentReservations = await _dashboardService.GetRecentReservationsAsync(hotelId, limit);
            return Ok(recentReservations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent reservations for hotel {HotelId}", hotelId);
            return StatusCode(500, "An error occurred while retrieving recent reservations");
        }
    }
}