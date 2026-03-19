using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HotelReservationSystem.Data;

namespace HotelReservationSystem.HealthChecks;

/// <summary>
/// Verificación de salud de la base de datos mediante una consulta de conectividad
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly HotelReservationContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(
        HotelReservationContext dbContext,
        ILogger<DatabaseHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta la verificación de conectividad con la base de datos
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verificar conectividad con una consulta ligera al proveedor
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                _logger.LogWarning("La verificación de salud de la base de datos falló: no se puede conectar");
                return new HealthCheckResult(
                    context.Registration.FailureStatus,
                    "No se puede conectar a la base de datos.");
            }

            // Recopilar información del proveedor para el reporte detallado
            var providerName = _dbContext.Database.ProviderName ?? "Desconocido";
            var data = new Dictionary<string, object>
            {
                { "proveedor", providerName },
                { "verificadoEn", DateTime.UtcNow.ToString("o") }
            };

            return HealthCheckResult.Healthy("La base de datos está disponible y respondiendo.", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la verificación de salud de la base de datos");
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "Error al verificar la base de datos.",
                ex);
        }
    }
}
