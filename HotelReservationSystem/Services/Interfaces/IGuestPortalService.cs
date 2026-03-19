using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces;

public interface IGuestPortalService
{
    Task<GuestLoginResponse> LoginAsync(GuestLoginRequest request);
    Task<GuestProfileDto> GetGuestProfileAsync(int guestId);
    Task<GuestProfileDto> UpdateGuestProfileAsync(int guestId, GuestProfileDto request);
    Task<IEnumerable<ReservationDto>> GetMyReservationsAsync(int guestId);
    Task<ReservationDto> ModifyReservationAsync(int guestId, int reservationId, UpdateReservationDatesRequest request);
    Task<bool> CancelReservationAsync(int guestId, int reservationId, string reason);
}