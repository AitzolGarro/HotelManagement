using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HotelReservationSystem.Services;

namespace HotelReservationSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StaticDataController : ControllerBase
    {
        private readonly IStaticDataCacheService _staticDataService;
        private readonly ILogger<StaticDataController> _logger;

        public StaticDataController(
            IStaticDataCacheService staticDataService,
            ILogger<StaticDataController> logger)
        {
            _staticDataService = staticDataService;
            _logger = logger;
        }

        /// <summary>
        /// Get all room types
        /// </summary>
        [HttpGet("room-types")]
        public async Task<ActionResult<IEnumerable<EnumDto>>> GetRoomTypes()
        {
            try
            {
                var roomTypes = await _staticDataService.GetRoomTypesAsync();
                return Ok(roomTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving room types");
                return StatusCode(500, "Internal server error while retrieving room types");
            }
        }

        /// <summary>
        /// Get all room statuses
        /// </summary>
        [HttpGet("room-statuses")]
        public async Task<ActionResult<IEnumerable<EnumDto>>> GetRoomStatuses()
        {
            try
            {
                var roomStatuses = await _staticDataService.GetRoomStatusesAsync();
                return Ok(roomStatuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving room statuses");
                return StatusCode(500, "Internal server error while retrieving room statuses");
            }
        }

        /// <summary>
        /// Get all reservation statuses
        /// </summary>
        [HttpGet("reservation-statuses")]
        public async Task<ActionResult<IEnumerable<EnumDto>>> GetReservationStatuses()
        {
            try
            {
                var reservationStatuses = await _staticDataService.GetReservationStatusesAsync();
                return Ok(reservationStatuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservation statuses");
                return StatusCode(500, "Internal server error while retrieving reservation statuses");
            }
        }

        /// <summary>
        /// Get all reservation sources
        /// </summary>
        [HttpGet("reservation-sources")]
        public async Task<ActionResult<IEnumerable<EnumDto>>> GetReservationSources()
        {
            try
            {
                var reservationSources = await _staticDataService.GetReservationSourcesAsync();
                return Ok(reservationSources);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservation sources");
                return StatusCode(500, "Internal server error while retrieving reservation sources");
            }
        }

        /// <summary>
        /// Get all static data in one call
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult<object>> GetAllStaticData()
        {
            try
            {
                var roomTypes = await _staticDataService.GetRoomTypesAsync();
                var roomStatuses = await _staticDataService.GetRoomStatusesAsync();
                var reservationStatuses = await _staticDataService.GetReservationStatusesAsync();
                var reservationSources = await _staticDataService.GetReservationSourcesAsync();

                var result = new
                {
                    RoomTypes = roomTypes,
                    RoomStatuses = roomStatuses,
                    ReservationStatuses = reservationStatuses,
                    ReservationSources = reservationSources
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all static data");
                return StatusCode(500, "Internal server error while retrieving static data");
            }
        }

        /// <summary>
        /// Invalidate static data cache (Admin only)
        /// </summary>
        [HttpPost("invalidate-cache")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> InvalidateStaticDataCache()
        {
            try
            {
                await _staticDataService.InvalidateStaticDataAsync();
                _logger.LogInformation("Static data cache invalidated by user");
                return Ok(new { message = "Static data cache invalidated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating static data cache");
                return StatusCode(500, "Internal server error while invalidating cache");
            }
        }
    }
}