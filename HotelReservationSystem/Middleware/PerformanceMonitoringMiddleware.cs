using HotelReservationSystem.Services.Interfaces;
using System.Diagnostics;

namespace HotelReservationSystem.Middleware
{
    public class PerformanceMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMonitoringMiddleware> _logger;

        public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IPerformanceMonitoringService performanceMonitoring)
        {
            var stopwatch = Stopwatch.StartNew();
            var endpoint = $"{context.Request.Method} {context.Request.Path}";

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                
                // Record API call performance
                performanceMonitoring.RecordApiCall(endpoint, stopwatch.Elapsed, context.Response.StatusCode);

                // Log slow requests
                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning("Slow request detected: {Endpoint} took {Duration}ms with status {StatusCode}",
                        endpoint, stopwatch.ElapsedMilliseconds, context.Response.StatusCode);
                }

                // Add performance headers for debugging (only if response hasn't started)
                if (!context.Response.HasStarted)
                {
                    if (context.Response.Headers.ContainsKey("X-Response-Time"))
                    {
                        context.Response.Headers.Remove("X-Response-Time");
                    }
                    context.Response.Headers.Add("X-Response-Time", $"{stopwatch.ElapsedMilliseconds}ms");
                }
            }
        }
    }
}