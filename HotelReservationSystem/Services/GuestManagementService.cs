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

    // Constructor con inyección de dependencias
    public GuestManagementService(HotelReservationContext context, IUnitOfWork unitOfWork, ILogger<GuestManagementService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // Crear nuevo huésped con validación de email duplicado
    public async Task<GuestDto> CreateGuestAsync(CreateGuestRequest request)
    {
        if (!string.IsNullOrEmpty(request.Email) && await _unitOfWork.Guests.ExistsByEmailAsync(request.Email))
            throw new InvalidOperationException("Ya existe un huésped con este correo electrónico.");

        var guest = MapFromCreateRequest(request);
        await _unitOfWork.Guests.AddAsync(guest);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Huésped creado con ID {GuestId}", guest.Id);
        return MapToDto(guest);
    }

    // Actualizar datos de un huésped existente
    public async Task<GuestDto> UpdateGuestAsync(int id, UpdateGuestRequest request)
    {
        var guest = await _unitOfWork.Guests.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Huésped con ID {id} no encontrado.");

        ApplyUpdateRequest(guest, request);
        _unitOfWork.Guests.Update(guest);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(guest);
    }

    public async Task<GuestDto?> GetGuestByIdAsync(int id)
    {
        var guest = await _unitOfWork.Guests.GetByIdAsync(id);
        return guest == null ? null : MapToDto(guest);
    }

    // Buscar huéspedes con criterios múltiples y paginación
    public async Task<PagedResultDto<GuestDto>> SearchGuestsAsync(GuestSearchCriteria criteria, int pageNumber = 1, int pageSize = 20)
    {
        var query = BuildSearchQuery(criteria);
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

    // Obtener historial completo de reservaciones del huésped
    public async Task<IEnumerable<ReservationDto>> GetGuestHistoryAsync(int guestId)
    {
        // Verificar que el huésped existe antes de consultar historial
        var exists = await _context.Guests.AnyAsync(g => g.Id == guestId);
        if (!exists) throw new KeyNotFoundException($"Huésped con ID {guestId} no encontrado.");

        var reservations = await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Hotel)
            .Include(r => r.Room)
            .Where(r => r.GuestId == guestId)
            .OrderByDescending(r => r.CheckInDate)
            .ToListAsync();

        return reservations.Select(MapReservationToDto);
    }

    // Calcular estadísticas del huésped a nivel de base de datos para eficiencia
    public async Task<GuestStatisticsDto> GetGuestStatisticsAsync(int guestId)
    {
        var baseQuery = _context.Reservations.AsNoTracking().Where(r => r.GuestId == guestId);

        var totalReservations = await baseQuery.CountAsync();
        var cancelledReservations = await baseQuery.CountAsync(r => r.Status == ReservationStatus.Cancelled);

        // Calcular ingresos solo de reservaciones activas o completadas
        var revenueStatuses = new[] { ReservationStatus.CheckedOut, ReservationStatus.Confirmed, ReservationStatus.CheckedIn };
        var totalRevenue = await baseQuery
            .Where(r => revenueStatuses.Contains(r.Status))
            .SumAsync(r => r.TotalAmount);

        // Calcular noches totales hospedadas de reservaciones completadas
        var completedStays = await baseQuery
            .Where(r => r.Status == ReservationStatus.CheckedOut)
            .Select(r => new { r.CheckInDate, r.CheckOutDate })
            .ToListAsync();

        var totalNightsStayed = completedStays.Sum(r => (r.CheckOutDate - r.CheckInDate).Days);

        var lastVisit = await baseQuery
            .Where(r => r.Status == ReservationStatus.CheckedOut)
            .OrderByDescending(r => r.CheckOutDate)
            .Select(r => (DateTime?)r.CheckOutDate)
            .FirstOrDefaultAsync();

        return new GuestStatisticsDto
        {
            GuestId = guestId,
            TotalReservations = totalReservations,
            CancelledReservations = cancelledReservations,
            TotalRevenue = totalRevenue,
            TotalNightsStayed = totalNightsStayed,
            LastVisit = lastVisit
        };
    }

    // Obtener todas las preferencias del huésped
    public async Task<IEnumerable<GuestPreference>> GetGuestPreferencesAsync(int guestId)
    {
        return await _context.GuestPreferences
            .AsNoTracking()
            .Where(p => p.GuestId == guestId)
            .OrderBy(p => p.Category)
            .ToListAsync();
    }

    // Agregar nueva preferencia al huésped con validación de duplicados
    public async Task<GuestPreference> AddGuestPreferenceAsync(int guestId, string category, string preference)
    {
        var guestExists = await _context.Guests.AnyAsync(g => g.Id == guestId);
        if (!guestExists) throw new KeyNotFoundException($"Huésped con ID {guestId} no encontrado.");

        var guestPref = new GuestPreference
        {
            GuestId = guestId,
            Category = category,
            Preference = preference,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GuestPreferences.Add(guestPref);
        await _context.SaveChangesAsync();
        return guestPref;
    }

    public async Task<bool> RemoveGuestPreferenceAsync(int preferenceId)
    {
        var pref = await _context.GuestPreferences.FindAsync(preferenceId);
        if (pref == null) return false;

        _context.GuestPreferences.Remove(pref);
        await _context.SaveChangesAsync();
        return true;
    }

    // Obtener todas las notas del personal sobre el huésped
    public async Task<IEnumerable<GuestNote>> GetGuestNotesAsync(int guestId)
    {
        return await _context.GuestNotes
            .AsNoTracking()
            .Where(n => n.GuestId == guestId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    // Agregar nota del personal sobre el huésped
    public async Task<GuestNote> AddGuestNoteAsync(int guestId, int userId, string noteText)
    {
        var guestExists = await _context.Guests.AnyAsync(g => g.Id == guestId);
        if (!guestExists) throw new KeyNotFoundException($"Huésped con ID {guestId} no encontrado.");

        var note = new GuestNote
        {
            GuestId = guestId,
            CreatedByUserId = userId,
            Note = noteText,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GuestNotes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }

    // Construir query de búsqueda con filtros dinámicos
    private IQueryable<Guest> BuildSearchQuery(GuestSearchCriteria criteria)
    {
        var query = _context.Guests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(criteria.SearchTerm))
        {
            var term = criteria.SearchTerm.ToLower();
            query = query.Where(g =>
                g.FirstName.ToLower().Contains(term) ||
                g.LastName.ToLower().Contains(term) ||
                (g.Email != null && g.Email.ToLower().Contains(term)) ||
                (g.Phone != null && g.Phone.Contains(criteria.SearchTerm)) ||
                (g.DocumentNumber != null && g.DocumentNumber.ToLower().Contains(term)));
        }

        if (!string.IsNullOrEmpty(criteria.Nationality))
            query = query.Where(g => g.Nationality == criteria.Nationality);

        if (criteria.IsVip.HasValue)
            query = query.Where(g => g.IsVip == criteria.IsVip.Value);

        return query;
    }

    // Mapear request de creación a entidad Guest
    private static Guest MapFromCreateRequest(CreateGuestRequest request) => new()
    {
        FirstName = request.FirstName,
        LastName = request.LastName,
        Email = request.Email,
        Phone = request.Phone,
        Address = request.Address,
        DocumentNumber = request.DocumentNumber,
        DocumentType = request.DocumentType,
        Nationality = request.Nationality,
        Company = request.Company,
        DateOfBirth = request.DateOfBirth,
        PreferredLanguage = request.PreferredLanguage,
        MarketingOptIn = request.MarketingOptIn,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    // Aplicar cambios del request de actualización a la entidad existente
    private static void ApplyUpdateRequest(Guest guest, UpdateGuestRequest request)
    {
        guest.FirstName = request.FirstName;
        guest.LastName = request.LastName;
        guest.Email = request.Email;
        guest.Phone = request.Phone;
        guest.Address = request.Address;
        guest.DocumentNumber = request.DocumentNumber;
        guest.DocumentType = request.DocumentType;
        guest.Nationality = request.Nationality;
        guest.Company = request.Company;
        guest.DateOfBirth = request.DateOfBirth;
        guest.PreferredLanguage = request.PreferredLanguage;
        guest.IsVip = request.IsVip;
        guest.VipStatus = request.VipStatus;
        guest.MarketingOptIn = request.MarketingOptIn;
        guest.UpdatedAt = DateTime.UtcNow;
    }

    // Mapear entidad Guest a DTO de respuesta
    private static GuestDto MapToDto(Guest guest) => new()
    {
        Id = guest.Id,
        FirstName = guest.FirstName,
        LastName = guest.LastName,
        Email = guest.Email,
        Phone = guest.Phone,
        Address = guest.Address,
        DocumentNumber = guest.DocumentNumber,
        DocumentType = guest.DocumentType,
        Nationality = guest.Nationality,
        Company = guest.Company,
        DateOfBirth = guest.DateOfBirth,
        PreferredLanguage = guest.PreferredLanguage,
        IsVip = guest.IsVip,
        VipStatus = guest.VipStatus,
        MarketingOptIn = guest.MarketingOptIn,
        CreatedAt = guest.CreatedAt,
        UpdatedAt = guest.UpdatedAt
    };

    // Mapear entidad Reservation a DTO para historial del huésped
    private static ReservationDto MapReservationToDto(Reservation r) => new()
    {
        Id = r.Id,
        HotelId = r.HotelId,
        RoomId = r.RoomId,
        GuestId = r.GuestId,
        BookingReference = r.BookingReference,
        Source = r.Source,
        CheckInDate = r.CheckInDate,
        CheckOutDate = r.CheckOutDate,
        NumberOfGuests = r.NumberOfGuests,
        TotalAmount = r.TotalAmount,
        Status = r.Status,
        SpecialRequests = r.SpecialRequests,
        InternalNotes = r.InternalNotes,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        HotelName = r.Hotel?.Name ?? string.Empty,
        RoomNumber = r.Room?.RoomNumber ?? string.Empty,
        RoomType = r.Room?.Type.ToString() ?? string.Empty
    };
}
