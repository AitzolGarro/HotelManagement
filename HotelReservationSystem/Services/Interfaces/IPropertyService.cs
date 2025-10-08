using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces;

public interface IPropertyService
{
    // Hotel operations
    Task<HotelDto> CreateHotelAsync(CreateHotelRequest request);
    Task<HotelDto?> GetHotelByIdAsync(int id);
    Task<IEnumerable<HotelDto>> GetAllHotelsAsync();
    Task<HotelDto> UpdateHotelAsync(int id, UpdateHotelRequest request);
    Task<bool> DeleteHotelAsync(int id);

    // Room operations
    Task<RoomDto> CreateRoomAsync(CreateRoomRequest request);
    Task<RoomDto?> GetRoomByIdAsync(int id);
    Task<IEnumerable<RoomDto>> GetAllRoomsAsync();
    Task<IEnumerable<RoomDto>> GetRoomsByHotelIdAsync(int hotelId);
    Task<RoomDto> UpdateRoomAsync(int id, UpdateRoomRequest request);
    Task<bool> DeleteRoomAsync(int id);
    Task<IEnumerable<RoomDto>> GetAvailableRoomsAsync(int hotelId, DateTime checkIn, DateTime checkOut);
    Task<bool> SetRoomStatusAsync(int roomId, RoomStatus status);

    // Validation methods
    Task ValidateHotelExistsAsync(int hotelId);
    Task ValidateRoomExistsAsync(int roomId);
    Task ValidateUniqueRoomNumberAsync(int hotelId, string roomNumber, int? excludeRoomId = null);
}