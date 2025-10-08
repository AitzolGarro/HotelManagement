using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Exceptions;

namespace HotelReservationSystem.Services;

public class ReservationService : IReservationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPropertyService _propertyService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(IUnitOfWork unitOfWork, IPropertyService propertyService, INotificationService notificationService, ILogger<ReservationService> logger)
    {
        _unitOfWork = unitOfWork;
        _propertyService = propertyService;
        _notificationService = notificationService;
        _logger = logger;
    }

    // Reservation CRUD operations
    public async Task<ReservationDto> CreateReservationAsync(CreateReservationRequest request)
    {
        _logger.LogInformation("Creating new reservation for guest {GuestId} in room {RoomId}", 
            request.GuestId, request.RoomId);

        // Validate request
        await ValidateReservationDatesAsync(request.CheckInDate, request.CheckOutDate);
        await _propertyService.ValidateHotelExistsAsync(request.HotelId);
        await _propertyService.ValidateRoomExistsAsync(request.RoomId);
        await ValidateRoomCapacityAsync(request.RoomId, request.NumberOfGuests);
        await ValidateNoConflictsAsync(request.RoomId, request.CheckInDate, request.CheckOutDate);

        // Validate guest exists
        var guest = await _unitOfWork.Guests.GetByIdAsync(request.GuestId);
        if (guest == null)
        {
            throw new ArgumentException($"Guest with ID {request.GuestId} not found");
        }

        // Validate booking reference uniqueness if provided
        if (!string.IsNullOrEmpty(request.BookingReference))
        {
            var existingReservation = await _unitOfWork.Reservations.GetReservationByBookingReferenceAsync(request.BookingReference);
            if (existingReservation != null)
            {
                throw new ReservationConflictException($"Booking reference {request.BookingReference} already exists");
            }
        }

        var reservation = new Reservation
        {
            HotelId = request.HotelId,
            RoomId = request.RoomId,
            GuestId = request.GuestId,
            BookingReference = request.BookingReference,
            Source = request.Source,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            NumberOfGuests = request.NumberOfGuests,
            TotalAmount = request.TotalAmount,
            Status = request.Status,
            SpecialRequests = request.SpecialRequests,
            InternalNotes = request.InternalNotes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Reservations.AddAsync(reservation);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Reservation created with ID {ReservationId}", reservation.Id);
        
        // Send SignalR notification
        await _notificationService.NotifyReservationCreatedAsync(reservation.Id, reservation.HotelId);
        
        // Load full reservation data for DTO
        var fullReservation = await _unitOfWork.Reservations.GetByIdAsync(reservation.Id);
        return await MapToReservationDto(fullReservation!);
    }

    public async Task<ReservationDto> CreateManualReservationAsync(CreateManualReservationRequest request)
    {
        _logger.LogInformation("Creating manual reservation for guest {GuestFirstName} {GuestLastName} in room {RoomId}", 
            request.GuestFirstName, request.GuestLastName, request.RoomId);

        // Validate request
        await ValidateReservationDatesAsync(request.CheckInDate, request.CheckOutDate);
        await _propertyService.ValidateHotelExistsAsync(request.HotelId);
        await _propertyService.ValidateRoomExistsAsync(request.RoomId);
        await ValidateRoomCapacityAsync(request.RoomId, request.NumberOfGuests);
        await ValidateNoConflictsAsync(request.RoomId, request.CheckInDate, request.CheckOutDate);

        // Validate booking reference uniqueness if provided
        if (!string.IsNullOrEmpty(request.BookingReference))
        {
            var existingReservation = await _unitOfWork.Reservations.GetReservationByBookingReferenceAsync(request.BookingReference);
            if (existingReservation != null)
            {
                throw new ReservationConflictException($"Booking reference {request.BookingReference} already exists");
            }
        }

        // Create or find guest
        Guest guest;
        var existingGuest = !string.IsNullOrEmpty(request.GuestEmail) 
            ? await _unitOfWork.Guests.GetGuestByEmailAsync(request.GuestEmail)
            : null;
        
        if (existingGuest != null)
        {
            // Update existing guest information if provided
            if (!string.IsNullOrEmpty(request.GuestPhone) && existingGuest.Phone != request.GuestPhone)
            {
                existingGuest.Phone = request.GuestPhone;
            }
            if (!string.IsNullOrEmpty(request.GuestAddress) && existingGuest.Address != request.GuestAddress)
            {
                existingGuest.Address = request.GuestAddress;
            }
            if (!string.IsNullOrEmpty(request.GuestDocumentNumber) && existingGuest.DocumentNumber != request.GuestDocumentNumber)
            {
                existingGuest.DocumentNumber = request.GuestDocumentNumber;
            }
            existingGuest.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Guests.Update(existingGuest);
            guest = existingGuest;
        }
        else
        {
            // Create new guest
            guest = new Guest
            {
                FirstName = request.GuestFirstName,
                LastName = request.GuestLastName,
                Email = request.GuestEmail,
                Phone = request.GuestPhone,
                Address = request.GuestAddress,
                DocumentNumber = request.GuestDocumentNumber,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Guests.AddAsync(guest);
            await _unitOfWork.SaveChangesAsync(); // Save to get the guest ID
        }

        // Create reservation
        var reservation = new Reservation
        {
            HotelId = request.HotelId,
            RoomId = request.RoomId,
            GuestId = guest.Id,
            BookingReference = request.BookingReference ?? $"MAN{DateTime.UtcNow:yyyyMMddHHmmss}",
            Source = ReservationSource.Manual,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            NumberOfGuests = request.NumberOfGuests,
            TotalAmount = request.TotalAmount,
            Status = request.Status,
            SpecialRequests = request.SpecialRequests,
            InternalNotes = request.InternalNotes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Reservations.AddAsync(reservation);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Manual reservation created with ID {ReservationId} for guest {GuestId}", 
            reservation.Id, guest.Id);
        
        // Send SignalR notification
        await _notificationService.NotifyReservationCreatedAsync(reservation.Id, reservation.HotelId);
        
        // Load full reservation data for DTO
        var fullReservation = await _unitOfWork.Reservations.GetByIdAsync(reservation.Id);
        return await MapToReservationDto(fullReservation!);
    }

    public async Task<ReservationDto?> GetReservationByIdAsync(int id)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdAsync(id);
        return reservation != null ? await MapToReservationDto(reservation) : null;
    }

    public async Task<IEnumerable<ReservationDto>> GetReservationsByDateRangeAsync(DateTime from, DateTime to, int? hotelId = null)
    {
        if (from >= to)
        {
            throw new InvalidDateRangeException("From date must be before to date");
        }

        var reservations = await _unitOfWork.Reservations.GetReservationsByDateRangeAsync(from, to, hotelId);
        var result = new List<ReservationDto>();
        
        foreach (var reservation in reservations)
        {
            result.Add(await MapToReservationDto(reservation));
        }
        
        return result;
    }

    public async Task<IEnumerable<ReservationDto>> GetReservationsByRoomAsync(int roomId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        await _propertyService.ValidateRoomExistsAsync(roomId);
        
        var reservations = await _unitOfWork.Reservations.GetReservationsByRoomAsync(roomId, fromDate, toDate);
        var result = new List<ReservationDto>();
        
        foreach (var reservation in reservations)
        {
            result.Add(await MapToReservationDto(reservation));
        }
        
        return result;
    }

    public async Task<IEnumerable<ReservationDto>> GetReservationsByGuestAsync(int guestId)
    {
        var guest = await _unitOfWork.Guests.GetByIdAsync(guestId);
        if (guest == null)
        {
            throw new ArgumentException($"Guest with ID {guestId} not found");
        }
        
        var reservations = await _unitOfWork.Reservations.GetReservationsByGuestAsync(guestId);
        var result = new List<ReservationDto>();
        
        foreach (var reservation in reservations)
        {
            result.Add(await MapToReservationDto(reservation));
        }
        
        return result;
    }

    public async Task<IEnumerable<ReservationDto>> GetReservationsByStatusAsync(ReservationStatus status, int? hotelId = null)
    {
        var reservations = await _unitOfWork.Reservations.GetReservationsByStatusAsync(status, hotelId);
        var result = new List<ReservationDto>();
        
        foreach (var reservation in reservations)
        {
            result.Add(await MapToReservationDto(reservation));
        }
        
        return result;
    }

    public async Task<ReservationDto?> GetReservationByBookingReferenceAsync(string bookingReference)
    {
        if (string.IsNullOrEmpty(bookingReference))
        {
            throw new ArgumentException("Booking reference cannot be null or empty");
        }
        
        var reservation = await _unitOfWork.Reservations.GetReservationByBookingReferenceAsync(bookingReference);
        return reservation != null ? await MapToReservationDto(reservation) : null;
    }

    public async Task<ReservationDto> UpdateReservationAsync(int id, UpdateReservationRequest request)
    {
        _logger.LogInformation("Updating reservation {ReservationId}", id);

        await ValidateReservationExistsAsync(id);
        await ValidateReservationDatesAsync(request.CheckInDate, request.CheckOutDate);

        var reservation = await _unitOfWork.Reservations.GetByIdAsync(id);
        if (reservation == null)
        {
            throw new ReservationNotFoundException($"Reservation with ID {id} not found");
        }

        // Check if dates changed and validate no conflicts
        if (reservation.CheckInDate != request.CheckInDate || reservation.CheckOutDate != request.CheckOutDate)
        {
            await ValidateNoConflictsAsync(reservation.RoomId, request.CheckInDate, request.CheckOutDate, id);
        }

        // Validate room capacity if number of guests changed
        if (reservation.NumberOfGuests != request.NumberOfGuests)
        {
            await ValidateRoomCapacityAsync(reservation.RoomId, request.NumberOfGuests);
        }

        // Validate status transition
        ValidateStatusTransition(reservation.Status, request.Status);

        reservation.CheckInDate = request.CheckInDate;
        reservation.CheckOutDate = request.CheckOutDate;
        reservation.NumberOfGuests = request.NumberOfGuests;
        reservation.TotalAmount = request.TotalAmount;
        reservation.Status = request.Status;
        reservation.SpecialRequests = request.SpecialRequests;
        reservation.InternalNotes = request.InternalNotes;
        reservation.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Reservations.Update(reservation);
        await _unitOfWork.SaveChangesAsync();

        // Send SignalR notification
        await _notificationService.NotifyReservationUpdatedAsync(reservation.Id, reservation.HotelId);

        return await MapToReservationDto(reservation);
    }

    public async Task<bool> CancelReservationAsync(int id, CancelReservationRequest request)
    {
        _logger.LogInformation("Cancelling reservation {ReservationId} with reason: {Reason}", id, request.Reason);

        await ValidateReservationExistsAsync(id);

        var reservation = await _unitOfWork.Reservations.GetByIdAsync(id);
        if (reservation == null)
        {
            return false;
        }

        if (reservation.Status == ReservationStatus.Cancelled)
        {
            throw new InvalidReservationStatusException("Reservation is already cancelled");
        }

        if (reservation.Status == ReservationStatus.CheckedOut)
        {
            throw new InvalidReservationStatusException("Cannot cancel a checked-out reservation");
        }

        reservation.Status = ReservationStatus.Cancelled;
        reservation.InternalNotes = $"{reservation.InternalNotes}\nCancelled on {DateTime.UtcNow:yyyy-MM-dd HH:mm}: {request.Reason}";
        reservation.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Reservations.Update(reservation);

        await _unitOfWork.SaveChangesAsync();
        
        // Send SignalR notification
        await _notificationService.NotifyReservationCancelledAsync(reservation.Id, reservation.HotelId);
        
        return true;
    }

    // Availability and conflict detection
    public async Task<bool> CheckAvailabilityAsync(AvailabilityCheckRequest request)
    {
        await ValidateReservationDatesAsync(request.CheckInDate, request.CheckOutDate);
        await _propertyService.ValidateRoomExistsAsync(request.RoomId);
        
        return await CheckAvailabilityAsync(request.RoomId, request.CheckInDate, request.CheckOutDate, request.ExcludeReservationId);
    }

    public async Task<bool> CheckAvailabilityAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null)
    {
        return await _unitOfWork.Rooms.IsRoomAvailableAsync(roomId, checkIn, checkOut, excludeReservationId);
    }

    public async Task<IEnumerable<ConflictDto>> DetectConflictsAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null)
    {
        await _propertyService.ValidateRoomExistsAsync(roomId);
        await ValidateReservationDatesAsync(checkIn, checkOut);

        var conflictingReservations = await _unitOfWork.Reservations.GetConflictingReservationsAsync(roomId, checkIn, checkOut, excludeReservationId);
        var conflicts = new List<ConflictDto>();

        foreach (var reservation in conflictingReservations)
        {
            conflicts.Add(new ConflictDto
            {
                ReservationId = reservation.Id,
                BookingReference = reservation.BookingReference ?? "",
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                GuestName = $"{reservation.Guest?.FirstName} {reservation.Guest?.LastName}".Trim(),
                Status = reservation.Status,
                ConflictType = "DateOverlap",
                Description = $"Reservation overlaps with requested dates ({checkIn:yyyy-MM-dd} to {checkOut:yyyy-MM-dd})"
            });
        }

        return conflicts;
    }

    // Status management
    public async Task<ReservationDto> UpdateReservationStatusAsync(int id, ReservationStatus status)
    {
        _logger.LogInformation("Updating reservation {ReservationId} status to {Status}", id, status);

        await ValidateReservationExistsAsync(id);

        var reservation = await _unitOfWork.Reservations.GetByIdAsync(id);
        if (reservation == null)
        {
            throw new ReservationNotFoundException($"Reservation with ID {id} not found");
        }

        ValidateStatusTransition(reservation.Status, status);

        reservation.Status = status;
        reservation.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Reservations.Update(reservation);

        await _unitOfWork.SaveChangesAsync();
        
        // Send SignalR notification
        await _notificationService.NotifyReservationUpdatedAsync(reservation.Id, reservation.HotelId);
        
        return await MapToReservationDto(reservation);
    }

    public async Task<ReservationDto> CheckInReservationAsync(int id)
    {
        _logger.LogInformation("Checking in reservation {ReservationId}", id);

        await ValidateReservationExistsAsync(id);

        var reservation = await _unitOfWork.Reservations.GetByIdAsync(id);
        if (reservation == null)
        {
            throw new ReservationNotFoundException($"Reservation with ID {id} not found");
        }

        if (reservation.Status != ReservationStatus.Confirmed)
        {
            throw new InvalidReservationStatusException("Only confirmed reservations can be checked in");
        }

        if (reservation.CheckInDate.Date > DateTime.Today)
        {
            throw new InvalidReservationStatusException("Cannot check in before the check-in date");
        }

        reservation.Status = ReservationStatus.CheckedIn;
        reservation.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Reservations.Update(reservation);

        await _unitOfWork.SaveChangesAsync();
        
        // Send SignalR notification
        await _notificationService.NotifyReservationUpdatedAsync(reservation.Id, reservation.HotelId);
        
        return await MapToReservationDto(reservation);
    }

    public async Task<ReservationDto> CheckOutReservationAsync(int id)
    {
        _logger.LogInformation("Checking out reservation {ReservationId}", id);

        await ValidateReservationExistsAsync(id);

        var reservation = await _unitOfWork.Reservations.GetByIdAsync(id);
        if (reservation == null)
        {
            throw new ReservationNotFoundException($"Reservation with ID {id} not found");
        }

        if (reservation.Status != ReservationStatus.CheckedIn)
        {
            throw new InvalidReservationStatusException("Only checked-in reservations can be checked out");
        }

        reservation.Status = ReservationStatus.CheckedOut;
        reservation.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Reservations.Update(reservation);

        await _unitOfWork.SaveChangesAsync();
        
        // Send SignalR notification
        await _notificationService.NotifyReservationUpdatedAsync(reservation.Id, reservation.HotelId);
        
        return await MapToReservationDto(reservation);
    }

    // Daily operations
    public async Task<IEnumerable<ReservationDto>> GetCheckInsForDateAsync(DateTime date, int? hotelId = null)
    {
        var reservations = await _unitOfWork.Reservations.GetCheckInsForDateAsync(date, hotelId);
        var result = new List<ReservationDto>();
        
        foreach (var reservation in reservations)
        {
            result.Add(await MapToReservationDto(reservation));
        }
        
        return result;
    }

    public async Task<IEnumerable<ReservationDto>> GetCheckOutsForDateAsync(DateTime date, int? hotelId = null)
    {
        var reservations = await _unitOfWork.Reservations.GetCheckOutsForDateAsync(date, hotelId);
        var result = new List<ReservationDto>();
        
        foreach (var reservation in reservations)
        {
            result.Add(await MapToReservationDto(reservation));
        }
        
        return result;
    }

    // Validation methods
    public async Task ValidateReservationExistsAsync(int reservationId)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdAsync(reservationId);
        if (reservation == null)
        {
            throw new ReservationNotFoundException($"Reservation with ID {reservationId} not found");
        }
    }

    public Task ValidateReservationDatesAsync(DateTime checkIn, DateTime checkOut)
    {
        if (checkIn >= checkOut)
        {
            throw new InvalidDateRangeException("Check-in date must be before check-out date");
        }

        if (checkIn.Date < DateTime.Today)
        {
            throw new InvalidDateRangeException("Check-in date cannot be in the past");
        }

        if ((checkOut - checkIn).TotalDays > 365)
        {
            throw new InvalidDateRangeException("Reservation cannot exceed 365 days");
        }

        return Task.CompletedTask;
    }

    public async Task ValidateRoomCapacityAsync(int roomId, int numberOfGuests)
    {
        var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
        if (room == null)
        {
            throw new RoomNotFoundException($"Room with ID {roomId} not found");
        }

        if (numberOfGuests > room.Capacity)
        {
            throw new ArgumentException($"Number of guests ({numberOfGuests}) exceeds room capacity ({room.Capacity})");
        }
    }

    public async Task ValidateNoConflictsAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludeReservationId = null)
    {
        var hasConflicts = await _unitOfWork.Reservations.HasConflictingReservationsAsync(roomId, checkIn, checkOut, excludeReservationId);
        if (hasConflicts)
        {
            throw new ReservationConflictException($"Room {roomId} is not available for the requested dates");
        }
    }

    // Helper methods
    private static void ValidateStatusTransition(ReservationStatus currentStatus, ReservationStatus newStatus)
    {
        var validTransitions = new Dictionary<ReservationStatus, ReservationStatus[]>
        {
            [ReservationStatus.Pending] = new[] { ReservationStatus.Confirmed, ReservationStatus.Cancelled },
            [ReservationStatus.Confirmed] = new[] { ReservationStatus.CheckedIn, ReservationStatus.Cancelled, ReservationStatus.NoShow },
            [ReservationStatus.CheckedIn] = new[] { ReservationStatus.CheckedOut },
            [ReservationStatus.Cancelled] = new ReservationStatus[0], // No transitions allowed
            [ReservationStatus.CheckedOut] = new ReservationStatus[0], // No transitions allowed
            [ReservationStatus.NoShow] = new[] { ReservationStatus.Cancelled }
        };

        if (!validTransitions.ContainsKey(currentStatus) || !validTransitions[currentStatus].Contains(newStatus))
        {
            throw new InvalidReservationStatusException($"Invalid status transition from {currentStatus} to {newStatus}");
        }
    }

    private async Task<ReservationDto> MapToReservationDto(Reservation reservation)
    {
        // Load related entities if not already loaded
        if (reservation.Hotel == null)
        {
            reservation.Hotel = await _unitOfWork.Hotels.GetByIdAsync(reservation.HotelId);
        }
        if (reservation.Room == null)
        {
            reservation.Room = await _unitOfWork.Rooms.GetByIdAsync(reservation.RoomId);
        }
        if (reservation.Guest == null)
        {
            reservation.Guest = await _unitOfWork.Guests.GetByIdAsync(reservation.GuestId);
        }

        return new ReservationDto
        {
            Id = reservation.Id,
            HotelId = reservation.HotelId,
            RoomId = reservation.RoomId,
            GuestId = reservation.GuestId,
            BookingReference = reservation.BookingReference,
            Source = reservation.Source,
            CheckInDate = reservation.CheckInDate,
            CheckOutDate = reservation.CheckOutDate,
            NumberOfGuests = reservation.NumberOfGuests,
            TotalAmount = reservation.TotalAmount,
            Status = reservation.Status,
            SpecialRequests = reservation.SpecialRequests,
            InternalNotes = reservation.InternalNotes,
            CreatedAt = reservation.CreatedAt,
            UpdatedAt = reservation.UpdatedAt,
            HotelName = reservation.Hotel?.Name ?? "",
            RoomNumber = reservation.Room?.RoomNumber ?? "",
            GuestName = $"{reservation.Guest?.FirstName} {reservation.Guest?.LastName}".Trim(),
            GuestEmail = reservation.Guest?.Email ?? "",
            GuestPhone = reservation.Guest?.Phone ?? ""
        };
    }
}