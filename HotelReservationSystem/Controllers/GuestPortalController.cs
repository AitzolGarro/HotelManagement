using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Models.DTOs.GuestPortal;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Controllers;

// ─── MVC Controller (Razor Views) ────────────────────────────────────────────

[Route("GuestPortal")]
public class GuestPortalViewController : Controller
{
    private readonly IGuestPortalService _portalService;
    private readonly ILogger<GuestPortalViewController> _logger;

    public GuestPortalViewController(IGuestPortalService portalService, ILogger<GuestPortalViewController> logger)
    {
        _portalService = portalService;
        _logger = logger;
    }

    // GET /GuestPortal/Login
    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new GuestLoginViewModel());
    }

    // GET /GuestPortal/Dashboard
    [HttpGet("Dashboard")]
    [HttpGet("")]
    [AllowAnonymous] // Auth handled client-side via JWT stored in localStorage
    public IActionResult Dashboard()
    {
        return View();
    }

    // GET /GuestPortal/Reservations
    [HttpGet("Reservations")]
    [AllowAnonymous]
    public IActionResult Reservations()
    {
        return View();
    }

    // GET /GuestPortal/Reservation/{id}
    [HttpGet("Reservation/{id:int}")]
    [AllowAnonymous]
    public IActionResult ReservationDetail(int id)
    {
        ViewData["ReservationId"] = id;
        return View();
    }

    // GET /GuestPortal/Profile
    [HttpGet("Profile")]
    [AllowAnonymous]
    public IActionResult Profile()
    {
        return View();
    }

    // GET /GuestPortal/Logout
    [HttpGet("Logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        return View();
    }
}

// ─── API Controller ───────────────────────────────────────────────────────────

[ApiController]
[Route("api/guest-portal")]
public class GuestPortalController : ControllerBase
{
    private readonly IGuestPortalService _portalService;
    private readonly ILogger<GuestPortalController> _logger;

    public GuestPortalController(IGuestPortalService portalService, ILogger<GuestPortalController> logger)
    {
        _portalService = portalService;
        _logger = logger;
    }

    // ─── Authentication ───────────────────────────────────────────────────────

    /// <summary>POST /api/guest-portal/login – authenticate with email + booking reference</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] GuestLoginRequest request)
    {
        try
        {
            var response = await _portalService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during guest login for {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during login." });
        }
    }

    // ─── Profile ──────────────────────────────────────────────────────────────

    /// <summary>GET /api/guest-portal/me – get current guest profile</summary>
    [HttpGet("me")]
    [Authorize(Roles = "Guest")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var guestId = GetGuestId();
            var profile = await _portalService.GetGuestProfileAsync(guestId);
            return Ok(profile);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting guest profile");
            return StatusCode(500, new { message = "An error occurred." });
        }
    }

    /// <summary>PUT /api/guest-portal/me – update guest profile</summary>
    [HttpPut("me")]
    [Authorize(Roles = "Guest")]
    public async Task<IActionResult> UpdateProfile([FromBody] GuestProfileDto request)
    {
        try
        {
            var guestId = GetGuestId();
            var profile = await _portalService.UpdateGuestProfileAsync(guestId, request);
            return Ok(profile);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating guest profile");
            return StatusCode(500, new { message = "An error occurred." });
        }
    }

    /// <summary>GET /api/guest-portal/me/notification-preferences</summary>
    [HttpGet("me/notification-preferences")]
    [Authorize(Roles = "Guest")]
    public async Task<IActionResult> GetNotificationPreferences()
    {
        try
        {
            var guestId = GetGuestId();
            var prefs = await _portalService.GetNotificationPreferencesAsync(guestId);
            return Ok(prefs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification preferences");
            return StatusCode(500, new { message = "An error occurred." });
        }
    }

    /// <summary>PUT /api/guest-portal/me/notification-preferences</summary>
    [HttpPut("me/notification-preferences")]
    [Authorize(Roles = "Guest")]
    public async Task<IActionResult> UpdateNotificationPreferences([FromBody] GuestNotificationPreferencesDto request)
    {
        try
        {
            var guestId = GetGuestId();
            var prefs = await _portalService.UpdateNotificationPreferencesAsync(guestId, request);
            return Ok(prefs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences");
            return StatusCode(500, new { message = "An error occurred." });
        }
    }

    // ─── Reservations ─────────────────────────────────────────────────────────

    /// <summary>GET /api/guest-portal/reservations – list all guest reservations</summary>
    [HttpGet("reservations")]
    [Authorize(Roles = "Guest")]
    public async Task<IActionResult> GetReservations()
    {
        try
        {
            var guestId = GetGuestId();
            var reservations = await _portalService.GetMyReservationsAsync(guestId);
            return Ok(reservations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting guest reservations");
            return StatusCode(500, new { message = "An error occurred." });
        }
    }

    /// <summary>GET /api/guest-portal/reservations/{id} – get single reservation</summary>
    [HttpGet("reservations/{id:int}")]
    [Authorize(Roles = "Guest")]
    public async Task<IActionResult> GetReservation(int id)
    {
        try
        {
            var guestId = GetGuestId();
            var reservation = await _portalService.GetMyReservationByIdAsync(guestId, id);
            if (reservation == null)
                return NotFound(new { message = "Reservation not found." });
            return Ok(reservation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reservation {Id}", id);
            return StatusCode(500, new { message = "An error occurred." });
        }
    }

    /// <summary>PATCH /api/guest-portal/reservations/{id}/dates – modify reservation dates</summary>
    [HttpPatch("reservations/{id:int}/dates")]
    [Authorize(Roles = "Guest")]
    public async Task<IActionResult> ModifyReservation(int id, [FromBody] UpdateReservationDatesRequest request)
    {
        try
        {
            var guestId = GetGuestId();
            var result = await _portalService.ModifyReservationAsync(guestId, id, request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error modifying reservation {Id}", id);
            return StatusCode(500, new { message = "An error occurred." });
        }
    }

    /// <summary>POST /api/guest-portal/reservations/{id}/cancel – cancel reservation</summary>
    [HttpPost("reservations/{id:int}/cancel")]
    [Authorize(Roles = "Guest")]
    public async Task<IActionResult> CancelReservation(int id, [FromBody] CancelReservationRequest request)
    {
        try
        {
            var guestId = GetGuestId();
            await _portalService.CancelReservationAsync(guestId, id, request.Reason);
            return Ok(new { message = "Reservation cancelled successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling reservation {Id}", id);
            return StatusCode(500, new { message = "An error occurred." });
        }
    }

    /// <summary>POST /api/guest-portal/reservations/{id}/special-requests – submit special requests</summary>
    [HttpPost("reservations/{id:int}/special-requests")]
    [Authorize(Roles = "Guest")]
    public async Task<IActionResult> SubmitSpecialRequest(int id, [FromBody] GuestSpecialRequestDto request)
    {
        try
        {
            var guestId = GetGuestId();
            var result = await _portalService.SubmitSpecialRequestAsync(guestId, id, request.SpecialRequests);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting special request for reservation {Id}", id);
            return StatusCode(500, new { message = "An error occurred." });
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private int GetGuestId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(value) || !int.TryParse(value, out var id))
            throw new UnauthorizedAccessException("Invalid guest token.");
        return id;
    }
}
