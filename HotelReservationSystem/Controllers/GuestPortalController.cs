using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Controllers;

[ApiController]
[Route("api/guest-portal")]
public class GuestPortalController : ControllerBase
{
    private readonly IGuestPortalService _portalService;

    public GuestPortalController(IGuestPortalService portalService)
    {
        _portalService = portalService;
    }

    [HttpPost("login")]
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
    }

    [HttpGet("me")]
    [Authorize(Roles = "Guest")]
    public async Task<IActionResult> GetProfile()
    {
        var guestId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var profile = await _portalService.GetGuestProfileAsync(guestId);
        return Ok(profile);
    }

    [HttpPut("me")]
    [Authorize(Roles = "Guest")]
    public async Task<IActionResult> UpdateProfile([FromBody] GuestProfileDto request)
    {
        var guestId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var profile = await _portalService.UpdateGuestProfileAsync(guestId, request);
        return Ok(profile);
    }

    [HttpGet("reservations")]
    [Authorize(Roles = "Guest")]
    public async Task<IActionResult> GetReservations()
    {
        var guestId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var reservations = await _portalService.GetMyReservationsAsync(guestId);
        return Ok(reservations);
    }

    [HttpPatch("reservations/{id}/dates")]
    [Authorize(Roles = "Guest")]
    public async Task<IActionResult> ModifyReservation(int id, [FromBody] UpdateReservationDatesRequest request)
    {
        try
        {
            var guestId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _portalService.ModifyReservationAsync(guestId, id, request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("reservations/{id}/cancel")]
    [Authorize(Roles = "Guest")]
    public async Task<IActionResult> CancelReservation(int id, [FromBody] CancelReservationRequest request)
    {
        try
        {
            var guestId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _portalService.CancelReservationAsync(guestId, id, request.Reason);
            return Ok(new { message = "Reservation cancelled successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}