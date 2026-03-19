using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class PerformanceController : ControllerBase
    {
        private readonly IPerformanceMonitoringService _performanceMonitoring;
        private readonly ILogger<PerformanceController> _logger;

        public PerformanceController(
            IPerformanceMonitoringService performanceMonitoring,
            ILogger<PerformanceController> logger)
        {
            _performanceMonitoring = performanceMonitoring;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el resumen de métricas de rendimiento en tiempo real (avg/min/max, cache hit ratio, consultas lentas)
        /// </summary>
        [HttpGet("metrics/summary")]
        public ActionResult<PerformanceMetricsSummaryDto> GetMetricsSummary()
        {
            try
            {
                var summary = _performanceMonitoring.GetMetricsSummary();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el resumen de métricas de rendimiento");
                return StatusCode(500, "Error interno al obtener el resumen de métricas");
            }
        }

        /// <summary>
        /// Get performance metrics for a specific date
        /// </summary>
        [HttpGet("metrics")]
        public async Task<ActionResult<PerformanceMetrics>> GetMetrics([FromQuery] DateTime? date = null)
        {
            try
            {
                var targetDate = date ?? DateTime.UtcNow.Date;
                var metrics = await _performanceMonitoring.GetMetricsAsync(targetDate);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving performance metrics for date {Date}", date);
                return StatusCode(500, "Internal server error while retrieving performance metrics");
            }
        }

        /// <summary>
        /// Get slow queries for a specific date
        /// </summary>
        [HttpGet("slow-queries")]
        public async Task<ActionResult<IEnumerable<SlowQuery>>> GetSlowQueries(
            [FromQuery] DateTime? date = null,
            [FromQuery] int threshold = 1000)
        {
            try
            {
                var targetDate = date ?? DateTime.UtcNow.Date;
                var slowQueries = await _performanceMonitoring.GetSlowQueriesAsync(targetDate, threshold);
                return Ok(slowQueries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving slow queries for date {Date}", date);
                return StatusCode(500, "Internal server error while retrieving slow queries");
            }
        }

        /// <summary>
        /// Get performance metrics for a date range
        /// </summary>
        [HttpGet("metrics/range")]
        public async Task<ActionResult<IEnumerable<PerformanceMetrics>>> GetMetricsRange(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            try
            {
                if (fromDate > toDate)
                {
                    return BadRequest("From date cannot be greater than to date");
                }

                if ((toDate - fromDate).TotalDays > 30)
                {
                    return BadRequest("Date range cannot exceed 30 days");
                }

                var metrics = new List<PerformanceMetrics>();
                var currentDate = fromDate.Date;

                while (currentDate <= toDate.Date)
                {
                    var dailyMetrics = await _performanceMonitoring.GetMetricsAsync(currentDate);
                    metrics.Add(dailyMetrics);
                    currentDate = currentDate.AddDays(1);
                }

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving performance metrics range from {FromDate} to {ToDate}", fromDate, toDate);
                return StatusCode(500, "Internal server error while retrieving performance metrics range");
            }
        }

        /// <summary>
        /// Get current system health status
        /// </summary>
        [HttpGet("health")]
        public async Task<ActionResult<object>> GetHealthStatus()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var metrics = await _performanceMonitoring.GetMetricsAsync(today);
                var slowQueries = await _performanceMonitoring.GetSlowQueriesAsync(today);

                var health = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Metrics = new
                    {
                        AverageResponseTime = metrics.AverageApiResponseTime,
                        CacheHitRate = metrics.CacheHitRate,
                        SlowQueriesCount = slowQueries.Count(),
                        TotalApiCalls = metrics.TotalApiCalls,
                        TotalQueries = metrics.TotalQueries
                    },
                    Alerts = GetHealthAlerts(metrics, slowQueries)
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving health status");
                return StatusCode(500, new
                {
                    Status = "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Error = "Unable to retrieve health metrics"
                });
            }
        }

        private static List<string> GetHealthAlerts(PerformanceMetrics metrics, IEnumerable<SlowQuery> slowQueries)
        {
            var alerts = new List<string>();

            if (metrics.AverageApiResponseTime > 2000)
            {
                alerts.Add($"High average API response time: {metrics.AverageApiResponseTime:F2}ms");
            }

            if (metrics.CacheHitRate < 70)
            {
                alerts.Add($"Low cache hit rate: {metrics.CacheHitRate:F1}%");
            }

            var slowQueryCount = slowQueries.Count();
            if (slowQueryCount > 10)
            {
                alerts.Add($"High number of slow queries: {slowQueryCount}");
            }

            if (metrics.SlowOperations.Any(op => op.Value > 5000))
            {
                var slowestOp = metrics.SlowOperations.OrderByDescending(op => op.Value).First();
                alerts.Add($"Very slow operation detected: {slowestOp.Key} ({slowestOp.Value:F2}ms)");
            }

            return alerts;
        }
    }
}