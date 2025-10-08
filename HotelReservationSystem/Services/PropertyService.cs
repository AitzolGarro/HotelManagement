using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Exceptions;

namespace HotelReservationSystem.Services;

public class PropertyService : IPropertyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IPerformanceMonitoringService _performanceMonitoring;
    private readonly ILogger<PropertyService> _logger;

    public PropertyService(
        IUnitOfWork unitOfWork, 
        ICacheService cacheService,
        IPerformanceMonitoringService performanceMonitoring,
        ILogger<PropertyService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _performanceMonitoring = performanceMonitoring;
        _logger = logger;
    }

    // Hotel operations
    public async Task<HotelDto> CreateHotelAsync(CreateHotelRequest request)
    {
        using var timer = _performanceMonitoring.StartTimer("PropertyService.CreateHotel");
        _logger.LogInformation("Creating new hotel: {HotelName}", request.Name);

        var hotel = new Hotel
        {
            Name = request.Name,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Hotels.AddAsync(hotel);
        await _unitOfWork.SaveChangesAsync();

        // Invalidate hotel caches
        await _cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllHotels);

        _logger.LogInformation("Hotel created with ID {HotelId}", hotel.Id);
        return MapToHotelDto(hotel);
    }

    public async Task<HotelDto?> GetHotelByIdAsync(int id)
    {
        using var timer = _performanceMonitoring.StartTimer("PropertyService.GetHotelById");
        var cacheKey = string.Format(CacheKeys.HotelById, id);
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var hotel = await _unitOfWork.Hotels.GetHotelWithRoomsAsync(id);
            return hotel != null ? MapToHotelDto(hotel) : null;
        }, CacheKeys.Expiration.Medium);
    }

    public async Task<IEnumerable<HotelDto>> GetAllHotelsAsync()
    {
        using var timer = _performanceMonitoring.StartTimer("PropertyService.GetAllHotels");
        
        return await _cacheService.GetOrSetAsync(CacheKeys.AllHotels, async () =>
        {
            var hotels = await _unitOfWork.Hotels.GetActiveHotelsAsync();
            return hotels.Select(MapToHotelDto).ToList();
        }, CacheKeys.Expiration.Long);
    }

    public async Task<HotelDto> UpdateHotelAsync(int id, UpdateHotelRequest request)
    {
        using var timer = _performanceMonitoring.StartTimer("PropertyService.UpdateHotel");
        _logger.LogInformation("Updating hotel {HotelId}", id);

        await ValidateHotelExistsAsync(id);

        var hotel = await _unitOfWork.Hotels.GetByIdAsync(id);
        if (hotel == null)
        {
            throw new PropertyNotFoundException($"Hotel with ID {id} not found");
        }

        hotel.Name = request.Name;
        hotel.Address = request.Address;
        hotel.Phone = request.Phone;
        hotel.Email = request.Email;
        hotel.IsActive = request.IsActive;
        hotel.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Hotels.Update(hotel);
        await _unitOfWork.SaveChangesAsync();

        // Invalidate related caches
        await _cacheService.RemoveByPatternAsync(string.Format(CacheKeys.Patterns.HotelSpecific, id));
        await _cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllHotels);

        return MapToHotelDto(hotel);
    }

    public async Task<bool> DeleteHotelAsync(int id)
    {
        using var timer = _performanceMonitoring.StartTimer("PropertyService.DeleteHotel");
        _logger.LogInformation("Deleting hotel {HotelId}", id);

        var hotel = await _unitOfWork.Hotels.GetByIdAsync(id);
        if (hotel == null)
        {
            return false;
        }

        // Check if hotel has active reservations
        var hasActiveReservations = await _unitOfWork.Reservations.HasActiveReservationsForHotelAsync(id);
        if (hasActiveReservations)
        {
            throw new InvalidOperationException("Cannot delete hotel with active reservations");
        }

        hotel.IsActive = false;
        hotel.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Hotels.Update(hotel);
        await _unitOfWork.SaveChangesAsync();

        // Invalidate related caches
        await _cacheService.RemoveByPatternAsync(string.Format(CacheKeys.Patterns.HotelSpecific, id));
        await _cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllHotels);

        return true;
    }

    // Room operations
    public async Task<RoomDto> CreateRoomAsync(CreateRoomRequest request)
    {
        using var timer = _performanceMonitoring.StartTimer("PropertyService.CreateRoom");
        _logger.LogInformation("Creating new room {RoomNumber} for hotel {HotelId}", 
            request.RoomNumber, request.HotelId);

        await ValidateHotelExistsAsync(request.HotelId);
        await ValidateUniqueRoomNumberAsync(request.HotelId, request.RoomNumber);

        var room = new Room
        {
            HotelId = request.HotelId,
            RoomNumber = request.RoomNumber,
            Type = request.Type,
            Capacity = request.Capacity,
            BaseRate = request.BaseRate,
            Status = request.Status,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Rooms.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        // Invalidate room caches
        await _cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllRooms);
        await _cacheService.RemoveByPatternAsync(string.Format(CacheKeys.Patterns.HotelSpecific, request.HotelId));

        _logger.LogInformation("Room created with ID {RoomId}", room.Id);
        
        // Load hotel information for DTO
        var hotel = await _unitOfWork.Hotels.GetByIdAsync(request.HotelId);
        return MapToRoomDto(room, hotel?.Name ?? "");
    }

    public async Task<RoomDto?> GetRoomByIdAsync(int id)
    {
        using var timer = _performanceMonitoring.StartTimer("PropertyService.GetRoomById");
        var cacheKey = string.Format(CacheKeys.RoomById, id);
        
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var room = await _unitOfWork.Rooms.GetRoomWithHotelAsync(id);
            return room != null ? MapToRoomDto(room, room.Hotel?.Name ?? "") : null;
        }, CacheKeys.Expiration.Medium);
    }

    public async Task<IEnumerable<RoomDto>> GetAllRoomsAsync()
    {
        using var timer = _performanceMonitoring.StartTimer("PropertyService.GetAllRooms");
        
        return await _cacheService.GetOrSetAsync("rooms:all", async () =>
        {
            var rooms = await _unitOfWork.Rooms.GetAllRoomsWithHotelAsync();
            return rooms.Select(room => MapToRoomDto(room, room.Hotel?.Name ?? "")).ToList();
        }, CacheKeys.Expiration.Long);
    }

    public async Task<IEnumerable<RoomDto>> GetRoomsByHotelIdAsync(int hotelId)
    {
        using var timer = _performanceMonitoring.StartTimer("PropertyService.GetRoomsByHotel");
        await ValidateHotelExistsAsync(hotelId);
        
        var cacheKey = string.Format(CacheKeys.RoomsByHotel, hotelId);
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var rooms = await _unitOfWork.Rooms.GetRoomsByHotelAsync(hotelId);
            var hotel = await _unitOfWork.Hotels.GetByIdAsync(hotelId);
            
            return rooms.Select(room => MapToRoomDto(room, hotel?.Name ?? "")).ToList();
        }, CacheKeys.Expiration.Medium);
    }

    public async Task<RoomDto> UpdateRoomAsync(int id, UpdateRoomRequest request)
    {
        using var timer = _performanceMonitoring.StartTimer("PropertyService.UpdateRoom");
        _logger.LogInformation("Updating room {RoomId}", id);

        await ValidateRoomExistsAsync(id);

        var room = await _unitOfWork.Rooms.GetByIdAsync(id);
        if (room == null)
        {
            throw new RoomNotFoundException($"Room with ID {id} not found");
        }

        await ValidateUniqueRoomNumberAsync(room.HotelId, request.RoomNumber, id);

        room.RoomNumber = request.RoomNumber;
        room.Type = request.Type;
        room.Capacity = request.Capacity;
        room.BaseRate = request.BaseRate;
        room.Status = request.Status;
        room.Description = request.Description;
        room.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Rooms.Update(room);
        await _unitOfWork.SaveChangesAsync();

        // Invalidate related caches
        await _cacheService.RemoveByPatternAsync(string.Format(CacheKeys.Patterns.RoomSpecific, id));
        await _cacheService.RemoveByPatternAsync(string.Format(CacheKeys.Patterns.HotelSpecific, room.HotelId));
        await _cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllRooms);

        var hotel = await _unitOfWork.Hotels.GetByIdAsync(room.HotelId);
        return MapToRoomDto(room, hotel?.Name ?? "");
    }

    public async Task<bool> DeleteRoomAsync(int id)
    {
        using var timer = _performanceMonitoring.StartTimer("PropertyService.DeleteRoom");
        _logger.LogInformation("Deleting room {RoomId}", id);

        var room = await _unitOfWork.Rooms.GetByIdAsync(id);
        if (room == null)
        {
            return false;
        }

        // Check if room has active reservations
        var hasActiveReservations = await _unitOfWork.Reservations.HasActiveReservationsForRoomAsync(id);
        if (hasActiveReservations)
        {
            throw new InvalidOperationException("Cannot delete room with active reservations");
        }

        _unitOfWork.Rooms.Remove(room);
        await _unitOfWork.SaveChangesAsync();

        // Invalidate related caches
        await _cacheService.RemoveByPatternAsync(string.Format(CacheKeys.Patterns.RoomSpecific, id));
        await _cacheService.RemoveByPatternAsync(string.Format(CacheKeys.Patterns.HotelSpecific, room.HotelId));
        await _cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllRooms);

        return true;
    }

    public async Task<IEnumerable<RoomDto>> GetAvailableRoomsAsync(int hotelId, DateTime checkIn, DateTime checkOut)
    {
        using var timer = _performanceMonitoring.StartTimer("PropertyService.GetAvailableRooms");
        await ValidateHotelExistsAsync(hotelId);

        if (checkIn >= checkOut)
        {
            throw new ArgumentException("Check-in date must be before check-out date");
        }

        if (checkIn.Date < DateTime.Today)
        {
            throw new ArgumentException("Check-in date cannot be in the past");
        }

        var cacheKey = string.Format(CacheKeys.AvailableRooms, hotelId, checkIn.ToString("yyyy-MM-dd"), checkOut.ToString("yyyy-MM-dd"));
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var rooms = await _unitOfWork.Rooms.GetAvailableRoomsAsync(hotelId, checkIn, checkOut);
            var hotel = await _unitOfWork.Hotels.GetByIdAsync(hotelId);
            
            return rooms.Select(room => MapToRoomDto(room, hotel?.Name ?? "")).ToList();
        }, CacheKeys.Expiration.Short); // Short expiration for availability data
    }

    public async Task<bool> SetRoomStatusAsync(int roomId, RoomStatus status)
    {
        using var timer = _performanceMonitoring.StartTimer("PropertyService.SetRoomStatus");
        _logger.LogInformation("Setting room {RoomId} status to {Status}", roomId, status);

        await ValidateRoomExistsAsync(roomId);

        var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
        if (room == null)
        {
            return false;
        }

        // Validate status change
        if (status == RoomStatus.Maintenance || status == RoomStatus.OutOfOrder)
        {
            var hasActiveReservations = await _unitOfWork.Reservations.HasActiveReservationsForRoomAsync(roomId);
            if (hasActiveReservations)
            {
                throw new InvalidRoomStatusException("Cannot set room to maintenance/out of order status with active reservations");
            }
        }

        room.Status = status;
        room.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Rooms.Update(room);

        await _unitOfWork.SaveChangesAsync();

        // Invalidate related caches
        await _cacheService.RemoveByPatternAsync(string.Format(CacheKeys.Patterns.RoomSpecific, roomId));
        await _cacheService.RemoveByPatternAsync(string.Format(CacheKeys.Patterns.HotelSpecific, room.HotelId));
        await _cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllRooms);

        return true;
    }

    // Validation methods
    public async Task ValidateHotelExistsAsync(int hotelId)
    {
        var hotel = await _unitOfWork.Hotels.GetByIdAsync(hotelId);
        if (hotel == null || !hotel.IsActive)
        {
            throw new PropertyNotFoundException($"Active hotel with ID {hotelId} not found");
        }
    }

    public async Task ValidateRoomExistsAsync(int roomId)
    {
        var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
        if (room == null)
        {
            throw new RoomNotFoundException($"Room with ID {roomId} not found");
        }
    }

    public async Task ValidateUniqueRoomNumberAsync(int hotelId, string roomNumber, int? excludeRoomId = null)
    {
        var existingRoom = await _unitOfWork.Rooms.GetRoomByNumberAsync(hotelId, roomNumber);
        if (existingRoom != null && existingRoom.Id != excludeRoomId)
        {
            throw new DuplicateRoomNumberException($"Room number {roomNumber} already exists in this hotel");
        }
    }

    // Mapping methods
    private static HotelDto MapToHotelDto(Hotel hotel)
    {
        return new HotelDto
        {
            Id = hotel.Id,
            Name = hotel.Name,
            Address = hotel.Address,
            Phone = hotel.Phone,
            Email = hotel.Email,
            IsActive = hotel.IsActive,
            CreatedAt = hotel.CreatedAt,
            UpdatedAt = hotel.UpdatedAt,
            Rooms = hotel.Rooms?.Select(room => MapToRoomDto(room, hotel.Name)).ToList() ?? new List<RoomDto>()
        };
    }

    private static RoomDto MapToRoomDto(Room room, string hotelName)
    {
        return new RoomDto
        {
            Id = room.Id,
            HotelId = room.HotelId,
            RoomNumber = room.RoomNumber,
            Type = room.Type,
            Capacity = room.Capacity,
            BaseRate = room.BaseRate,
            Status = room.Status,
            Description = room.Description,
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt,
            HotelName = hotelName
        };
    }
}