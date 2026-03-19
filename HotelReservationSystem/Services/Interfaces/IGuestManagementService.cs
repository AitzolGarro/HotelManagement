using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces;

public interface IGuestManagementService
{
    Task<GuestDto> CreateGuestAsync(CreateGuestRequest request);
    Task<GuestDto> UpdateGuestAsync(int id, UpdateGuestRequest request);
    Task<GuestDto?> GetGuestByIdAsync(int id);
    Task<PagedResultDto<GuestDto>> SearchGuestsAsync(GuestSearchCriteria criteria, int pageNumber = 1, int pageSize = 20);
    
    Task<IEnumerable<ReservationDto>> GetGuestHistoryAsync(int guestId);
    Task<GuestStatisticsDto> GetGuestStatisticsAsync(int guestId);
    
    // Preferences and Notes
    Task<IEnumerable<GuestPreference>> GetGuestPreferencesAsync(int guestId);
    Task<GuestPreference> AddGuestPreferenceAsync(int guestId, string category, string preference);
    Task<bool> RemoveGuestPreferenceAsync(int preferenceId);
    
    Task<IEnumerable<GuestNote>> GetGuestNotesAsync(int guestId);
    Task<GuestNote> AddGuestNoteAsync(int guestId, int userId, string noteText);
}

public class GuestSearchCriteria
{
    public string? SearchTerm { get; set; }
    public string? Nationality { get; set; }
    public bool? IsVip { get; set; }
}

public class GuestStatisticsDto
{
    public int GuestId { get; set; }
    public int TotalReservations { get; set; }
    public int CancelledReservations { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalNightsStayed { get; set; }
    public DateTime? LastVisit { get; set; }
}