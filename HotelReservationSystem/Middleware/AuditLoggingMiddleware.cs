using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using HotelReservationSystem.Data;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.Middleware;

/// <summary>
/// Middleware para registro automático de auditoría de operaciones de escritura HTTP.
/// Captura información de usuario, IP, duración y cuerpo de solicitud para POST, PUT, DELETE y PATCH.
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    // Tamaño máximo del cuerpo de solicitud a capturar (4KB)
    private const int MaxRequestBodySizeBytes = 4096;

    // Rutas que se deben omitir del registro de auditoría
    private static readonly string[] SkippedPaths = ["/health", "/swagger"];

    // Métodos HTTP que se deben registrar (solo operaciones de escritura)
    private static readonly HashSet<string> LoggedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Delete,
        HttpMethods.Patch
    };

    public AuditLoggingMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;

        // Omitir métodos de solo lectura y rutas excluidas
        if (!LoggedMethods.Contains(method) || ShouldSkipPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var timestamp = DateTime.UtcNow;

        // Continuar con el pipeline de solicitudes
        await _next(context);

        stopwatch.Stop();

        // Para operaciones de escritura, capturar el cuerpo de la solicitud
        var requestBody = await CaptureRequestBodyAsync(context);

        // Guardar el registro de auditoría de forma asíncrona sin bloquear el pipeline
        _ = SaveAuditLogAsync(context, method, requestBody, timestamp, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Determina si la ruta de la solicitud debe omitirse del registro de auditoría
    /// </summary>
    private static bool ShouldSkipPath(PathString path)
    {
        foreach (var skipped in SkippedPaths)
        {
            if (path.StartsWithSegments(skipped, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Captura el cuerpo de la solicitud con límite de tamaño de 4KB
    /// </summary>
    private static async Task<string?> CaptureRequestBodyAsync(HttpContext context)
    {
        try
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: MaxRequestBodySizeBytes,
                leaveOpen: true);

            // Leer hasta el límite máximo de bytes
            var buffer = new char[MaxRequestBodySizeBytes];
            var charsRead = await reader.ReadAsync(buffer, 0, buffer.Length);
            if (context.Request.Body.CanSeek)
                context.Request.Body.Position = 0;

            return charsRead > 0 ? new string(buffer, 0, charsRead) : null;
        }
        catch
        {
            // Si no se puede leer el cuerpo, continuar sin él
            return null;
        }
    }

    /// <summary>
    /// Extrae el ID de usuario desde los claims del JWT
    /// </summary>
    private static string? ExtractUserId(HttpContext context)
    {
        return context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : null;
    }

    /// <summary>
    /// Extrae el nombre de usuario desde los claims del JWT
    /// </summary>
    private static string? ExtractUserName(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return null;

        return context.User.FindFirst(ClaimTypes.Name)?.Value
            ?? context.User.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Obtiene la dirección IP del cliente, considerando proxies con X-Forwarded-For
    /// </summary>
    private static string? GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Tomar la primera IP de la cadena de proxies
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Obtiene el ID de correlación del contexto de la solicitud
    /// </summary>
    private static string? GetCorrelationId(HttpContext context)
    {
        return context.Response.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
    }

    /// <summary>
    /// Guarda el registro de auditoría en la base de datos de forma asíncrona.
    /// Usa IServiceScopeFactory para evitar problemas de tiempo de vida del DbContext.
    /// </summary>
    private async Task SaveAuditLogAsync(
        HttpContext context,
        string method,
        string? requestBody,
        DateTime timestamp,
        long durationMs)
    {
        try
        {
            var entry = new AuditLogEntry
            {
                UserId = ExtractUserId(context),
                UserName = ExtractUserName(context),
                IpAddress = GetClientIpAddress(context),
                HttpMethod = method,
                Path = context.Request.Path.ToString(),
                StatusCode = context.Response.StatusCode,
                RequestBody = requestBody,
                Timestamp = timestamp,
                DurationMs = durationMs,
                CorrelationId = GetCorrelationId(context)
            };

            // Crear un nuevo scope para obtener una instancia fresca del DbContext
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<HotelReservationContext>();
            dbContext.AuditLogs.Add(entry);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Registrar el error pero no interrumpir el pipeline de solicitudes
            _logger.LogWarning(ex, "Error al guardar registro de auditoría para {Method} {Path}",
                method, context.Request.Path);
        }
    }
}
