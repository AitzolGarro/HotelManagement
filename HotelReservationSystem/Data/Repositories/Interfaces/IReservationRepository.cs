using HotelReservationSystem.Models;

namespace HotelReservationSystem.Data.Repositories.Interfaces;

public interface IReservationRepository : IRepository<Reservation>
{
    Task<IEnumerable<Reservation>> GetReservationsByDateRangeAsync(DateTime fromDate, DateTime toDate, int? hotelId = null);
    Task<IEnumerable<Reservation>> GetReservationsByRoomAsync(int roomId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<Reservation>> GetReservationsByGuestAsync(int guestId);
    Task<IEnumerable<Reservation>> GetReservationsByStatusAsync(ReservationStatus status, int? hotelId = null);
    Task<Reservation?> GetReservationByBookingReferenceAsync(string bookingReference);
    Task<IEnumerable<Reservation>> GetConflictingReservationsAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null);
    Task<IEnumerable<Reservation>> GetCheckInsForDateAsync(DateTime date, int? hotelId = null);
    Task<IEnumerable<Reservation>> GetCheckOutsForDateAsync(DateTime date, int? hotelId = null);
    Task<bool> HasConflictingReservationsAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null);
    Task<bool> HasActiveReservationsForHotelAsync(int hotelId);
    Task<bool> HasActiveReservationsForRoomAsync(int roomId);
}