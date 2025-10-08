using Microsoft.AspNetCore.Mvc;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Exceptions;

namespace HotelReservationSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly IPropertyService _propertyService;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(IPropertyService propertyService, ILogger<RoomsController> logger)
    {
        _propertyService = propertyService;
        _logger = logger;
    }

    /// <summary>
    /// Get all rooms
    /// </summary>
    /// <returns>List of all rooms</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RoomDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetAllRooms()
    {
        _logger.LogInformation("Getting all rooms");
        
        try
        {
            var rooms = await _propertyService.GetAllRoomsAsync();
            return Ok(rooms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all rooms");
            return StatusCode(500, "An error occurred while retrieving rooms");
        }
    }

    /// <summary>
    /// Get room by ID
    /// </summary>
    /// <param name="id">Room ID</param>
    /// <returns>Room details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomDto>> GetRoom(int id)
    {
        _logger.LogInformation("Getting room with ID {RoomId}", id);
        
        try
        {
            var room = await _propertyService.GetRoomByIdAsync(id);
            
            if (room == null)
            {
                return NotFound($"Room with ID {id} not found");
            }

            return Ok(room);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room {RoomId}", id);
            return StatusCode(500, "An error occurred while retrieving the room");
        }
    }

    /// <summary>
    /// Update an existing room
    /// </summary>
    /// <param name="id">Room ID</param>
    /// <param name="request">Room update request</param>
    /// <returns>Updated room</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoomDto>> UpdateRoom(int id, [FromBody] UpdateRoomRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Updating room {RoomId}", id);
        
        try
        {
            var updatedRoom = await _propertyService.UpdateRoomAsync(id, request);
            return Ok(updatedRoom);
        }
        catch (RoomNotFoundException ex)
        {
            _logger.LogWarning(ex, "Room {RoomId} not found for update", id);
            return NotFound(ex.Message);
        }
        catch (DuplicateRoomNumberException ex)
        {
            _logger.LogWarning(ex, "Duplicate room number {RoomNumber} for room {RoomId}", request.RoomNumber, id);
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room {RoomId}", id);
            return StatusCode(500, "An error occurred while updating the room");
        }
    }

    /// <summary>
    /// Delete a room
    /// </summary>
    /// <param name="id">Room ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> DeleteRoom(int id)
    {
        _logger.LogInformation("Deleting room {RoomId}", id);
        
        try
        {
            var result = await _propertyService.DeleteRoomAsync(id);
            
            if (!result)
            {
                return NotFound($"Room with ID {id} not found");
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete room {RoomId} - has active reservations", id);
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting room {RoomId}", id);
            return StatusCode(500, "An error occurred while deleting the room");
        }
    }

    /// <summary>
    /// Update room status
    /// </summary>
    /// <param name="id">Room ID</param>
    /// <param name="request">Room status update request</param>
    /// <returns>Success status</returns>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> UpdateRoomStatus(int id, [FromBody] UpdateRoomStatusRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Updating room {RoomId} status to {Status}", id, request.Status);
        
        try
        {
            var result = await _propertyService.SetRoomStatusAsync(id, request.Status);
            
            if (!result)
            {
                return NotFound($"Room with ID {id} not found");
            }

            return Ok(new { message = "Room status updated successfully" });
        }
        catch (RoomNotFoundException ex)
        {
            _logger.LogWarning(ex, "Room {RoomId} not found", id);
            return NotFound(ex.Message);
        }
        catch (InvalidRoomStatusException ex)
        {
            _logger.LogWarning(ex, "Invalid room status change for room {RoomId}", id);
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room {RoomId} status", id);
            return StatusCode(500, "An error occurred while updating the room status");
        }
    }
}