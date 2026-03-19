using HotelReservationSystem.Middleware;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services;

/// <summary>
/// Implementación de ICorrelationIdService que lee el ID de correlación
/// desde HttpContext.Items, donde lo almacena el CorrelationIdMiddleware.
/// </summary>
public class CorrelationIdService : ICorrelationIdService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Retorna el ID de correlación de la solicitud actual.
    /// Si no hay contexto HTTP activo (ej. background jobs), genera un ID temporal.
    /// </summary>
    public string GetCorrelationId()
    {
        var context = _httpContextAccessor.HttpContext;

        if (context?.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var id) == true
            && id is string correlationId
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        // Fallback para contextos sin solicitud HTTP (servicios en segundo plano)
        return Guid.NewGuid().ToString("D");
    }
}
