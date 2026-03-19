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
        // Consulta de solo lectura: filtrado por rango de fechas a nivel SQL
        var query = _dbSet.AsNoTracking()
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

    public async Task<(IEnumerable<Reservation> Items, int TotalCount)> GetPagedReservationsAsync(DateTime? fromDate, DateTime? toDate, int? hotelId, ReservationStatus? status, int? roomId, int pageNumber, int pageSize)
    {
        // Consulta paginada con seguimiento desactivado para mejor rendimiento
        var query = _dbSet.AsNoTracking()
            .Include(r => r.Hotel)
            .Include(r => r.Room)
            .Include(r => r.Guest)
            .AsQueryable();

        if (fromDate.HasValue && toDate.HasValue)
        {
            query = query.Where(r => r.CheckInDate <= toDate.Value && r.CheckOutDate >= fromDate.Value);
        }

        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (roomId.HasValue)
        {
            query = query.Where(r => r.RoomId == roomId.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IEnumerable<Reservation>> GetReservationsByRoomAsync(int roomId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        // Consulta de solo lectura filtrada por habitación
        var query = _dbSet.AsNoTracking()
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
        // Consulta de solo lectura: historial de reservaciones por huésped
        return await _dbSet.AsNoTracking()
            .Include(r => r.Hotel)
            .Include(r => r.Room)
            .Where(r => r.GuestId == guestId)
            .OrderByDescending(r => r.CheckInDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reservation>> GetReservationsByStatusAsync(ReservationStatus status, int? hotelId = null)
    {
        // Consulta de solo lectura con límite de seguridad para evitar cargar conjuntos de datos masivos
        var query = _dbSet.AsNoTracking()
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
            .Take(200)
            .ToListAsync();
    }

    public async Task<Reservation?> GetReservationByBookingReferenceAsync(string bookingReference)
    {
        // Incluye entidades relacionadas necesarias para mostrar detalles completos de la reservación
        return await _dbSet
            .Include(r => r.Hotel)
            .Include(r => r.Room)
            .Include(r => r.Guest)
            .FirstOrDefaultAsync(r => r.BookingReference == bookingReference);
    }

    public async Task<IEnumerable<Reservation>> GetConflictingReservationsAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null)
    {
        // Filtrado de conflictos directamente en SQL para evitar carga innecesaria de datos
        var query = _dbSet.AsNoTracking()
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
        // Consulta de solo lectura: check-ins del día filtrados a nivel SQL
        var query = _dbSet.AsNoTracking()
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
        // Consulta de solo lectura: check-outs del día filtrados a nivel SQL
        var query = _dbSet.AsNoTracking()
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
        // Verificación de existencia: no se necesita cargar entidades completas
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
        // Verificación de existencia eficiente sin cargar entidades
        return await _dbSet.AnyAsync(r => r.HotelId == hotelId &&
                                         r.Status != ReservationStatus.Cancelled &&
                                         r.Status != ReservationStatus.CheckedOut &&
                                         r.CheckOutDate >= DateTime.Today);
    }

    public async Task<bool> HasActiveReservationsForRoomAsync(int roomId)
    {
        // Verificación de existencia eficiente sin cargar entidades
        return await _dbSet.AnyAsync(r => r.RoomId == roomId &&
                                         r.Status != ReservationStatus.Cancelled &&
                                         r.Status != ReservationStatus.CheckedOut &&
                                         r.CheckOutDate >= DateTime.Today);
    }

    public async Task<(IEnumerable<Reservation> Items, int TotalCount)> SearchReservationsAsync(Models.DTOs.ReservationSearchCriteria criteria, int pageNumber, int pageSize)
    {
        // Búsqueda paginada con múltiples filtros opcionales aplicados a nivel SQL
        var query = _dbSet.AsNoTracking()
            .Include(r => r.Hotel)
            .Include(r => r.Room)
            .Include(r => r.Guest)
            .AsQueryable();

        if (criteria.DateFrom.HasValue)
            query = query.Where(r => r.CheckOutDate >= criteria.DateFrom.Value);

        if (criteria.DateTo.HasValue)
            query = query.Where(r => r.CheckInDate <= criteria.DateTo.Value);

        if (criteria.HotelId.HasValue)
            query = query.Where(r => r.HotelId == criteria.HotelId.Value);

        if (criteria.Statuses != null && criteria.Statuses.Any())
            query = query.Where(r => criteria.Statuses.Contains(r.Status));

        if (criteria.Sources != null && criteria.Sources.Any())
            query = query.Where(r => criteria.Sources.Contains(r.Source));

        if (criteria.MinAmount.HasValue)
            query = query.Where(r => r.TotalAmount >= criteria.MinAmount.Value);

        if (criteria.MaxAmount.HasValue)
            query = query.Where(r => r.TotalAmount <= criteria.MaxAmount.Value);

        if (!string.IsNullOrEmpty(criteria.GuestName))
        {
            var term = criteria.GuestName.ToLower();
            query = query.Where(r => r.Guest != null &&
                (r.Guest.FirstName.ToLower().Contains(term) || r.Guest.LastName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrEmpty(criteria.BookingReference))
        {
            var term = criteria.BookingReference.ToLower();
            query = query.Where(r => r.BookingReference != null && r.BookingReference.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
