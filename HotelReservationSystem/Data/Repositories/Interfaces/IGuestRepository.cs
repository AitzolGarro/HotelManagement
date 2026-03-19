using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Data.Repositories.Interfaces;

public interface IGuestRepository : IRepository<Guest>
{
    Task<Guest?> GetGuestByEmailAsync(string email);
    Task<Guest?> GetGuestByDocumentNumberAsync(string documentNumber);
    Task<Guest?> GetGuestWithReservationsAsync(int guestId);
    Task<IEnumerable<Guest>> SearchGuestsAsync(string searchTerm);
    Task<(IEnumerable<Guest> Items, int TotalCount)> GetPagedGuestsAsync(int pageNumber, int pageSize);
    Task<(IEnumerable<Guest> Items, int TotalCount)> SearchAsync(GuestSearchCriteria criteria, int pageNumber, int pageSize);
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByDocumentNumberAsync(string documentNumber);
}
