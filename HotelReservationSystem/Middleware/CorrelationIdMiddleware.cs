using Serilog.Context;

namespace HotelReservationSystem.Middleware;

/// <summary>
/// Middleware que genera o propaga un ID de correlación único por solicitud HTTP.
/// El ID se almacena en el contexto HTTP, se añade a las cabeceras de respuesta
/// y se enriquece en el contexto de Serilog para trazabilidad completa.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Nombre de la cabecera HTTP para el ID de correlación</summary>
    public const string CorrelationIdHeader = "X-Correlation-ID";

    /// <summary>Clave para almacenar el ID en HttpContext.Items</summary>
    public const string CorrelationIdItemKey = "CorrelationId";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Reutilizar el ID de correlación entrante o generar uno nuevo
        var correlationId = ObtenerOGenerarCorrelationId(context);

        // Almacenar en Items para que los servicios puedan acceder sin inyección
        context.Items[CorrelationIdItemKey] = correlationId;

        // Añadir el ID a las cabeceras de respuesta
        AgregarCabeceraRespuesta(context, correlationId);

        // Enriquecer todos los logs de esta solicitud con el ID de correlación
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    /// <summary>
    /// Obtiene el ID de correlación de la cabecera entrante o genera uno nuevo con GUID.
    /// </summary>
    private static string ObtenerOGenerarCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString();
        }

        return Guid.NewGuid().ToString("D");
    }

    /// <summary>
    /// Registra un callback para añadir el ID de correlación a las cabeceras de respuesta
    /// antes de que se envíen al cliente.
    /// </summary>
    private static void AgregarCabeceraRespuesta(HttpContext context, string correlationId)
    {
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
            {
                context.Response.Headers.Append(CorrelationIdHeader, correlationId);
            }
            return Task.CompletedTask;
        });
    }
}
