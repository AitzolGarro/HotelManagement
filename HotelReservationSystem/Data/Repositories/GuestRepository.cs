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
        return await _dbSet
            .Include(g => g.Reservations)
                .ThenInclude(r => r.Hotel)
            .Include(g => g.Reservations)
                .ThenInclude(r => r.Room)
            .FirstOrDefaultAsync(g => g.Id == guestId);
    }

    public async Task<IEnumerable<Guest>> SearchGuestsAsync(string searchTerm)
    {
        var lowerSearchTerm = searchTerm.ToLower();
        
        return await _dbSet
            .Where(g => g.FirstName.ToLower().Contains(lowerSearchTerm) ||
                       g.LastName.ToLower().Contains(lowerSearchTerm) ||
                       (g.Email != null && g.Email.ToLower().Contains(lowerSearchTerm)) ||
                       (g.Phone != null && g.Phone.Contains(searchTerm)) ||
                       (g.DocumentNumber != null && g.DocumentNumber.ToLower().Contains(lowerSearchTerm)))
            .OrderBy(g => g.LastName)
            .ThenBy(g => g.FirstName)
            .ToListAsync();
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _dbSet.AnyAsync(g => g.Email == email);
    }

    public async Task<bool> ExistsByDocumentNumberAsync(string documentNumber)
    {
        return await _dbSet.AnyAsync(g => g.DocumentNumber == documentNumber);
    }
}