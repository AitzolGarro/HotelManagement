using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.Data.Repositories;

public class RoomRepository : Repository<Room>, IRoomRepository
{
    public RoomRepository(HotelReservationContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Room>> GetRoomsByHotelAsync(int hotelId)
    {
        return await _dbSet
            .Where(r => r.HotelId == hotelId)
            .OrderBy(r => r.RoomNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<Room>> GetAllRoomsWithHotelAsync()
    {
        return await _dbSet
            .Include(r => r.Hotel)
            .OrderBy(r => r.Hotel.Name)
            .ThenBy(r => r.RoomNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<Room>> GetAvailableRoomsAsync(int hotelId, DateTime checkIn, DateTime checkOut)
    {
        return await _dbSet
            .Where(r => r.HotelId == hotelId && 
                       r.Status == RoomStatus.Available &&
                       !r.Reservations.Any(res => 
                           res.Status != ReservationStatus.Cancelled &&
                           res.CheckInDate < checkOut && 
                           res.CheckOutDate > checkIn))
            .OrderBy(r => r.RoomNumber)
            .ToListAsync();
    }

    public async Task<Room?> GetRoomWithReservationsAsync(int roomId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _dbSet
            .Include(r => r.Reservations)
                .ThenInclude(res => res.Guest)
            .Where(r => r.Id == roomId);

        if (fromDate.HasValue && toDate.HasValue)
        {
            query = query.Where(r => r.Reservations.Any(res => 
                res.CheckInDate <= toDate.Value && res.CheckOutDate >= fromDate.Value));
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null)
    {
        var room = await _dbSet
            .Include(r => r.Reservations)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null || room.Status != RoomStatus.Available)
            return false;

        var conflictingReservations = room.Reservations
            .Where(r => r.Status != ReservationStatus.Cancelled &&
                       r.CheckInDate < checkOut && 
                       r.CheckOutDate > checkIn);

        if (excludeReservationId.HasValue)
        {
            conflictingReservations = conflictingReservations
                .Where(r => r.Id != excludeReservationId.Value);
        }

        return !conflictingReservations.Any();
    }

    public async Task<IEnumerable<Room>> GetRoomsByStatusAsync(RoomStatus status)
    {
        return await _dbSet
            .Include(r => r.Hotel)
            .Where(r => r.Status == status)
            .OrderBy(r => r.Hotel.Name)
            .ThenBy(r => r.RoomNumber)
            .ToListAsync();
    }

    public async Task<bool> ExistsInHotelAsync(int hotelId, string roomNumber)
    {
        return await _dbSet.AnyAsync(r => r.HotelId == hotelId && r.RoomNumber == roomNumber);
    }

    public async Task<Room?> GetRoomWithHotelAsync(int roomId)
    {
        return await _dbSet
            .Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.Id == roomId);
    }

    public async Task<Room?> GetRoomByNumberAsync(int hotelId, string roomNumber)
    {
        return await _dbSet
            .FirstOrDefaultAsync(r => r.HotelId == hotelId && r.RoomNumber == roomNumber);
    }
}