using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.Data.Repositories;

public class HotelRepository : Repository<Hotel>, IHotelRepository
{
    public HotelRepository(HotelReservationContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Hotel>> GetActiveHotelsAsync()
    {
        return await _dbSet
            .Where(h => h.IsActive)
            .OrderBy(h => h.Name)
            .ToListAsync();
    }

    public async Task<Hotel?> GetHotelWithRoomsAsync(int hotelId)
    {
        return await _dbSet
            .Include(h => h.Rooms)
            .FirstOrDefaultAsync(h => h.Id == hotelId);
    }

    public async Task<Hotel?> GetHotelWithReservationsAsync(int hotelId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _dbSet
            .Include(h => h.Reservations)
                .ThenInclude(r => r.Room)
            .Include(h => h.Reservations)
                .ThenInclude(r => r.Guest)
            .Where(h => h.Id == hotelId);

        if (fromDate.HasValue && toDate.HasValue)
        {
            query = query.Where(h => h.Reservations.Any(r => 
                r.CheckInDate <= toDate.Value && r.CheckOutDate >= fromDate.Value));
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<bool> ExistsAsync(int hotelId)
    {
        return await _dbSet.AnyAsync(h => h.Id == hotelId);
    }
}