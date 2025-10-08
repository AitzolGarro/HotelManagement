using HotelReservationSystem.Models;

namespace HotelReservationSystem.Data.Repositories.Interfaces;

public interface IHotelRepository : IRepository<Hotel>
{
    Task<IEnumerable<Hotel>> GetActiveHotelsAsync();
    Task<Hotel?> GetHotelWithRoomsAsync(int hotelId);
    Task<Hotel?> GetHotelWithReservationsAsync(int hotelId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<bool> ExistsAsync(int hotelId);
}