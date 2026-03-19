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

    public GuestsController(IGuestManagementService guestService)
    {
        _guestService = guestService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateGuest([FromBody] CreateGuestRequest request)
    {
        try
        {
            var guest = await _guestService.CreateGuestAsync(request);
            return Ok(guest);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGuest(int id)
    {
        var guest = await _guestService.GetGuestByIdAsync(id);
        if (guest == null) return NotFound();
        return Ok(guest);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchGuests([FromQuery] GuestSearchCriteria criteria, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _guestService.SearchGuestsAsync(criteria, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetGuestHistory(int id)
    {
        var history = await _guestService.GetGuestHistoryAsync(id);
        return Ok(history);
    }

    [HttpGet("{id}/statistics")]
    public async Task<IActionResult> GetGuestStatistics(int id)
    {
        var stats = await _guestService.GetGuestStatisticsAsync(id);
        return Ok(stats);
    }

    [HttpGet("{id}/preferences")]
    public async Task<IActionResult> GetPreferences(int id)
    {
        var prefs = await _guestService.GetGuestPreferencesAsync(id);
        return Ok(prefs);
    }

    [HttpPost("{id}/preferences")]
    public async Task<IActionResult> AddPreference(int id, [FromBody] PreferenceRequest request)
    {
        var pref = await _guestService.AddGuestPreferenceAsync(id, request.Category, request.Preference);
        return Ok(pref);
    }

    [HttpGet("{id}/notes")]
    public async Task<IActionResult> GetNotes(int id)
    {
        var notes = await _guestService.GetGuestNotesAsync(id);
        return Ok(notes);
    }

    [HttpPost("{id}/notes")]
    public async Task<IActionResult> AddNote(int id, [FromBody] NoteRequest request)
    {
        // Assuming we have some way to get the current user's ID. Let's hardcode 1 for demo purposes if not available.
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int userId = int.TryParse(userIdStr, out var parsed) ? parsed : 1;

        var note = await _guestService.AddGuestNoteAsync(id, userId, request.Note);
        return Ok(note);
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