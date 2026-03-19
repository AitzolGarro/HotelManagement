using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GuestsController : ControllerBase
{
    private readonly IGuestManagementService _guestService;
    private readonly ILogger<GuestsController> _logger;

    // Constructor con inyección de dependencias del servicio de huéspedes y logger
    public GuestsController(IGuestManagementService guestService, ILogger<GuestsController> logger)
    {
        _guestService = guestService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateGuest([FromBody] CreateGuestRequest request)
    {
        _logger.LogInformation("Creando nuevo huésped: {FirstName} {LastName}", request.FirstName, request.LastName);

        try
        {
            var guest = await _guestService.CreateGuestAsync(request);
            return Ok(guest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear huésped");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGuest(int id)
    {
        _logger.LogInformation("Obteniendo huésped con ID {GuestId}", id);

        try
        {
            var guest = await _guestService.GetGuestByIdAsync(id);
            if (guest == null) return NotFound();
            return Ok(guest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener huésped {GuestId}", id);
            return StatusCode(500, "Ocurrió un error al obtener el huésped");
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchGuests([FromQuery] GuestSearchCriteria criteria, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Buscando huéspedes - Página: {PageNumber}, Tamaño: {PageSize}", pageNumber, pageSize);

        try
        {
            var result = await _guestService.SearchGuestsAsync(criteria, pageNumber, pageSize);

            // Agregar metadatos de paginación a los encabezados de respuesta
            Response.Headers.Append("X-Pagination-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Pagination-Page-Number", result.PageNumber.ToString());
            Response.Headers.Append("X-Pagination-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Pagination-Total-Pages", result.TotalPages.ToString());

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar huéspedes");
            return StatusCode(500, "Ocurrió un error al buscar huéspedes");
        }
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetGuestHistory(int id)
    {
        _logger.LogInformation("Obteniendo historial de reservaciones del huésped {GuestId}", id);

        try
        {
            var history = await _guestService.GetGuestHistoryAsync(id);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener historial del huésped {GuestId}", id);
            return StatusCode(500, "Ocurrió un error al obtener el historial del huésped");
        }
    }

    [HttpGet("{id}/statistics")]
    public async Task<IActionResult> GetGuestStatistics(int id)
    {
        _logger.LogInformation("Obteniendo estadísticas del huésped {GuestId}", id);

        try
        {
            var stats = await _guestService.GetGuestStatisticsAsync(id);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas del huésped {GuestId}", id);
            return StatusCode(500, "Ocurrió un error al obtener las estadísticas del huésped");
        }
    }

    [HttpGet("{id}/preferences")]
    public async Task<IActionResult> GetPreferences(int id)
    {
        _logger.LogInformation("Obteniendo preferencias del huésped {GuestId}", id);

        try
        {
            var prefs = await _guestService.GetGuestPreferencesAsync(id);
            return Ok(prefs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener preferencias del huésped {GuestId}", id);
            return StatusCode(500, "Ocurrió un error al obtener las preferencias del huésped");
        }
    }

    [HttpPost("{id}/preferences")]
    public async Task<IActionResult> AddPreference(int id, [FromBody] PreferenceRequest request)
    {
        _logger.LogInformation("Agregando preferencia al huésped {GuestId}, categoría: {Category}", id, request.Category);

        try
        {
            var pref = await _guestService.AddGuestPreferenceAsync(id, request.Category, request.Preference);
            return Ok(pref);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar preferencia al huésped {GuestId}", id);
            return StatusCode(500, "Ocurrió un error al agregar la preferencia del huésped");
        }
    }

    [HttpGet("{id}/notes")]
    public async Task<IActionResult> GetNotes(int id)
    {
        _logger.LogInformation("Obteniendo notas del huésped {GuestId}", id);

        try
        {
            var notes = await _guestService.GetGuestNotesAsync(id);
            return Ok(notes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener notas del huésped {GuestId}", id);
            return StatusCode(500, "Ocurrió un error al obtener las notas del huésped");
        }
    }

    [HttpPost("{id}/notes")]
    public async Task<IActionResult> AddNote(int id, [FromBody] NoteRequest request)
    {
        // Obtener el ID del usuario actual desde los claims del token JWT
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int userId = int.TryParse(userIdStr, out var parsed) ? parsed : 1;

        _logger.LogInformation("Agregando nota al huésped {GuestId} por usuario {UserId}", id, userId);

        try
        {
            var note = await _guestService.AddGuestNoteAsync(id, userId, request.Note);
            return Ok(note);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar nota al huésped {GuestId}", id);
            return StatusCode(500, "Ocurrió un error al agregar la nota del huésped");
        }
    }
}

public class PreferenceRequest
{
    public string Category { get; set; } = string.Empty;
    public string Preference { get; set; } = string.Empty;
}

public class NoteRequest
{
    public string Note { get; set; } = string.Empty;
}
