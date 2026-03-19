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

                // Registrar tiempo de respuesta de la API usando el nuevo método dedicado
                performanceMonitoring.RecordApiResponseTime(endpoint, stopwatch.Elapsed.TotalMilliseconds);

                // Registrar también con el método existente para compatibilidad con métricas por fecha
                performanceMonitoring.RecordApiCall(endpoint, stopwatch.Elapsed, context.Response.StatusCode);

                // Registrar solicitudes lentas (más de 1 segundo)
                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning("Solicitud lenta detectada: {Endpoint} tardó {Duration}ms con estado {StatusCode}",
                        endpoint, stopwatch.ElapsedMilliseconds, context.Response.StatusCode);
                }

                // Agregar cabecera de tiempo de respuesta para depuración (solo si la respuesta no ha iniciado)
                if (!context.Response.HasStarted)
                {
                    context.Response.Headers["X-Response-Time"] = $"{stopwatch.ElapsedMilliseconds}ms";
                }
            }
        }
    }
}