using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using HotelReservationSystem.Data;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services;

public class GuestPortalService : IGuestPortalService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly HotelReservationContext _context;
    private readonly IConfiguration _configuration;
    private readonly IReservationService _reservationService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<GuestPortalService> _logger;

    public GuestPortalService(
        IUnitOfWork unitOfWork,
        HotelReservationContext context,
        IConfiguration configuration,
        IReservationService reservationService,
        INotificationService notificationService,
        ILogger<GuestPortalService> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _configuration = configuration;
        _reservationService = reservationService;
        _notificationService = notificationService;
        _logger = logger;
    }

    // ─── Authentication ───────────────────────────────────────────────────────

    public async Task<GuestLoginResponse> LoginAsync(GuestLoginRequest request)
    {
        var reservation = await _unitOfWork.Reservations.GetReservationByBookingReferenceAsync(request.BookingReference);
        if (reservation == null || reservation.Guest?.Email?.ToLower() != request.Email.ToLower())
        {
            _logger.LogWarning("Guest login failed for email {Email} with booking ref {Ref}", request.Email, request.BookingReference);
            throw new UnauthorizedAccessException("Invalid email or booking reference.");
        }

        var guest = reservation.Guest;
        var token = GenerateGuestJwtToken(guest);

        _logger.LogInformation("Guest {GuestId} logged in via booking reference {Ref}", guest.Id, request.BookingReference);

        return new GuestLoginResponse
        {
            Token = token,
            Expires = DateTime.UtcNow.AddHours(24),
            Guest = MapToProfile(guest)
        };
    }

    // ─── Profile ──────────────────────────────────────────────────────────────

    public async Task<GuestProfileDto> GetGuestProfileAsync(int guestId)
    {
        var guest = await _unitOfWork.Guests.GetByIdAsync(guestId)
            ?? throw new KeyNotFoundException($"Guest {guestId} not found.");
        return MapToProfile(guest);
    }

    public async Task<GuestProfileDto> UpdateGuestProfileAsync(int guestId, GuestProfileDto request)
    {
        var guest = await _unitOfWork.Guests.GetByIdAsync(guestId)
            ?? throw new KeyNotFoundException($"Guest {guestId} not found.");

        guest.Phone = request.Phone;
        guest.Address = request.Address;
        guest.Nationality = request.Nationality;
        guest.PreferredLanguage = request.PreferredLanguage;
        guest.MarketingOptIn = request.MarketingOptIn;
        guest.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Guests.Update(guest);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Guest {GuestId} updated their profile", guestId);
        return MapToProfile(guest);
    }

    public async Task<GuestNotificationPreferencesDto> GetNotificationPreferencesAsync(int guestId)
    {
        var pref = await _context.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GuestId == guestId);

        if (pref == null)
        {
            return new GuestNotificationPreferencesDto
            {
                BookingConfirmations = true,
                CheckInReminders = true,
                CheckOutReminders = true,
                ModificationConfirmations = true,
                PromotionalOffers = false,
                EmailChannel = true,
                SmsChannel = false
            };
        }

        return new GuestNotificationPreferencesDto
        {
            BookingConfirmations = pref.BookingConfirmations,
            CheckInReminders = pref.CheckInReminders,
            CheckOutReminders = pref.CheckOutReminders,
            ModificationConfirmations = pref.ModificationConfirmations,
            PromotionalOffers = pref.PromotionalOffers,
            EmailChannel = pref.EmailChannel,
            SmsChannel = pref.SmsChannel
        };
    }

    public async Task<GuestNotificationPreferencesDto> UpdateNotificationPreferencesAsync(int guestId, GuestNotificationPreferencesDto dto)
    {
        var pref = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.GuestId == guestId);

        if (pref == null)
        {
            pref = new NotificationPreference { GuestId = guestId };
            _context.NotificationPreferences.Add(pref);
        }

        pref.BookingConfirmations = dto.BookingConfirmations;
        pref.CheckInReminders = dto.CheckInReminders;
        pref.CheckOutReminders = dto.CheckOutReminders;
        pref.ModificationConfirmations = dto.ModificationConfirmations;
        pref.PromotionalOffers = dto.PromotionalOffers;
        pref.EmailChannel = dto.EmailChannel;
        pref.SmsChannel = dto.SmsChannel;
        pref.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Guest {GuestId} updated notification preferences", guestId);
        return dto;
    }

    // ─── Reservations ─────────────────────────────────────────────────────────

    public async Task<IEnumerable<ReservationDto>> GetMyReservationsAsync(int guestId)
    {
        return await _reservationService.GetReservationsByGuestAsync(guestId);
    }

    public async Task<ReservationDto?> GetMyReservationByIdAsync(int guestId, int reservationId)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdAsync(reservationId);
        if (reservation == null || reservation.GuestId != guestId)
            return null;

        return await _reservationService.GetReservationByIdAsync(reservationId);
    }

    public async Task<ReservationDto> ModifyReservationAsync(int guestId, int reservationId, UpdateReservationDatesRequest request)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdAsync(reservationId);
        if (reservation == null || reservation.GuestId != guestId)
            throw new UnauthorizedAccessException("You do not have permission to modify this reservation.");

        if (reservation.Status != ReservationStatus.Confirmed && reservation.Status != ReservationStatus.Pending)
            throw new InvalidOperationException("This reservation cannot be modified in its current status.");

        if (request.CheckInDate < DateTime.UtcNow.Date)
            throw new InvalidOperationException("Check-in date cannot be in the past.");

        if (request.CheckOutDate <= request.CheckInDate)
            throw new InvalidOperationException("Check-out date must be after check-in date.");

        var result = await _reservationService.UpdateReservationDatesAsync(reservationId, request);

        // Send modification confirmation email
        try { await SendModificationConfirmationAsync(reservationId); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to send modification confirmation for reservation {Id}", reservationId); }

        return result;
    }

    public async Task<bool> CancelReservationAsync(int guestId, int reservationId, string reason)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdAsync(reservationId);
        if (reservation == null || reservation.GuestId != guestId)
            throw new UnauthorizedAccessException("You do not have permission to cancel this reservation.");

        if (reservation.Status == ReservationStatus.Cancelled)
            throw new InvalidOperationException("This reservation is already cancelled.");

        if (reservation.Status == ReservationStatus.CheckedIn || reservation.Status == ReservationStatus.CheckedOut)
            throw new InvalidOperationException("Cannot cancel a reservation that is checked-in or completed.");

        var result = await _reservationService.CancelReservationAsync(reservationId, new CancelReservationRequest { Reason = reason });

        // Notify guest
        if (result)
        {
            var guest = await _unitOfWork.Guests.GetByIdAsync(guestId);
            if (guest?.Email != null)
            {
                try
                {
                    var subject = $"Reservation Cancellation Confirmed – {reservation.BookingReference}";
                    var body = BuildCancellationEmailBody(reservation, guest, reason);
                    await _notificationService.SendEmailNotificationAsync(guest.Email, subject, body);
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to send cancellation email for reservation {Id}", reservationId); }
            }
        }

        return result;
    }

    public async Task<ReservationDto> SubmitSpecialRequestAsync(int guestId, int reservationId, string specialRequests)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Hotel)
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.GuestId == guestId)
            ?? throw new UnauthorizedAccessException("Reservation not found or access denied.");

        if (reservation.Status == ReservationStatus.Cancelled || reservation.Status == ReservationStatus.CheckedOut)
            throw new InvalidOperationException("Cannot submit special requests for this reservation.");

        reservation.SpecialRequests = specialRequests;
        reservation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Guest {GuestId} submitted special request for reservation {ReservationId}", guestId, reservationId);

        return MapReservationToDto(reservation);
    }

    // ─── Notifications ────────────────────────────────────────────────────────

    public async Task SendBookingConfirmationEmailAsync(int reservationId)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Guest)
            .Include(r => r.Hotel)
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation?.Guest?.Email == null) return;

        var prefs = await GetNotificationPreferencesAsync(reservation.GuestId);
        if (!prefs.BookingConfirmations || !prefs.EmailChannel) return;

        var subject = $"Booking Confirmation – {reservation.BookingReference}";
        var body = BuildConfirmationEmailBody(reservation);
        await _notificationService.SendEmailNotificationAsync(reservation.Guest.Email, subject, body);
        _logger.LogInformation("Booking confirmation sent for reservation {Id}", reservationId);
    }

    public async Task SendCheckInReminderAsync(int reservationId)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Guest)
            .Include(r => r.Hotel)
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation?.Guest?.Email == null) return;

        var prefs = await GetNotificationPreferencesAsync(reservation.GuestId);
        if (!prefs.CheckInReminders || !prefs.EmailChannel) return;

        var subject = $"Check-In Reminder – Tomorrow at {reservation.Hotel?.Name}";
        var body = BuildCheckInReminderBody(reservation);
        await _notificationService.SendEmailNotificationAsync(reservation.Guest.Email, subject, body);
        _logger.LogInformation("Check-in reminder sent for reservation {Id}", reservationId);
    }

    public async Task SendCheckOutReminderAsync(int reservationId)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Guest)
            .Include(r => r.Hotel)
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation?.Guest?.Email == null) return;

        var prefs = await GetNotificationPreferencesAsync(reservation.GuestId);
        if (!prefs.CheckOutReminders || !prefs.EmailChannel) return;

        var subject = $"Check-Out Reminder – Today at {reservation.Hotel?.Name}";
        var body = BuildCheckOutReminderBody(reservation);
        await _notificationService.SendEmailNotificationAsync(reservation.Guest.Email, subject, body);
        _logger.LogInformation("Check-out reminder sent for reservation {Id}", reservationId);
    }

    public async Task SendModificationConfirmationAsync(int reservationId)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Guest)
            .Include(r => r.Hotel)
            .Include(r => r.Room)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation?.Guest?.Email == null) return;

        var prefs = await GetNotificationPreferencesAsync(reservation.GuestId);
        if (!prefs.ModificationConfirmations || !prefs.EmailChannel) return;

        var subject = $"Reservation Modified – {reservation.BookingReference}";
        var body = BuildModificationEmailBody(reservation);
        await _notificationService.SendEmailNotificationAsync(reservation.Guest.Email, subject, body);
        _logger.LogInformation("Modification confirmation sent for reservation {Id}", reservationId);
    }

    public async Task ProcessUpcomingRemindersAsync()
    {
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        var today = DateTime.UtcNow.Date;

        // Check-in reminders: reservations checking in tomorrow
        var checkInTomorrow = await _context.Reservations
            .Where(r => r.CheckInDate.Date == tomorrow
                     && (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Pending))
            .Select(r => r.Id)
            .ToListAsync();

        foreach (var id in checkInTomorrow)
        {
            try { await SendCheckInReminderAsync(id); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to send check-in reminder for reservation {Id}", id); }
        }

        // Check-out reminders: reservations checking out today
        var checkOutToday = await _context.Reservations
            .Where(r => r.CheckOutDate.Date == today && r.Status == ReservationStatus.CheckedIn)
            .Select(r => r.Id)
            .ToListAsync();

        foreach (var id in checkOutToday)
        {
            try { await SendCheckOutReminderAsync(id); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to send check-out reminder for reservation {Id}", id); }
        }

        _logger.LogInformation("Processed {CheckIn} check-in reminders and {CheckOut} check-out reminders",
            checkInTomorrow.Count, checkOutToday.Count);
    }

    // ─── JWT Token Generation ─────────────────────────────────────────────────

    private string GenerateGuestJwtToken(Guest guest)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"] ?? "HotelReservationSystemSecretKeyForJWTTokenGeneration2024!");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, guest.Id.ToString()),
            new(ClaimTypes.Email, guest.Email ?? string.Empty),
            new(ClaimTypes.Name, $"{guest.FirstName} {guest.LastName}"),
            new(ClaimTypes.Role, "Guest"),
            new("GuestPortal", "true")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    // ─── Mapping Helpers ──────────────────────────────────────────────────────

    private static GuestProfileDto MapToProfile(Guest guest) => new()
    {
        Id = guest.Id,
        FirstName = guest.FirstName,
        LastName = guest.LastName,
        Email = guest.Email,
        Phone = guest.Phone,
        Address = guest.Address,
        Nationality = guest.Nationality,
        PreferredLanguage = guest.PreferredLanguage,
        MarketingOptIn = guest.MarketingOptIn,
        IsVip = guest.IsVip,
        VipStatus = guest.VipStatus,
        LoyaltyPoints = ComputeLoyaltyPoints(guest),
        LoyaltyTier = ComputeLoyaltyTier(guest)
    };

    private static int ComputeLoyaltyPoints(Guest guest)
    {
        // Simple heuristic: 10 points per reservation (real impl would use a loyalty table)
        return (guest.Reservations?.Count ?? 0) * 10;
    }

    private static string ComputeLoyaltyTier(Guest guest)
    {
        if (guest.IsVip) return guest.VipStatus ?? "VIP";
        var stays = guest.Reservations?.Count ?? 0;
        return stays switch
        {
            >= 20 => "Platinum",
            >= 10 => "Gold",
            >= 5  => "Silver",
            _     => "Standard"
        };
    }

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

    // ─── Email Body Builders ──────────────────────────────────────────────────

    private static string BuildConfirmationEmailBody(Reservation r) => $@"
<html><body style='font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto'>
<div style='background:#0d6efd;padding:20px;text-align:center'>
  <h1 style='color:#fff;margin:0'>Booking Confirmed!</h1>
</div>
<div style='padding:24px'>
  <p>Dear {r.Guest?.FirstName},</p>
  <p>Your reservation has been confirmed. Here are your booking details:</p>
  <table style='width:100%;border-collapse:collapse;margin:16px 0'>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>Booking Reference</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.BookingReference}</td></tr>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>Hotel</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.Hotel?.Name}</td></tr>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>Room</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.Room?.RoomNumber} ({r.Room?.Type})</td></tr>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>Check-In</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.CheckInDate:dddd, MMMM d, yyyy}</td></tr>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>Check-Out</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.CheckOutDate:dddd, MMMM d, yyyy}</td></tr>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>Guests</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.NumberOfGuests}</td></tr>
    <tr><td style='padding:8px;font-weight:bold'>Total Amount</td><td style='padding:8px'>${r.TotalAmount:F2}</td></tr>
  </table>
  {(string.IsNullOrEmpty(r.SpecialRequests) ? "" : $"<p><strong>Special Requests:</strong> {r.SpecialRequests}</p>")}
  <p>You can manage your reservation at any time through our Guest Portal.</p>
  <p>We look forward to welcoming you!</p>
