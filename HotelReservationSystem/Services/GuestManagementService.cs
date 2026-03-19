using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.Data;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services;

public class GuestManagementService : IGuestManagementService
{
    private readonly HotelReservationContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GuestManagementService> _logger;

    public GuestManagementService(HotelReservationContext context, IUnitOfWork unitOfWork, ILogger<GuestManagementService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GuestDto> CreateGuestAsync(CreateGuestRequest request)
    {
        if (!string.IsNullOrEmpty(request.Email) && await _unitOfWork.Guests.ExistsByEmailAsync(request.Email))
        {
            throw new Exception("Guest with this email already exists.");
        }

        var guest = new Guest
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            DocumentNumber = request.DocumentNumber,
            Nationality = request.Nationality,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Guests.AddAsync(guest);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(guest);
    }

    public async Task<GuestDto> UpdateGuestAsync(int id, UpdateGuestRequest request)
    {
        var guest = await _unitOfWork.Guests.GetByIdAsync(id);
        if (guest == null) throw new Exception("Guest not found");

        guest.FirstName = request.FirstName;
        guest.LastName = request.LastName;
        guest.Email = request.Email;
        guest.Phone = request.Phone;
        guest.Address = request.Address;
        guest.DocumentNumber = request.DocumentNumber;
        guest.Nationality = request.Nationality;
        guest.IsVip = request.IsVip;
        guest.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Guests.Update(guest);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(guest);
    }

    public async Task<GuestDto?> GetGuestByIdAsync(int id)
    {
        var guest = await _unitOfWork.Guests.GetByIdAsync(id);
        return guest == null ? null : MapToDto(guest);
    }

    public async Task<PagedResultDto<GuestDto>> SearchGuestsAsync(GuestSearchCriteria criteria, int pageNumber = 1, int pageSize = 20)
    {
        var query = _context.Guests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(criteria.SearchTerm))
        {
            var term = criteria.SearchTerm.ToLower();
            query = query.Where(g => 
                g.FirstName.ToLower().Contains(term) || 
                g.LastName.ToLower().Contains(term) ||
                (g.Email != null && g.Email.ToLower().Contains(term)));
        }

        if (!string.IsNullOrEmpty(criteria.Nationality))
        {
            query = query.Where(g => g.Nationality == criteria.Nationality);
        }

        if (criteria.IsVip.HasValue)
        {
            query = query.Where(g => g.IsVip == criteria.IsVip.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(g => g.LastName)
            .ThenBy(g => g.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<GuestDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<ReservationDto>> GetGuestHistoryAsync(int guestId)
    {
        var reservations = await _context.Reservations
            .Include(r => r.Hotel)
            .Include(r => r.Room)
            .Where(r => r.GuestId == guestId)
            .OrderByDescending(r => r.CheckInDate)
            .ToListAsync();

        // Very basic mapping for history
        return reservations.Select(r => new ReservationDto
        {
            Id = r.Id,
            HotelName = r.Hotel?.Name ?? "",
            RoomNumber = r.Room?.RoomNumber ?? "",
            CheckInDate = r.CheckInDate,
            CheckOutDate = r.CheckOutDate,
            Status = r.Status,
            TotalAmount = r.TotalAmount
        });
    }

    public async Task<GuestStatisticsDto> GetGuestStatisticsAsync(int guestId)
    {
        var reservations = await _context.Reservations
            .Where(r => r.GuestId == guestId)
            .ToListAsync();

        return new GuestStatisticsDto
        {
            GuestId = guestId,
            TotalReservations = reservations.Count,
            CancelledReservations = reservations.Count(r => r.Status == ReservationStatus.Cancelled),
            TotalRevenue = reservations.Where(r => r.Status == ReservationStatus.CheckedOut || r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.CheckedIn)
                                     .Sum(r => r.TotalAmount),
            TotalNightsStayed = reservations.Where(r => r.Status == ReservationStatus.CheckedOut)
                                           .Sum(r => (r.CheckOutDate - r.CheckInDate).Days),
            LastVisit = reservations.Where(r => r.Status == ReservationStatus.CheckedOut)
                                   .OrderByDescending(r => r.CheckOutDate)
                                   .FirstOrDefault()?.CheckOutDate
        };
    }

    public async Task<IEnumerable<GuestPreference>> GetGuestPreferencesAsync(int guestId)
    {
        return await _context.Set<GuestPreference>().Where(p => p.GuestId == guestId).ToListAsync();
    }

    public async Task<GuestPreference> AddGuestPreferenceAsync(int guestId, string category, string preference)
    {
        var guestPref = new GuestPreference
        {
            GuestId = guestId,
            Category = category,
            Preference = preference,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<GuestPreference>().Add(guestPref);
        await _context.SaveChangesAsync();
        return guestPref;
    }

    public async Task<bool> RemoveGuestPreferenceAsync(int preferenceId)
    {
        var pref = await _context.Set<GuestPreference>().FindAsync(preferenceId);
        if (pref == null) return false;

        _context.Set<GuestPreference>().Remove(pref);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<GuestNote>> GetGuestNotesAsync(int guestId)
    {
        return await _context.Set<GuestNote>().Where(n => n.GuestId == guestId).ToListAsync();
    }

    public async Task<GuestNote> AddGuestNoteAsync(int guestId, int userId, string noteText)
    {
        var note = new GuestNote
        {
            GuestId = guestId,
            CreatedByUserId = userId,
            Note = noteText,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<GuestNote>().Add(note);
        await _context.SaveChangesAsync();
        return note;
    }

    private static GuestDto MapToDto(Guest guest)
    {
        return new GuestDto
        {
            Id = guest.Id,
            FirstName = guest.FirstName,
            LastName = guest.LastName,
            Email = guest.Email,
            Phone = guest.Phone,
            DocumentNumber = guest.DocumentNumber
        };
    }
}