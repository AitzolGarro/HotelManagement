using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.Data.Repositories;

public class ReservationRepository : Repository<Reservation>, IReservationRepository
{
    public ReservationRepository(HotelReservationContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Reservation>> GetReservationsByDateRangeAsync(DateTime fromDate, DateTime toDate, int? hotelId = null)
    {
        var query = _dbSet
            .Include(r => r.Hotel)
            .Include(r => r.Room)
            .Include(r => r.Guest)
            .Where(r => r.CheckInDate <= toDate && r.CheckOutDate >= fromDate);

        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        return await query
            .OrderBy(r => r.CheckInDate)
            .ThenBy(r => r.Room.RoomNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reservation>> GetReservationsByRoomAsync(int roomId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _dbSet
            .Include(r => r.Guest)
            .Where(r => r.RoomId == roomId);

        if (fromDate.HasValue && toDate.HasValue)
        {
            query = query.Where(r => r.CheckInDate <= toDate.Value && r.CheckOutDate >= fromDate.Value);
        }

        return await query
            .OrderBy(r => r.CheckInDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reservation>> GetReservationsByGuestAsync(int guestId)
    {
        return await _dbSet
            .Include(r => r.Hotel)
            .Include(r => r.Room)
            .Where(r => r.GuestId == guestId)
            .OrderByDescending(r => r.CheckInDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reservation>> GetReservationsByStatusAsync(ReservationStatus status, int? hotelId = null)
    {
        var query = _dbSet
            .Include(r => r.Hotel)
            .Include(r => r.Room)
            .Include(r => r.Guest)
            .Where(r => r.Status == status);

        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        return await query
            .OrderBy(r => r.CheckInDate)
            .ToListAsync();
    }

    public async Task<Reservation?> GetReservationByBookingReferenceAsync(string bookingReference)
    {
        return await _dbSet
            .Include(r => r.Hotel)
            .Include(r => r.Room)
            .Include(r => r.Guest)
            .FirstOrDefaultAsync(r => r.BookingReference == bookingReference);
    }

    public async Task<IEnumerable<Reservation>> GetConflictingReservationsAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null)
    {
        var query = _dbSet
            .Include(r => r.Guest)
            .Where(r => r.RoomId == roomId &&
                       r.Status != ReservationStatus.Cancelled &&
                       r.CheckInDate < checkOut && 
                       r.CheckOutDate > checkIn);

        if (excludeReservationId.HasValue)
        {
            query = query.Where(r => r.Id != excludeReservationId.Value);
        }

        return await query
            .OrderBy(r => r.CheckInDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reservation>> GetCheckInsForDateAsync(DateTime date, int? hotelId = null)
    {
        var query = _dbSet
            .Include(r => r.Hotel)
            .Include(r => r.Room)
            .Include(r => r.Guest)
            .Where(r => r.CheckInDate.Date == date.Date && 
                       r.Status == ReservationStatus.Confirmed);

        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        return await query
            .OrderBy(r => r.Room.RoomNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reservation>> GetCheckOutsForDateAsync(DateTime date, int? hotelId = null)
    {
        var query = _dbSet
            .Include(r => r.Hotel)
            .Include(r => r.Room)
            .Include(r => r.Guest)
            .Where(r => r.CheckOutDate.Date == date.Date && 
                       (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.CheckedIn));

        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        return await query
            .OrderBy(r => r.Room.RoomNumber)
            .ToListAsync();
    }

    public async Task<bool> HasConflictingReservationsAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null)
    {
        var query = _dbSet
            .Where(r => r.RoomId == roomId &&
                       r.Status != ReservationStatus.Cancelled &&
                       r.CheckInDate < checkOut && 
                       r.CheckOutDate > checkIn);

        if (excludeReservationId.HasValue)
        {
            query = query.Where(r => r.Id != excludeReservationId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> HasActiveReservationsForHotelAsync(int hotelId)
    {
        return await _dbSet.AnyAsync(r => r.HotelId == hotelId && 
                                         r.Status != ReservationStatus.Cancelled &&
                                         r.Status != ReservationStatus.CheckedOut &&
                                         r.CheckOutDate >= DateTime.Today);
    }

    public async Task<bool> HasActiveReservationsForRoomAsync(int roomId)
    {
        return await _dbSet.AnyAsync(r => r.RoomId == roomId && 
                                         r.Status != ReservationStatus.Cancelled &&
                                         r.Status != ReservationStatus.CheckedOut &&
                                         r.CheckOutDate >= DateTime.Today);
    }
}