</div>
<div style='background:#f8f9fa;padding:16px;text-align:center;font-size:12px;color:#666'>
  <p>Hotel Reservation System &bull; This is an automated message, please do not reply.</p>
</div>
</body></html>";

    private static string BuildCheckInReminderBody(Reservation r) => $@"
<html><body style='font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto'>
<div style='background:#198754;padding:20px;text-align:center'>
  <h1 style='color:#fff;margin:0'>Check-In Reminder</h1>
</div>
<div style='padding:24px'>
  <p>Dear {r.Guest?.FirstName},</p>
  <p>This is a friendly reminder that your check-in is <strong>tomorrow</strong>!</p>
  <table style='width:100%;border-collapse:collapse;margin:16px 0'>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>Booking Reference</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.BookingReference}</td></tr>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>Hotel</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.Hotel?.Name}</td></tr>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>Check-In Date</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.CheckInDate:dddd, MMMM d, yyyy}</td></tr>
    <tr><td style='padding:8px;font-weight:bold'>Room</td><td style='padding:8px'>{r.Room?.RoomNumber} ({r.Room?.Type})</td></tr>
  </table>
  <p>Standard check-in time is 3:00 PM. Early check-in may be available upon request.</p>
  <p>We look forward to seeing you tomorrow!</p>
