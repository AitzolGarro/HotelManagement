using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Exceptions;
using HotelReservationSystem.Authorization;

namespace HotelReservationSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HotelsController : ControllerBase
{
    private readonly IPropertyService _propertyService;
    private readonly ILogger<HotelsController> _logger;

    public HotelsController(IPropertyService propertyService, ILogger<HotelsController> logger)
    {
        _propertyService = propertyService;
        _logger = logger;
    }

    /// <summary>
    /// Get all hotels
    /// </summary>
    /// <returns>List of hotels</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<HotelDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<HotelDto>>> GetHotels()
    {
        _logger.LogInformation("Getting all hotels");
        var hotels = await _propertyService.GetAllHotelsAsync();
        return Ok(hotels);
    }

    /// <summary>
    /// Get hotel by ID
    /// </summary>
    /// <param name="id">Hotel ID</param>
    /// <returns>Hotel details</returns>
    [HttpGet("{id}")]
    [RequireHotelAccess("id")]
    [ProducesResponseType(typeof(HotelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HotelDto>> GetHotel(int id)
    {
        _logger.LogInformation("Getting hotel with ID {HotelId}", id);
        
        try
        {
            var hotel = await _propertyService.GetHotelByIdAsync(id);
            
            if (hotel == null)
            {
                return NotFound($"Hotel with ID {id} not found");
            }

            return Ok(hotel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel {HotelId}", id);
            return StatusCode(500, "An error occurred while retrieving the hotel");
        }
    }

    /// <summary>
    /// Create a new hotel
    /// </summary>
    /// <param name="request">Hotel creation request</param>
    /// <returns>Created hotel</returns>
    [HttpPost]
    [RequireRole(UserRole.Admin)]
    [ProducesResponseType(typeof(HotelDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HotelDto>> CreateHotel([FromBody] CreateHotelRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Creating new hotel: {HotelName}", request.Name);
        
        try
        {
            var createdHotel = await _propertyService.CreateHotelAsync(request);
            return CreatedAtAction(nameof(GetHotel), new { id = createdHotel.Id }, createdHotel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating hotel {HotelName}", request.Name);
            return StatusCode(500, "An error occurred while creating the hotel");
        }
    }

    /// <summary>
    /// Update an existing hotel
    /// </summary>
    /// <param name="id">Hotel ID</param>
    /// <param name="request">Hotel update request</param>
    /// <returns>Updated hotel</returns>
    [HttpPut("{id}")]
    [RequireRole(UserRole.Admin, UserRole.Manager)]
    [RequireHotelAccess("id")]
    [ProducesResponseType(typeof(HotelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HotelDto>> UpdateHotel(int id, [FromBody] UpdateHotelRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Updating hotel {HotelId}", id);
        
        try
        {
            var updatedHotel = await _propertyService.UpdateHotelAsync(id, request);
            return Ok(updatedHotel);
        }
        catch (PropertyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Hotel {HotelId} not found for update", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hotel {HotelId}", id);
            return StatusCode(500, "An error occurred while updating the hotel");
        }
    }

    /// <summary>
    /// Delete a hotel (soft delete - sets IsActive to false)
    /// </summary>
    /// <param name="id">Hotel ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [RequireRole(UserRole.Admin)]
    [RequireHotelAccess("id")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> DeleteHotel(int id)
    {
        _logger.LogInformation("Deleting hotel {HotelId}", id);
        
        try
        {
            var result = await _propertyService.DeleteHotelAsync(id);
            
            if (!result)
            {
                return NotFound($"Hotel with ID {id} not found");
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete hotel {HotelId} - has active reservations", id);
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hotel {HotelId}", id);
            return StatusCode(500, "An error occurred while deleting the hotel");
        }
    }

    // Room management endpoints within hotels

    /// <summary>
    /// Get all rooms for a specific hotel
    /// </summary>
    /// <param name="id">Hotel ID</param>
    /// <returns>List of rooms</returns>
    [HttpGet("{id}/rooms")]
    [RequireHotelAccess("id")]
    [ProducesResponseType(typeof(IEnumerable<RoomDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetHotelRooms(int id)
    {
        _logger.LogInformation("Getting rooms for hotel {HotelId}", id);
        
        try
        {
            var rooms = await _propertyService.GetRoomsByHotelIdAsync(id);
            return Ok(rooms);
        }
        catch (PropertyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Hotel {HotelId} not found", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rooms for hotel {HotelId}", id);
            return StatusCode(500, "An error occurred while retrieving hotel rooms");
        }
    }

    /// <summary>
    /// Create a new room for a hotel
    /// </summary>
    /// <param name="id">Hotel ID</param>
    /// <param name="request">Room creation request</param>
    /// <returns>Created room</returns>
    [HttpPost("{id}/rooms")]
    [RequireRole(UserRole.Admin, UserRole.Manager)]
    [RequireHotelAccess("id")]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoomDto>> CreateHotelRoom(int id, [FromBody] CreateRoomRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Ensure the hotel ID in the URL matches the request
        if (request.HotelId != id)
        {
            request.HotelId = id;
        }

        _logger.LogInformation("Creating room {RoomNumber} for hotel {HotelId}", request.RoomNumber, id);
        
        try
        {
            var createdRoom = await _propertyService.CreateRoomAsync(request);
            return CreatedAtAction("GetRoom", "Rooms", new { id = createdRoom.Id }, createdRoom);
        }
        catch (PropertyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Hotel {HotelId} not found", id);
            return NotFound(ex.Message);
        }
        catch (DuplicateRoomNumberException ex)
        {
            _logger.LogWarning(ex, "Duplicate room number {RoomNumber} for hotel {HotelId}", request.RoomNumber, id);
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room for hotel {HotelId}", id);
            return StatusCode(500, "An error occurred while creating the room");
        }
    }

    /// <summary>
    /// Get available rooms for a hotel within a date range
    /// </summary>
    /// <param name="id">Hotel ID</param>
    /// <param name="checkIn">Check-in date</param>
    /// <param name="checkOut">Check-out date</param>
    /// <returns>List of available rooms</returns>
    [HttpGet("{id}/rooms/available")]
    [RequireHotelAccess("id")]
    [ProducesResponseType(typeof(IEnumerable<RoomDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetAvailableRooms(
        int id, 
        [FromQuery] DateTime checkIn, 
        [FromQuery] DateTime checkOut)
    {
        _logger.LogInformation("Getting available rooms for hotel {HotelId} from {CheckIn} to {CheckOut}", 
            id, checkIn, checkOut);
        
        try
        {
            var availableRooms = await _propertyService.GetAvailableRoomsAsync(id, checkIn, checkOut);
            return Ok(availableRooms);
        }
        catch (PropertyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Hotel {HotelId} not found", id);
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid date range for availability check");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available rooms for hotel {HotelId}", id);
            return StatusCode(500, "An error occurred while retrieving available rooms");
        }
    }
}