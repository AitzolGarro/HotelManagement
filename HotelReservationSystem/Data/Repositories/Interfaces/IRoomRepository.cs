using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Data.Repositories.Interfaces;

public interface IRoomRepository : IRepository<Room>
{
    Task<IEnumerable<Room>> GetRoomsByHotelAsync(int hotelId);
    Task<IEnumerable<Room>> GetAllRoomsWithHotelAsync();
    Task<IEnumerable<Room>> GetAvailableRoomsAsync(int hotelId, DateTime checkIn, DateTime checkOut);
    Task<Room?> GetRoomWithReservationsAsync(int roomId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<Room?> GetRoomWithHotelAsync(int roomId);
    Task<Room?> GetRoomByNumberAsync(int hotelId, string roomNumber);
    Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null);
    Task<IEnumerable<Room>> GetRoomsByStatusAsync(RoomStatus status);
    Task<bool> ExistsInHotelAsync(int hotelId, string roomNumber);
    Task<(IEnumerable<Room> Items, int TotalCount)> SearchAsync(RoomSearchCriteria criteria, int pageNumber, int pageSize);
}
