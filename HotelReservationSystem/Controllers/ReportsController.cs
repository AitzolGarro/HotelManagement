using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Authorization;

namespace HotelReservationSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportingService reportingService, ILogger<ReportsController> logger)
    {
        _reportingService = reportingService;
        _logger = logger;
    }

    /// <summary>
    /// Generate occupancy report
    /// </summary>
    /// <param name="startDate">Start date for the report</param>
    /// <param name="endDate">End date for the report</param>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Occupancy report data</returns>
    [HttpGet("occupancy")]
    [ProducesResponseType(typeof(OccupancyReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OccupancyReportDto>> GetOccupancyReport(
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

        _logger.LogInformation("Generating occupancy report from {StartDate} to {EndDate} for hotel {HotelId}", 
            startDate, endDate, hotelId);

        try
        {
            var report = await _reportingService.GenerateOccupancyReportAsync(startDate, endDate, hotelId);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating occupancy report");
            return StatusCode(500, "An error occurred while generating the occupancy report");
        }
    }

    /// <summary>
    /// Generate revenue report
    /// </summary>
    /// <param name="startDate">Start date for the report</param>
    /// <param name="endDate">End date for the report</param>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Revenue report data</returns>
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(RevenueReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RevenueReportDto>> GetRevenueReport(
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

        _logger.LogInformation("Generating revenue report from {StartDate} to {EndDate} for hotel {HotelId}", 
            startDate, endDate, hotelId);

        try
        {
            var report = await _reportingService.GenerateRevenueReportAsync(startDate, endDate, hotelId);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating revenue report");
            return StatusCode(500, "An error occurred while generating the revenue report");
        }
    }

    /// <summary>
    /// Generate guest pattern report
    /// </summary>
    /// <param name="startDate">Start date for the report</param>
    /// <param name="endDate">End date for the report</param>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Guest pattern report data</returns>
    [HttpGet("guest-patterns")]
    [ProducesResponseType(typeof(GuestPatternReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GuestPatternReportDto>> GetGuestPatternReport(
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

        _logger.LogInformation("Generating guest pattern report from {StartDate} to {EndDate} for hotel {HotelId}", 
            startDate, endDate, hotelId);

        try
        {
            var report = await _reportingService.GenerateGuestPatternReportAsync(startDate, endDate, hotelId);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating guest pattern report");
            return StatusCode(500, "An error occurred while generating the guest pattern report");
        }
    }

    /// <summary>
    /// Get daily occupancy data
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Daily occupancy data</returns>
    [HttpGet("occupancy/daily")]
    [ProducesResponseType(typeof(List<DailyOccupancyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<DailyOccupancyDto>>> GetDailyOccupancy(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? hotelId = null)
    {
        if (startDate >= endDate)
        {
            return BadRequest("Start date must be before end date");
        }

        if ((endDate - startDate).Days > 90)
        {
            return BadRequest("Date range for daily data cannot exceed 90 days");
        }

        try
        {
            var data = await _reportingService.GetDailyOccupancyAsync(startDate, endDate, hotelId);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily occupancy data");
            return StatusCode(500, "An error occurred while retrieving daily occupancy data");
        }
    }

    /// <summary>
    /// Get monthly revenue data
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Monthly revenue data</returns>
    [HttpGet("revenue/monthly")]
    [ProducesResponseType(typeof(List<MonthlyRevenueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<MonthlyRevenueDto>>> GetMonthlyRevenue(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? hotelId = null)
    {
        if (startDate >= endDate)
        {
            return BadRequest("Start date must be before end date");
        }

        if ((endDate - startDate).Days > 1095) // 3 years
        {
            return BadRequest("Date range for monthly data cannot exceed 3 years");
        }

        try
        {
            var data = await _reportingService.GetMonthlyRevenueAsync(startDate, endDate, hotelId);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monthly revenue data");
            return StatusCode(500, "An error occurred while retrieving monthly revenue data");
        }
    }

    /// <summary>
    /// Get revenue breakdown by source
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Revenue by source data</returns>
    [HttpGet("revenue/by-source")]
    [ProducesResponseType(typeof(List<RevenueSourceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<RevenueSourceDto>>> GetRevenueBySource(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? hotelId = null)
    {
        if (startDate >= endDate)
        {
            return BadRequest("Start date must be before end date");
        }

        try
        {
            var data = await _reportingService.GetRevenueBySourceAsync(startDate, endDate, hotelId);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue by source data");
            return StatusCode(500, "An error occurred while retrieving revenue by source data");
        }
    }

    /// <summary>
    /// Get revenue breakdown by room type
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Revenue by room type data</returns>
    [HttpGet("revenue/by-room-type")]
    [ProducesResponseType(typeof(List<RoomTypeRevenueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<RoomTypeRevenueDto>>> GetRevenueByRoomType(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? hotelId = null)
    {
        if (startDate >= endDate)
        {
            return BadRequest("Start date must be before end date");
        }

        try
        {
            var data = await _reportingService.GetRevenueByRoomTypeAsync(startDate, endDate, hotelId);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue by room type data");
            return StatusCode(500, "An error occurred while retrieving revenue by room type data");
        }
    }

    /// <summary>
    /// Get booking source patterns
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Booking source pattern data</returns>
    [HttpGet("guest-patterns/booking-sources")]
    [ProducesResponseType(typeof(List<BookingSourcePatternDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<BookingSourcePatternDto>>> GetBookingSourcePatterns(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? hotelId = null)
    {
        if (startDate >= endDate)
        {
            return BadRequest("Start date must be before end date");
        }

        try
        {
            var data = await _reportingService.GetBookingSourcePatternsAsync(startDate, endDate, hotelId);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking source patterns");
            return StatusCode(500, "An error occurred while retrieving booking source patterns");
        }
    }

    /// <summary>
    /// Get guest loyalty analysis
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Guest loyalty data</returns>
    [HttpGet("guest-patterns/loyalty")]
    [ProducesResponseType(typeof(List<GuestLoyaltyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<GuestLoyaltyDto>>> GetGuestLoyaltyAnalysis(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? hotelId = null)
    {
        if (startDate >= endDate)
        {
            return BadRequest("Start date must be before end date");
        }

        try
        {
            var data = await _reportingService.GetGuestLoyaltyAnalysisAsync(startDate, endDate, hotelId);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting guest loyalty analysis");
            return StatusCode(500, "An error occurred while retrieving guest loyalty analysis");
        }
    }

    /// <summary>
    /// Export report in specified format
    /// </summary>
    /// <param name="request">Report filter and export request</param>
    /// <returns>Exported report file</returns>
    [HttpPost("export")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportReport([FromBody] ReportFilterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.StartDate >= request.EndDate)
        {
            return BadRequest("Start date must be before end date");
        }

        if ((request.EndDate - request.StartDate).Days > 365)
        {
            return BadRequest("Date range cannot exceed 365 days");
        }

        _logger.LogInformation("Exporting {ReportType} report in {Format} format from {StartDate} to {EndDate}", 
            request.ReportType, request.ExportFormat, request.StartDate, request.EndDate);

        try
        {
            var exportResult = await _reportingService.ExportReportAsync(request);
            
            return File(exportResult.Data, exportResult.ContentType, exportResult.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report");
            return StatusCode(500, "An error occurred while exporting the report");
        }
    }

    /// <summary>
    /// Calculate occupancy variance between two periods
    /// </summary>
    /// <param name="currentStart">Current period start date</param>
    /// <param name="currentEnd">Current period end date</param>
    /// <param name="previousStart">Previous period start date</param>
    /// <param name="previousEnd">Previous period end date</param>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Occupancy variance percentage</returns>
    [HttpGet("variance/occupancy")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<decimal>> GetOccupancyVariance(
        [FromQuery] DateTime currentStart,
        [FromQuery] DateTime currentEnd,
        [FromQuery] DateTime previousStart,
        [FromQuery] DateTime previousEnd,
        [FromQuery] int? hotelId = null)
    {
        if (currentStart >= currentEnd || previousStart >= previousEnd)
        {
            return BadRequest("Start dates must be before end dates");
        }

        try
        {
            var variance = await _reportingService.CalculateOccupancyVarianceAsync(
                currentStart, currentEnd, previousStart, previousEnd, hotelId);
            
            return Ok(Math.Round(variance, 2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating occupancy variance");
            return StatusCode(500, "An error occurred while calculating occupancy variance");
        }
    }

    /// <summary>
    /// Calculate revenue variance between two periods
    /// </summary>
    /// <param name="currentStart">Current period start date</param>
    /// <param name="currentEnd">Current period end date</param>
    /// <param name="previousStart">Previous period start date</param>
    /// <param name="previousEnd">Previous period end date</param>
    /// <param name="hotelId">Optional hotel ID to filter data</param>
    /// <returns>Revenue variance percentage</returns>
    [HttpGet("variance/revenue")]
    [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<decimal>> GetRevenueVariance(
        [FromQuery] DateTime currentStart,
        [FromQuery] DateTime currentEnd,
        [FromQuery] DateTime previousStart,
        [FromQuery] DateTime previousEnd,
        [FromQuery] int? hotelId = null)
    {
        if (currentStart >= currentEnd || previousStart >= previousEnd)
        {
            return BadRequest("Start dates must be before end dates");
        }

        try
        {
            var variance = await _reportingService.CalculateRevenueVarianceAsync(
                currentStart, currentEnd, previousStart, previousEnd, hotelId);
            
            return Ok(Math.Round(variance, 2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating revenue variance");
            return StatusCode(500, "An error occurred while calculating revenue variance");
        }
    }
}