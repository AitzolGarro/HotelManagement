namespace HotelReservationSystem.Configuration;

/// <summary>
/// Configuración para el middleware de limitación de tasa de solicitudes
/// </summary>
public class RateLimitSettings
{
    /// <summary>
    /// Número máximo de solicitudes permitidas por ventana de tiempo
    /// </summary>
    public int RequestsPerWindow { get; set; } = 300;

    /// <summary>
    /// Número máximo de solicitudes GET/HEAD permitidas por ventana.
    /// Debe ser más permisivo para no romper la navegación normal.
    /// </summary>
    public int ReadRequestsPerWindow { get; set; } = 1200;

    /// <summary>
    /// Número máximo de solicitudes de escritura (POST/PUT/PATCH/DELETE) por ventana.
    /// </summary>
    public int WriteRequestsPerWindow { get; set; } = 180;

    /// <summary>
    /// Duración de la ventana de tiempo en segundos
    /// </summary>
    public int WindowSizeSeconds { get; set; } = 60;

    /// <summary>
    /// Indica si el rate limiting está habilitado
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Lista de rutas excluidas del rate limiting (ej: /health, /swagger)
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new() { "/health", "/swagger", "/favicon.ico", "/css", "/js", "/lib", "/images", "/uploads", "/api/i18n" };
}
