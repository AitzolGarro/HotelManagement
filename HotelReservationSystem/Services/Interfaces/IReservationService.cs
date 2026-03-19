using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces;

public interface IReservationService
{
    // Reservation CRUD operations
    Task<ReservationDto> CreateReservationAsync(CreateReservationRequest request);
    Task<ReservationDto> CreateManualReservationAsync(CreateManualReservationRequest request);
    Task<ReservationDto?> GetReservationByIdAsync(int id);
    Task<PagedResultDto<ReservationDto>> GetPagedReservationsAsync(DateTime? from, DateTime? to, int? hotelId, ReservationStatus? status, int? roomId, int pageNumber, int pageSize);
    Task<PagedResultDto<ReservationDto>> SearchReservationsAsync(ReservationSearchCriteria criteria, int pageNumber, int pageSize);
    Task<IEnumerable<ReservationDto>> GetReservationsByDateRangeAsync(DateTime from, DateTime to, int? hotelId = null);
    Task<IEnumerable<ReservationDto>> GetReservationsByRoomAsync(int roomId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<ReservationDto>> GetReservationsByGuestAsync(int guestId);
    Task<IEnumerable<ReservationDto>> GetReservationsByStatusAsync(ReservationStatus status, int? hotelId = null);
    Task<ReservationDto?> GetReservationByBookingReferenceAsync(string bookingReference);
    Task<ReservationDto> UpdateReservationAsync(int id, UpdateReservationRequest request);
    Task<ReservationDto> UpdateReservationDatesAsync(int id, UpdateReservationDatesRequest request);
    Task<bool> CancelReservationAsync(int id, CancelReservationRequest request);

    // Availability and conflict detection
    Task<bool> CheckAvailabilityAsync(AvailabilityCheckRequest request);
    Task<bool> CheckAvailabilityAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null);
    Task<IEnumerable<ConflictDto>> DetectConflictsAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null);

    // Status management
    Task<ReservationDto> UpdateReservationStatusAsync(int id, ReservationStatus status);
    Task<ReservationDto> CheckInReservationAsync(int id);
    Task<ReservationDto> CheckOutReservationAsync(int id);

    // Daily operations
    Task<IEnumerable<ReservationDto>> GetCheckInsForDateAsync(DateTime date, int? hotelId = null);
    Task<IEnumerable<ReservationDto>> GetCheckOutsForDateAsync(DateTime date, int? hotelId = null);

    // Validation methods
    Task ValidateReservationExistsAsync(int reservationId);
    Task ValidateReservationDatesAsync(DateTime checkIn, DateTime checkOut);
    Task ValidateRoomCapacityAsync(int roomId, int numberOfGuests);
    Task ValidateNoConflictsAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null);
}