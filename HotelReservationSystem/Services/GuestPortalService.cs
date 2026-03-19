using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Exceptions;

namespace HotelReservationSystem.Services;

public class GuestPortalService : IGuestPortalService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IReservationService _reservationService;
    private readonly ILogger<GuestPortalService> _logger;

    public GuestPortalService(
        IUnitOfWork unitOfWork, 
        IConfiguration configuration,
        IReservationService reservationService,
        ILogger<GuestPortalService> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _reservationService = reservationService;
        _logger = logger;
    }

    public async Task<GuestLoginResponse> LoginAsync(GuestLoginRequest request)
    {
        var reservation = await _unitOfWork.Reservations.GetReservationByBookingReferenceAsync(request.BookingReference);
        if (reservation == null || reservation.Guest?.Email?.ToLower() != request.Email.ToLower())
        {
            throw new UnauthorizedAccessException("Invalid email or booking reference");
        }

        var guest = reservation.Guest;
        var token = GenerateGuestJwtToken(guest);

        return new GuestLoginResponse
        {
            Token = token,
            Expires = DateTime.UtcNow.AddHours(24),
            Guest = MapToProfile(guest)
        };
    }

    public async Task<GuestProfileDto> GetGuestProfileAsync(int guestId)
    {
        var guest = await _unitOfWork.Guests.GetByIdAsync(guestId);
        if (guest == null) throw new Exception("Guest not found");

        return MapToProfile(guest);
    }

    public async Task<GuestProfileDto> UpdateGuestProfileAsync(int guestId, GuestProfileDto request)
    {
        var guest = await _unitOfWork.Guests.GetByIdAsync(guestId);
        if (guest == null) throw new Exception("Guest not found");

        guest.Phone = request.Phone;
        guest.Address = request.Address;
        guest.Nationality = request.Nationality;
        guest.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Guests.Update(guest);
        await _unitOfWork.SaveChangesAsync();

        return MapToProfile(guest);
    }

    public async Task<IEnumerable<ReservationDto>> GetMyReservationsAsync(int guestId)
    {
        return await _reservationService.GetReservationsByGuestAsync(guestId);
    }

    public async Task<ReservationDto> ModifyReservationAsync(int guestId, int reservationId, UpdateReservationDatesRequest request)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdAsync(reservationId);
        if (reservation == null || reservation.GuestId != guestId)
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this reservation.");
        }

        // Guests can only modify confirmed or pending reservations
        if (reservation.Status != ReservationStatus.Confirmed && reservation.Status != ReservationStatus.Pending)
        {
            throw new InvalidOperationException("This reservation cannot be modified.");
        }

        return await _reservationService.UpdateReservationDatesAsync(reservationId, request);
    }

    public async Task<bool> CancelReservationAsync(int guestId, int reservationId, string reason)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdAsync(reservationId);
        if (reservation == null || reservation.GuestId != guestId)
        {
            throw new UnauthorizedAccessException("You do not have permission to cancel this reservation.");
        }

        return await _reservationService.CancelReservationAsync(reservationId, new CancelReservationRequest { Reason = reason });
    }

    private string GenerateGuestJwtToken(Guest guest)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"] ?? "HotelReservationSystemSecretKeyForJWTTokenGeneration2024!");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, guest.Id.ToString()),
            new(ClaimTypes.Email, guest.Email ?? ""),
            new(ClaimTypes.Name, $"{guest.FirstName} {guest.LastName}"),
            new("role", "Guest")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static GuestProfileDto MapToProfile(Guest guest)
    {
        return new GuestProfileDto
        {
            Id = guest.Id,
            FirstName = guest.FirstName,
            LastName = guest.LastName,
            Email = guest.Email,
            Phone = guest.Phone,
            Address = guest.Address,
            Nationality = guest.Nationality,
            IsVip = guest.IsVip,
            LoyaltyPoints = 150 // Mock logic
        };
    }
}