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
        // Consulta de solo lectura: se agrega AsNoTracking para evitar seguimiento innecesario
        return await _dbSet.AsNoTracking()
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
        // AsSplitQuery evita el producto cartesiano al cargar colecciones relacionadas
        var query = _dbSet.AsNoTracking()
            .Include(r => r.Reservations)
                .ThenInclude(res => res.Guest)
            .AsSplitQuery()
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
        // Verificar primero si la habitación existe y está disponible (sin cargar reservaciones)
        var roomStatus = await _dbSet.AsNoTracking()
            .Where(r => r.Id == roomId)
            .Select(r => new { r.Status })
            .FirstOrDefaultAsync();

        if (roomStatus == null || roomStatus.Status != RoomStatus.Available)
            return false;

        // Filtrar conflictos directamente en SQL, evitando el problema N+1 de cargar todas las reservaciones
        var query = _context.Set<Reservation>()
            .Where(r => r.RoomId == roomId &&
                       r.Status != ReservationStatus.Cancelled &&
                       r.CheckInDate < checkOut &&
                       r.CheckOutDate > checkIn);

        if (excludeReservationId.HasValue)
        {
            query = query.Where(r => r.Id != excludeReservationId.Value);
        }

        return !await query.AnyAsync();
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

    public async Task<(IEnumerable<Room> Items, int TotalCount)> SearchAsync(
        Models.DTOs.RoomSearchCriteria criteria, int pageNumber, int pageSize)
    {
        var query = _dbSet.AsNoTracking()
            .Include(r => r.Hotel)
            .ApplyCriteria(criteria);

        var totalCount = await query.CountAsync();

        var items = await query
            .ApplySort(criteria.SortBy, criteria.SortDirection)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}