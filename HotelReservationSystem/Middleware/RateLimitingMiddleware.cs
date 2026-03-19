using System.Collections.Concurrent;
using HotelReservationSystem.Configuration;
using Microsoft.Extensions.Options;

namespace HotelReservationSystem.Middleware;

/// <summary>
/// Middleware para limitar la tasa de solicitudes por usuario autenticado o por dirección IP
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitSettings _settings;

    // Almacenamiento en memoria: clave -> (contador, tiempo de expiración)
    private static readonly ConcurrentDictionary<string, RateLimitEntry> _requestCounts = new();

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IOptions<RateLimitSettings> settings)
    {
        _next = next;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <summary>
    /// Procesa la solicitud HTTP aplicando la limitación de tasa configurada
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Si el rate limiting está deshabilitado, continuar sin restricciones
        if (!_settings.Enabled)
        {
            await _next(context);
            return;
        }

        // Verificar si la ruta está excluida del rate limiting
        if (IsExcludedPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var cacheKey = BuildCacheKey(context);
        var window = TimeSpan.FromSeconds(_settings.WindowSizeSeconds);
        var limit = _settings.RequestsPerWindow;

        var entry = GetOrCreateEntry(cacheKey, window);
        var currentCount = IncrementCounter(entry);

        // Calcular tiempo restante hasta que se reinicie la ventana
        var resetTime = entry.WindowStart.Add(window);
        var resetEpoch = new DateTimeOffset(resetTime).ToUnixTimeSeconds();
        var remaining = Math.Max(0, limit - currentCount);

        // Verificar si se excedió el límite
        if (currentCount > limit)
        {
            await HandleRateLimitExceeded(context, limit, resetEpoch, resetTime);
            return;
        }

        // Agregar encabezados de rate limit a la respuesta
        context.Response.OnStarting(() =>
        {
            AgregarEncabezadosRateLimit(context.Response, limit, remaining, resetEpoch);
            return Task.CompletedTask;
        });

        await _next(context);
    }

    /// <summary>
    /// Verifica si la ruta de la solicitud está en la lista de exclusiones
    /// </summary>
    private bool IsExcludedPath(PathString path)
    {
        return _settings.ExcludedPaths.Any(excluded =>
            path.StartsWithSegments(excluded, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Construye la clave de caché basada en el usuario autenticado o la dirección IP
    /// </summary>
    private static string BuildCacheKey(HttpContext context)
    {
        // Usar ID de usuario si está autenticado, de lo contrario usar IP
        var userId = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            : null;

        if (!string.IsNullOrEmpty(userId))
            return $"rl_user_{userId}";

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"rl_ip_{ipAddress}";
    }

    /// <summary>
    /// Obtiene o crea una entrada de rate limit para la clave dada
    /// </summary>
    private RateLimitEntry GetOrCreateEntry(string key, TimeSpan window)
    {
        var now = DateTime.UtcNow;

        return _requestCounts.AddOrUpdate(
            key,
            // Crear nueva entrada si no existe
            _ => new RateLimitEntry { Count = 0, WindowStart = now, WindowDuration = window },
            // Actualizar entrada existente, reiniciando si la ventana expiró
            (_, existing) =>
            {
                if (now >= existing.WindowStart.Add(existing.WindowDuration))
                {
                    // La ventana expiró, crear nueva ventana
                    return new RateLimitEntry { Count = 0, WindowStart = now, WindowDuration = window };
                }
                return existing;
            });
    }

    /// <summary>
    /// Incrementa el contador de solicitudes de forma atómica y devuelve el nuevo valor
    /// </summary>
    private static int IncrementCounter(RateLimitEntry entry)
    {
        return Interlocked.Increment(ref entry.Count);
    }

    /// <summary>
    /// Maneja la respuesta cuando se excede el límite de solicitudes (HTTP 429)
    /// </summary>
    private async Task HandleRateLimitExceeded(
        HttpContext context,
        int limit,
        long resetEpoch,
        DateTime resetTime)
    {
        var cacheKey = BuildCacheKey(context);
        _logger.LogWarning(
            "Límite de tasa excedido para {Key}. Límite: {Limit} solicitudes. Reinicio: {Reset}",
            cacheKey, limit, resetTime);

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.ContentType = "application/json";

        AgregarEncabezadosRateLimit(context.Response, limit, 0, resetEpoch);

        var retryAfter = (int)(resetTime - DateTime.UtcNow).TotalSeconds;
        context.Response.Headers.Append("Retry-After", Math.Max(0, retryAfter).ToString());

        var respuesta = new
        {
            error = "Too Many Requests",
            message = "Ha excedido el límite de solicitudes. Intente nuevamente más tarde.",
            retryAfterSeconds = Math.Max(0, retryAfter)
        };

        await context.Response.WriteAsJsonAsync(respuesta);
    }

    /// <summary>
    /// Agrega los encabezados estándar de rate limiting a la respuesta HTTP
    /// </summary>
    private static void AgregarEncabezadosRateLimit(
        HttpResponse response,
        int limit,
        int remaining,
        long resetEpoch)
    {
        response.Headers["X-RateLimit-Limit"] = limit.ToString();
        response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        response.Headers["X-RateLimit-Reset"] = resetEpoch.ToString();
    }
}

/// <summary>
/// Entrada de rate limit que almacena el contador y la información de la ventana de tiempo
/// </summary>
internal class RateLimitEntry
{
    /// <summary>
    /// Contador de solicitudes (acceso atómico mediante Interlocked)
    /// </summary>
    public int Count;

    /// <summary>
    /// Inicio de la ventana de tiempo actual
    /// </summary>
    public DateTime WindowStart { get; set; }

    /// <summary>
    /// Duración de la ventana de tiempo
    /// </summary>
    public TimeSpan WindowDuration { get; set; }
}