</div>
<div style='background:#f8f9fa;padding:16px;text-align:center;font-size:12px;color:#666'>
  <p>Hotel Reservation System &bull; This is an automated message, please do not reply.</p>
</div>
</body></html>";

    private static string BuildCheckOutReminderBody(Reservation r) => $@"
<html><body style='font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto'>
<div style='background:#fd7e14;padding:20px;text-align:center'>
  <h1 style='color:#fff;margin:0'>Check-Out Reminder</h1>
</div>
<div style='padding:24px'>
  <p>Dear {r.Guest?.FirstName},</p>
  <p>We hope you've enjoyed your stay! This is a reminder that your check-out is <strong>today</strong>.</p>
  <table style='width:100%;border-collapse:collapse;margin:16px 0'>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>Booking Reference</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.BookingReference}</td></tr>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>Hotel</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.Hotel?.Name}</td></tr>
    <tr><td style='padding:8px;font-weight:bold'>Check-Out Date</td><td style='padding:8px'>{r.CheckOutDate:dddd, MMMM d, yyyy}</td></tr>
  </table>
  <p>Standard check-out time is 11:00 AM. Late check-out may be available upon request at the front desk.</p>
  <p>Thank you for staying with us. We hope to see you again soon!</p>
</div>
<div style='background:#f8f9fa;padding:16px;text-align:center;font-size:12px;color:#666'>
  <p>Hotel Reservation System &bull; This is an automated message, please do not reply.</p>
