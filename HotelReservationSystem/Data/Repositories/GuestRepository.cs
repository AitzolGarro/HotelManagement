using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;

namespace HotelReservationSystem.Data.Repositories;

public class GuestRepository : Repository<Guest>, IGuestRepository
{
    public GuestRepository(HotelReservationContext context) : base(context)
    {
    }

    public async Task<Guest?> GetGuestByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(g => g.Email == email);
    }

    public async Task<Guest?> GetGuestByDocumentNumberAsync(string documentNumber)
    {
        return await _dbSet
            .FirstOrDefaultAsync(g => g.DocumentNumber == documentNumber);
    }

    public async Task<Guest?> GetGuestWithReservationsAsync(int guestId)
    {
        // AsNoTracking para consulta de solo lectura; se limita a las últimas 50 reservaciones
        // para evitar cargar todo el historial del huésped en memoria
        return await _dbSet.AsNoTracking()
            .Include(g => g.Reservations.OrderByDescending(r => r.CheckInDate).Take(50))
                .ThenInclude(r => r.Hotel)
            .Include(g => g.Reservations.OrderByDescending(r => r.CheckInDate).Take(50))
                .ThenInclude(r => r.Room)
            .FirstOrDefaultAsync(g => g.Id == guestId);
    }

    public async Task<IEnumerable<Guest>> SearchGuestsAsync(string searchTerm)
    {
        var lowerSearchTerm = searchTerm.ToLower();

        // Límite de 50 resultados para evitar respuestas masivas en búsquedas amplias
        return await _dbSet.AsNoTracking()
            .Where(g => g.FirstName.ToLower().Contains(lowerSearchTerm) ||
                       g.LastName.ToLower().Contains(lowerSearchTerm) ||
                       (g.Email != null && g.Email.ToLower().Contains(lowerSearchTerm)) ||
                       (g.Phone != null && g.Phone.Contains(searchTerm)) ||
                       (g.DocumentNumber != null && g.DocumentNumber.ToLower().Contains(lowerSearchTerm)))
            .OrderBy(g => g.LastName)
            .ThenBy(g => g.FirstName)
            .Take(50)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Guest> Items, int TotalCount)> GetPagedGuestsAsync(int pageNumber, int pageSize)
    {
        var query = _dbSet.AsNoTracking();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(g => g.LastName)
            .ThenBy(g => g.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _dbSet.AnyAsync(g => g.Email == email);
    }

    public async Task<bool> ExistsByDocumentNumberAsync(string documentNumber)
    {
        return await _dbSet.AnyAsync(g => g.DocumentNumber == documentNumber);
    }

    public async Task<(IEnumerable<Guest> Items, int TotalCount)> SearchAsync(
        Models.DTOs.GuestSearchCriteria criteria, int pageNumber, int pageSize)
    {
        var query = _dbSet.AsNoTracking()
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
