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
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly ILogger<ReservationsController> _logger;

    public ReservationsController(IReservationService reservationService, ILogger<ReservationsController> logger)
    {
        _reservationService = reservationService;
        _logger = logger;
    }

    /// <summary>
    /// Get reservations with optional filtering and pagination
    /// </summary>
    /// <param name="from">Start date for filtering</param>
    /// <param name="to">End date for filtering</param>
    /// <param name="hotelId">Hotel ID for filtering</param>
    /// <param name="status">Reservation status for filtering</param>
    /// <param name="roomId">Room ID for filtering</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <returns>Paged list of reservations</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ReservationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResultDto<ReservationDto>>> GetReservations(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? hotelId,
        [FromQuery] ReservationStatus? status,
        [FromQuery] int? roomId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Getting reservations with filters - From: {From}, To: {To}, HotelId: {HotelId}, Status: {Status}, RoomId: {RoomId}, Page: {PageNumber}, Size: {PageSize}",
            from, to, hotelId, status, roomId, pageNumber, pageSize);

        try
        {
            var pagedResult = await _reservationService.GetPagedReservationsAsync(from, to, hotelId, status, roomId, pageNumber, pageSize);

            // Add pagination metadata to headers
            Response.Headers.Append("X-Pagination-Total-Count", pagedResult.TotalCount.ToString());
            Response.Headers.Append("X-Pagination-Page-Number", pagedResult.PageNumber.ToString());
            Response.Headers.Append("X-Pagination-Page-Size", pagedResult.PageSize.ToString());
            Response.Headers.Append("X-Pagination-Total-Pages", pagedResult.TotalPages.ToString());

            return Ok(pagedResult);
        }
        catch (InvalidDateRangeException ex)
        {
            _logger.LogWarning(ex, "Invalid date range provided");
            return BadRequest(ex.Message);
        }
        catch (PropertyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Hotel or room not found");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservations");
            return StatusCode(500, "An error occurred while retrieving reservations");
        }
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResultDto<ReservationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ReservationDto>>> SearchReservations(
        [FromQuery] ReservationSearchCriteria criteria,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("Searching reservations - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

        try
        {
            var pagedResult = await _reservationService.SearchReservationsAsync(criteria, pageNumber, pageSize);

            // Add pagination metadata to headers
            Response.Headers.Append("X-Pagination-Total-Count", pagedResult.TotalCount.ToString());
            Response.Headers.Append("X-Pagination-Page-Number", pagedResult.PageNumber.ToString());
            Response.Headers.Append("X-Pagination-Page-Size", pagedResult.PageSize.ToString());
            Response.Headers.Append("X-Pagination-Total-Pages", pagedResult.TotalPages.ToString());

            return Ok(pagedResult);
        }
        catch (InvalidDateRangeException ex)
        {
            _logger.LogWarning(ex, "Invalid date range provided");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching reservations");
            return StatusCode(500, "An error occurred while searching reservations");
        }
    }

    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportReservations(
        [FromServices] IExportService exportService,
        [FromQuery] ReservationSearchCriteria criteria,
        [FromQuery] string format = "csv")
    {
        try
        {
            // For export we might want a larger page size or to retrieve all matching records
            var pagedResult = await _reservationService.SearchReservationsAsync(criteria, 1, 10000);
            var data = pagedResult.Items;

            return format.ToLower() switch
            {
                "excel" => File(exportService.ExportToExcel(data, "Reservations"), 
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                                $"Reservations_{DateTime.UtcNow:yyyyMMdd}.xlsx"),
                "pdf" => File(exportService.ExportToPdf(data, "Reservations Report"), 
                              "application/pdf", 
                              $"Reservations_{DateTime.UtcNow:yyyyMMdd}.pdf"),
                _ => File(exportService.ExportToCsv(data), 
                          "text/csv", 
                          $"Reservations_{DateTime.UtcNow:yyyyMMdd}.csv")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting reservations");
            return StatusCode(500, "An error occurred while exporting reservations");
        }
    }

    /// <summary>
    /// Get reservation by ID
    /// </summary>
    /// <param name="id">Reservation ID</param>
    /// <returns>Reservation details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationDto>> GetReservation(int id)
    {
        _logger.LogInformation("Getting reservation with ID {ReservationId}", id);

        try
        {
            var reservation = await _reservationService.GetReservationByIdAsync(id);

            if (reservation == null)
            {
                return NotFound($"Reservation with ID {id} not found");
            }

            return Ok(reservation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reservation {ReservationId}", id);
            return StatusCode(500, "An error occurred while retrieving the reservation");
        }
    }

    /// <summary>
    /// Get reservation by booking reference
    /// </summary>
    /// <param name="bookingReference">Booking reference</param>
    /// <returns>Reservation details</returns>
    [HttpGet("by-reference/{bookingReference}")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationDto>> GetReservationByReference(string bookingReference)
    {
        _logger.LogInformation("Getting reservation with booking reference {BookingReference}", bookingReference);

        try
        {
            var reservation = await _reservationService.GetReservationByBookingReferenceAsync(bookingReference);

            if (reservation == null)
            {
                return NotFound($"Reservation with booking reference {bookingReference} not found");
            }

            return Ok(reservation);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid booking reference");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reservation by reference {BookingReference}", bookingReference);
            return StatusCode(500, "An error occurred while retrieving the reservation");
        }
    }

    /// <summary>
    /// Create a new reservation
    /// </summary>
    /// <param name="request">Reservation creation request</param>
    /// <returns>Created reservation</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationDto>> CreateReservation([FromBody] CreateReservationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Creating reservation for guest {GuestId} in room {RoomId}", request.GuestId, request.RoomId);

        try
        {
            var createdReservation = await _reservationService.CreateReservationAsync(request);
            return CreatedAtAction(nameof(GetReservation), new { id = createdReservation.Id }, createdReservation);
        }
        catch (PropertyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Hotel or room not found");
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid reservation data");
            return BadRequest(ex.Message);
        }
        catch (ReservationConflictException ex)
        {
            _logger.LogWarning(ex, "Reservation conflict detected");
            return Conflict(ex.Message);
        }
        catch (InvalidDateRangeException ex)
        {
            _logger.LogWarning(ex, "Invalid date range");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reservation");
            return StatusCode(500, "An error occurred while creating the reservation");
        }
    }

    /// <summary>
    /// Create a manual reservation with guest data capture
    /// </summary>
    /// <param name="request">Manual reservation creation request with guest information</param>
    /// <returns>Created reservation</returns>
    [HttpPost("manual")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationDto>> CreateManualReservation([FromBody] CreateManualReservationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Creating manual reservation for guest {GuestFirstName} {GuestLastName} in room {RoomId}", 
            request.GuestFirstName, request.GuestLastName, request.RoomId);

        try
        {
            var createdReservation = await _reservationService.CreateManualReservationAsync(request);
            return CreatedAtAction(nameof(GetReservation), new { id = createdReservation.Id }, createdReservation);
        }
        catch (PropertyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Hotel or room not found");
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid reservation data");
            return BadRequest(ex.Message);
        }
        catch (ReservationConflictException ex)
        {
            _logger.LogWarning(ex, "Reservation conflict detected");
            return Conflict(ex.Message);
        }
        catch (InvalidDateRangeException ex)
        {
            _logger.LogWarning(ex, "Invalid date range");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating manual reservation");
            return StatusCode(500, "An error occurred while creating the manual reservation");
        }
    }

    /// <summary>
    /// Update an existing reservation
    /// </summary>
    /// <param name="id">Reservation ID</param>
    /// <param name="request">Reservation update request</param>
    /// <returns>Updated reservation</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationDto>> UpdateReservation(int id, [FromBody] UpdateReservationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Updating reservation {ReservationId}", id);

        try
        {
            var updatedReservation = await _reservationService.UpdateReservationAsync(id, request);
            return Ok(updatedReservation);
        }
        catch (ReservationNotFoundException ex)
        {
            _logger.LogWarning(ex, "Reservation {ReservationId} not found", id);
            return NotFound(ex.Message);
        }
        catch (ReservationConflictException ex)
        {
            _logger.LogWarning(ex, "Reservation conflict detected");
            return Conflict(ex.Message);
        }
        catch (InvalidDateRangeException ex)
        {
            _logger.LogWarning(ex, "Invalid date range");
            return BadRequest(ex.Message);
        }
        catch (InvalidReservationStatusException ex)
        {
            _logger.LogWarning(ex, "Invalid status transition");
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid reservation data");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reservation {ReservationId}", id);
            return StatusCode(500, "An error occurred while updating the reservation");
        }
    }

    /// <summary>
    /// Modify reservation dates (for calendar drag-and-drop)
    /// </summary>
    /// <param name="id">Reservation ID</param>
    /// <param name="request">New dates and optional room</param>
    /// <returns>Updated reservation</returns>
    [HttpPatch("{id}/dates")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationDto>> UpdateReservationDates(int id, [FromBody] UpdateReservationDatesRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Updating dates for reservation {ReservationId}", id);

        try
        {
            var updatedReservation = await _reservationService.UpdateReservationDatesAsync(id, request);
            return Ok(updatedReservation);
        }
        catch (ReservationNotFoundException ex)
        {
            _logger.LogWarning(ex, "Reservation {ReservationId} not found", id);
            return NotFound(ex.Message);
        }
        catch (ReservationConflictException ex)
        {
            _logger.LogWarning(ex, "Reservation conflict detected during drag-and-drop");
            return Conflict(ex.Message);
        }
        catch (InvalidDateRangeException ex)
        {
            _logger.LogWarning(ex, "Invalid date range");
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid reservation data");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dates for reservation {ReservationId}", id);
            return StatusCode(500, "An error occurred while updating the reservation dates");
        }
    }

    /// <summary>
    /// Cancel a reservation
    /// </summary>
    /// <param name="id">Reservation ID</param>
    /// <param name="request">Cancellation request</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CancelReservation(int id, [FromBody] CancelReservationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Cancelling reservation {ReservationId}", id);

        try
        {
            var result = await _reservationService.CancelReservationAsync(id, request);

            if (!result)
            {
                return NotFound($"Reservation with ID {id} not found");
            }

            return NoContent();
        }
        catch (ReservationNotFoundException ex)
        {
            _logger.LogWarning(ex, "Reservation {ReservationId} not found", id);
            return NotFound(ex.Message);
        }
        catch (InvalidReservationStatusException ex)
        {
            _logger.LogWarning(ex, "Invalid cancellation attempt");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling reservation {ReservationId}", id);
            return StatusCode(500, "An error occurred while cancelling the reservation");
        }
    }

    /// <summary>
    /// Check availability for a room and date range
    /// </summary>
    /// <param name="request">Availability check request</param>
    /// <returns>Availability status and conflicts if any</returns>
    [HttpPost("check-availability")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CheckAvailability([FromBody] AvailabilityCheckRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Checking availability for room {RoomId} from {CheckIn} to {CheckOut}",
            request.RoomId, request.CheckInDate, request.CheckOutDate);

        try
        {
            var isAvailable = await _reservationService.CheckAvailabilityAsync(request);
            var conflicts = await _reservationService.DetectConflictsAsync(
                request.RoomId, 
                request.CheckInDate, 
                request.CheckOutDate, 
                request.ExcludeReservationId);

            return Ok(new
            {
                IsAvailable = isAvailable,
                Conflicts = conflicts
            });
        }
        catch (PropertyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Room not found");
            return NotFound(ex.Message);
        }
        catch (InvalidDateRangeException ex)
        {
            _logger.LogWarning(ex, "Invalid date range");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking availability");
            return StatusCode(500, "An error occurred while checking availability");
        }
    }

    /// <summary>
    /// Update reservation status
    /// </summary>
    /// <param name="id">Reservation ID</param>
    /// <param name="request">Status update request</param>
    /// <returns>Updated reservation</returns>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationDto>> UpdateReservationStatus(int id, [FromBody] UpdateReservationStatusRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Updating reservation {ReservationId} status to {Status}", id, request.Status);

        try
        {
            var updatedReservation = await _reservationService.UpdateReservationStatusAsync(id, request.Status);
            return Ok(updatedReservation);
        }
        catch (ReservationNotFoundException ex)
        {
            _logger.LogWarning(ex, "Reservation {ReservationId} not found", id);
            return NotFound(ex.Message);
        }
        catch (InvalidReservationStatusException ex)
        {
            _logger.LogWarning(ex, "Invalid status transition");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reservation {ReservationId} status", id);
            return StatusCode(500, "An error occurred while updating the reservation status");
        }
    }

    /// <summary>
    /// Check in a reservation
    /// </summary>
    /// <param name="id">Reservation ID</param>
    /// <returns>Updated reservation</returns>
    [HttpPost("{id}/checkin")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationDto>> CheckInReservation(int id)
    {
        _logger.LogInformation("Checking in reservation {ReservationId}", id);

        try
        {
            var updatedReservation = await _reservationService.CheckInReservationAsync(id);
            return Ok(updatedReservation);
        }
        catch (ReservationNotFoundException ex)
        {
            _logger.LogWarning(ex, "Reservation {ReservationId} not found", id);
            return NotFound(ex.Message);
        }
        catch (InvalidReservationStatusException ex)
        {
            _logger.LogWarning(ex, "Invalid check-in attempt");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking in reservation {ReservationId}", id);
            return StatusCode(500, "An error occurred while checking in the reservation");
        }
    }

    /// <summary>
    /// Check out a reservation
    /// </summary>
    /// <param name="id">Reservation ID</param>
    /// <returns>Updated reservation</returns>
    [HttpPost("{id}/checkout")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationDto>> CheckOutReservation(int id)
    {
        _logger.LogInformation("Checking out reservation {ReservationId}", id);

        try
        {
            var updatedReservation = await _reservationService.CheckOutReservationAsync(id);
            return Ok(updatedReservation);
        }
        catch (ReservationNotFoundException ex)
        {
            _logger.LogWarning(ex, "Reservation {ReservationId} not found", id);
            return NotFound(ex.Message);
        }
        catch (InvalidReservationStatusException ex)
        {
            _logger.LogWarning(ex, "Invalid check-out attempt");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking out reservation {ReservationId}", id);
            return StatusCode(500, "An error occurred while checking out the reservation");
        }
    }

    /// <summary>
    /// Get today's check-ins
    /// </summary>
    /// <param name="hotelId">Optional hotel ID filter</param>
    /// <returns>List of today's check-ins</returns>
    [HttpGet("checkins/today")]
    [ProducesResponseType(typeof(IEnumerable<ReservationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReservationDto>>> GetTodaysCheckIns([FromQuery] int? hotelId)
    {
        _logger.LogInformation("Getting today's check-ins for hotel {HotelId}", hotelId);

        try
        {
            var checkIns = await _reservationService.GetCheckInsForDateAsync(DateTime.Today, hotelId);
            return Ok(checkIns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting today's check-ins");
            return StatusCode(500, "An error occurred while retrieving today's check-ins");
        }
    }

    /// <summary>
    /// Get today's check-outs
    /// </summary>
    /// <param name="hotelId">Optional hotel ID filter</param>
    /// <returns>List of today's check-outs</returns>
    [HttpGet("checkouts/today")]
    [ProducesResponseType(typeof(IEnumerable<ReservationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReservationDto>>> GetTodaysCheckOuts([FromQuery] int? hotelId)
    {
        _logger.LogInformation("Getting today's check-outs for hotel {HotelId}", hotelId);

        try
        {
            var checkOuts = await _reservationService.GetCheckOutsForDateAsync(DateTime.Today, hotelId);
            return Ok(checkOuts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting today's check-outs");
            return StatusCode(500, "An error occurred while retrieving today's check-outs");
        }
    }

    /// <summary>
    /// Get check-ins for a specific date
    /// </summary>
    /// <param name="date">Date to check</param>
    /// <param name="hotelId">Optional hotel ID filter</param>
    /// <returns>List of check-ins for the date</returns>
    [HttpGet("checkins/{date:datetime}")]
    [ProducesResponseType(typeof(IEnumerable<ReservationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReservationDto>>> GetCheckInsForDate(DateTime date, [FromQuery] int? hotelId)
    {
        _logger.LogInformation("Getting check-ins for date {Date} and hotel {HotelId}", date, hotelId);

        try
        {
            var checkIns = await _reservationService.GetCheckInsForDateAsync(date, hotelId);
            return Ok(checkIns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting check-ins for date {Date}", date);
            return StatusCode(500, "An error occurred while retrieving check-ins");
        }
    }

    /// <summary>
    /// Get check-outs for a specific date
    /// </summary>
    /// <param name="date">Date to check</param>
    /// <param name="hotelId">Optional hotel ID filter</param>
    /// <returns>List of check-outs for the date</returns>
    [HttpGet("checkouts/{date:datetime}")]
    [ProducesResponseType(typeof(IEnumerable<ReservationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReservationDto>>> GetCheckOutsForDate(DateTime date, [FromQuery] int? hotelId)
    {
        _logger.LogInformation("Getting check-outs for date {Date} and hotel {HotelId}", date, hotelId);

        try
        {
            var checkOuts = await _reservationService.GetCheckOutsForDateAsync(date, hotelId);
            return Ok(checkOuts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting check-outs for date {Date}", date);
            return StatusCode(500, "An error occurred while retrieving check-outs");
        }
    }
}