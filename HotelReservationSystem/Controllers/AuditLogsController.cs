using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.Data;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.Controllers;

/// <summary>
/// Controlador para consultar registros de auditoría del sistema.
/// Solo accesible para administradores.
/// </summary>
[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly HotelReservationContext _context;
    private readonly ILogger<AuditLogsController> _logger;

    // Tamaño de página máximo permitido para evitar consultas excesivas
    private const int MaxPageSize = 100;

    public AuditLogsController(HotelReservationContext context, ILogger<AuditLogsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene una lista paginada de registros de auditoría con filtros opcionales.
    /// GET /api/audit-logs
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? userId,
        [FromQuery] string? httpMethod,
        [FromQuery] string? path,
        [FromQuery] int? statusCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Validar parámetros de paginación
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > MaxPageSize) pageSize = 20;

        var query = BuildFilteredQuery(startDate, endDate, userId, httpMethod, path, statusCode);

        // Contar total de registros para metadatos de paginación
        var totalCount = await query.CountAsync();

        // Obtener página de resultados ordenados por fecha descendente
        var entries = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => MapToDto(e))
            .ToListAsync();

        return Ok(new
        {
            data = entries,
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        });
    }

    /// <summary>
    /// Obtiene un registro de auditoría específico por su ID.
    /// GET /api/audit-logs/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAuditLog(int id)
    {
        var entry = await _context.AuditLogs
            .Where(e => e.Id == id)
            .Select(e => MapToDto(e))
            .FirstOrDefaultAsync();

        if (entry is null)
        {
            _logger.LogWarning("Registro de auditoría con ID {Id} no encontrado", id);
            return NotFound(new { message = $"Registro de auditoría con ID {id} no encontrado" });
        }

        return Ok(entry);
    }

    /// <summary>
    /// Construye la consulta con los filtros aplicados
    /// </summary>
    private IQueryable<AuditLogEntry> BuildFilteredQuery(
        DateTime? startDate,
        DateTime? endDate,
        string? userId,
        string? httpMethod,
        string? path,
        int? statusCode)
    {
        var query = _context.AuditLogs.AsQueryable();

        // Filtrar por rango de fechas
        if (startDate.HasValue)
            query = query.Where(e => e.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.Timestamp <= endDate.Value);

        // Filtrar por usuario
        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(e => e.UserId == userId);

        // Filtrar por método HTTP (insensible a mayúsculas)
        if (!string.IsNullOrWhiteSpace(httpMethod))
            query = query.Where(e => e.HttpMethod.ToUpper() == httpMethod.ToUpper());

        // Filtrar por ruta (búsqueda parcial)
        if (!string.IsNullOrWhiteSpace(path))
            query = query.Where(e => e.Path.Contains(path));

        // Filtrar por código de estado HTTP
        if (statusCode.HasValue)
            query = query.Where(e => e.StatusCode == statusCode.Value);

        return query;
    }

    /// <summary>
    /// Proyecta una entidad AuditLogEntry a un DTO anónimo para la respuesta
    /// </summary>
    private static object MapToDto(AuditLogEntry e) => new
    {
        e.Id,
        e.UserId,
        e.UserName,
        e.IpAddress,
        e.HttpMethod,
        e.Path,
        e.StatusCode,
        e.Timestamp,
        e.DurationMs,
        e.CorrelationId,
        // Omitir cuerpos de solicitud/respuesta en listados para reducir tamaño de respuesta
        hasRequestBody = e.RequestBody != null
    };
}
