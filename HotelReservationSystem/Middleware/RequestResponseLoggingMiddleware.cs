using System.Diagnostics;

namespace HotelReservationSystem.Middleware;

/// <summary>
/// Middleware que registra información de cada solicitud y respuesta HTTP.
/// Solo activo cuando el nivel de log es Debug o inferior para evitar overhead en producción.
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    // Rutas excluidas del logging detallado para reducir ruido
    private static readonly HashSet<string> RutasExcluidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health", "/health/details", "/favicon.ico", "/swagger"
    };

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Omitir rutas excluidas para no saturar los logs
        if (DebeOmitirRuta(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        RegistrarSolicitudEntrante(context);

        await _next(context);

        stopwatch.Stop();

        RegistrarRespuestaSaliente(context, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>Registra los datos básicos de la solicitud entrante.</summary>
    private void RegistrarSolicitudEntrante(HttpContext context)
    {
        _logger.LogDebug(
            "Solicitud entrante: {Method} {Path}{QueryString} desde {RemoteIp}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            context.Connection.RemoteIpAddress);
    }

    /// <summary>Registra el código de estado y tiempo de respuesta de la solicitud.</summary>
    private void RegistrarRespuestaSaliente(HttpContext context, long elapsedMs)
    {
        var nivel = DeterminarNivelLog(context.Response.StatusCode, elapsedMs);

        if (nivel == LogLevel.Warning)
        {
            _logger.LogWarning(
                "Respuesta: {Method} {Path} → {StatusCode} en {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsedMs);
        }
        else
        {
            _logger.LogDebug(
                "Respuesta: {Method} {Path} → {StatusCode} en {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsedMs);
        }
    }

    /// <summary>
    /// Determina el nivel de log apropiado según el código de estado HTTP y el tiempo de respuesta.
    /// Respuestas 4xx/5xx o lentas se registran como Warning.
    /// </summary>
    private static LogLevel DeterminarNivelLog(int statusCode, long elapsedMs)
    {
        if (statusCode >= 400 || elapsedMs > 2000)
            return LogLevel.Warning;

        return LogLevel.Debug;
    }

    /// <summary>Verifica si la ruta debe omitirse del logging detallado.</summary>
    private static bool DebeOmitirRuta(PathString path)
    {
        foreach (var ruta in RutasExcluidas)
        {
            if (path.StartsWithSegments(ruta, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
