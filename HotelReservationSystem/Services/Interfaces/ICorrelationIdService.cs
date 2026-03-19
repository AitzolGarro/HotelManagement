namespace HotelReservationSystem.Services.Interfaces;

/// <summary>
/// Servicio para acceder al ID de correlación de la solicitud actual.
/// Permite propagar el ID a llamadas externas (Booking.com, etc.) para trazabilidad.
/// </summary>
public interface ICorrelationIdService
{
    /// <summary>Obtiene el ID de correlación de la solicitud HTTP actual.</summary>
    string GetCorrelationId();
}
