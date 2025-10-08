using HotelReservationSystem.Models;

namespace HotelReservationSystem.Data.Repositories.Interfaces;

public interface IGuestRepository : IRepository<Guest>
{
    Task<Guest?> GetGuestByEmailAsync(string email);
    Task<Guest?> GetGuestByDocumentNumberAsync(string documentNumber);
    Task<Guest?> GetGuestWithReservationsAsync(int guestId);
    Task<IEnumerable<Guest>> SearchGuestsAsync(string searchTerm);
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByDocumentNumberAsync(string documentNumber);
}