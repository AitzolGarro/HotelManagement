using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces;

public interface IGuestPortalService
{
    // Authentication
    Task<GuestLoginResponse> LoginAsync(GuestLoginRequest request);

    // Profile management
    Task<GuestProfileDto> GetGuestProfileAsync(int guestId);
    Task<GuestProfileDto> UpdateGuestProfileAsync(int guestId, GuestProfileDto request);
    Task<GuestNotificationPreferencesDto> GetNotificationPreferencesAsync(int guestId);
    Task<GuestNotificationPreferencesDto> UpdateNotificationPreferencesAsync(int guestId, GuestNotificationPreferencesDto prefs);

    // Reservation management
    Task<IEnumerable<ReservationDto>> GetMyReservationsAsync(int guestId);
    Task<ReservationDto?> GetMyReservationByIdAsync(int guestId, int reservationId);
    Task<ReservationDto> ModifyReservationAsync(int guestId, int reservationId, UpdateReservationDatesRequest request);
    Task<bool> CancelReservationAsync(int guestId, int reservationId, string reason);
    Task<ReservationDto> SubmitSpecialRequestAsync(int guestId, int reservationId, string specialRequests);

    // Notifications
    Task SendBookingConfirmationEmailAsync(int reservationId);
    Task SendCheckInReminderAsync(int reservationId);
    Task SendCheckOutReminderAsync(int reservationId);
    Task SendModificationConfirmationAsync(int reservationId);
    Task ProcessUpcomingRemindersAsync();
}