</div>
</body></html>";

    private static string BuildModificationEmailBody(Reservation r) => $@"
<html><body style='font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto'>
<div style='background:#6f42c1;padding:20px;text-align:center'>
  <h1 style='color:#fff;margin:0'>Reservation Modified</h1>
</div>
<div style='padding:24px'>
  <p>Dear {r.Guest?.FirstName},</p>
  <p>Your reservation has been successfully modified. Here are your updated booking details:</p>
  <table style='width:100%;border-collapse:collapse;margin:16px 0'>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>Booking Reference</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.BookingReference}</td></tr>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>Hotel</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.Hotel?.Name}</td></tr>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>New Check-In</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.CheckInDate:dddd, MMMM d, yyyy}</td></tr>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>New Check-Out</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.CheckOutDate:dddd, MMMM d, yyyy}</td></tr>
    <tr><td style='padding:8px;font-weight:bold'>Total Amount</td><td style='padding:8px'>${r.TotalAmount:F2}</td></tr>
  </table>
  <p>If you did not make this change, please contact us immediately.</p>
</div>
<div style='background:#f8f9fa;padding:16px;text-align:center;font-size:12px;color:#666'>
  <p>Hotel Reservation System &bull; This is an automated message, please do not reply.</p>
</div>
</body></html>";

    private static string BuildCancellationEmailBody(Reservation r, Guest guest, string reason) => $@"
<html><body style='font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto'>
<div style='background:#dc3545;padding:20px;text-align:center'>
  <h1 style='color:#fff;margin:0'>Reservation Cancelled</h1>
</div>
<div style='padding:24px'>
  <p>Dear {guest.FirstName},</p>
  <p>Your reservation has been cancelled as requested.</p>
  <table style='width:100%;border-collapse:collapse;margin:16px 0'>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>Booking Reference</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.BookingReference}</td></tr>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>Hotel</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.Hotel?.Name}</td></tr>
    <tr><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold'>Original Check-In</td><td style='padding:8px;border-bottom:1px solid #eee'>{r.CheckInDate:dddd, MMMM d, yyyy}</td></tr>
    <tr><td style='padding:8px;font-weight:bold'>Reason</td><td style='padding:8px'>{reason}</td></tr>
  </table>
  <p>We hope to welcome you again in the future. If you have any questions, please contact our support team.</p>
</div>
<div style='background:#f8f9fa;padding:16px;text-align:center;font-size:12px;color:#666'>
  <p>Hotel Reservation System &bull; This is an automated message, please do not reply.</p>
</div>
</body></html>";
}
