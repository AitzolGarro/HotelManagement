namespace HotelReservationSystem.Models;

/// <summary>
/// Entidad para registrar entradas de auditoría de operaciones HTTP en el sistema.
/// Captura información sobre quién realizó qué operación, cuándo y con qué resultado.
/// </summary>
public class AuditLogEntry
{
    /// <summary>Identificador único del registro de auditoría</summary>
    public int Id { get; set; }

    /// <summary>Identificador del usuario autenticado (extraído del JWT)</summary>
    public string? UserId { get; set; }

    /// <summary>Nombre de usuario del usuario autenticado (extraído del JWT)</summary>
    public string? UserName { get; set; }

    /// <summary>Dirección IP del cliente que realizó la solicitud</summary>
    public string? IpAddress { get; set; }

    /// <summary>Método HTTP de la solicitud (GET, POST, PUT, DELETE, PATCH)</summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>Ruta de la solicitud HTTP</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Código de estado HTTP de la respuesta</summary>
    public int StatusCode { get; set; }

    /// <summary>Cuerpo de la solicitud (limitado a 4KB para operaciones de escritura)</summary>
    public string? RequestBody { get; set; }

    /// <summary>Cuerpo de la respuesta (opcional)</summary>
    public string? ResponseBody { get; set; }

    /// <summary>Marca de tiempo UTC cuando se procesó la solicitud</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Duración de la solicitud en milisegundos</summary>
    public long DurationMs { get; set; }

    /// <summary>Identificador de correlación para rastreo de solicitudes</summary>
    public string? CorrelationId { get; set; }
}
