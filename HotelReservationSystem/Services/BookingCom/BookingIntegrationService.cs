using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.BookingCom;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using System.Globalization;

namespace HotelReservationSystem.Services.BookingCom;

public interface IBookingIntegrationService
{
    Task SyncReservationsAsync(CancellationToken cancellationToken = default);
    Task SyncReservationsForHotelAsync(int hotelId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task PushAvailabilityUpdateAsync(int roomId, DateTime date, int availableCount, CancellationToken cancellationToken = default);
    Task<BookingComReservation?> FetchReservationAsync(string bookingReference, CancellationToken cancellationToken = default);
    Task HandleWebhookAsync(string xmlPayload, CancellationToken cancellationToken = default);
    Task<ReservationDto> ProcessExternalReservationAsync(BookingComReservation externalReservation, CancellationToken cancellationToken = default);
}

public class BookingIntegrationService : IBookingIntegrationService
{
    private readonly IBookingComHttpClient _httpClient;
    private readonly IXmlSerializationService _xmlSerializer;
    private readonly IBookingComAuthenticationService _authService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReservationService _reservationService;
    private readonly IPropertyService _propertyService;
    private readonly ILogger<BookingIntegrationService> _logger;

    public BookingIntegrationService(
        IBookingComHttpClient httpClient,
        IXmlSerializationService xmlSerializer,
        IBookingComAuthenticationService authService,
        IUnitOfWork unitOfWork,
        IReservationService reservationService,
        IPropertyService propertyService,
        ILogger<BookingIntegrationService> logger)
    {
        _httpClient = httpClient;
        _xmlSerializer = xmlSerializer;
        _authService = authService;
        _unitOfWork = unitOfWork;
        _reservationService = reservationService;
        _propertyService = propertyService;
        _logger = logger;
    }

    public async Task SyncReservationsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting full reservation synchronization");

        try
        {
            // Get all active hotels
            var hotels = await _unitOfWork.Hotels.GetActiveHotelsAsync();
            
            foreach (var hotel in hotels)
            {
                await SyncReservationsForHotelAsync(hotel.Id, cancellationToken: cancellationToken);
            }

            _logger.LogInformation("Completed full reservation synchronization for {HotelCount} hotels", hotels.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete full reservation synchronization");
            throw;
        }
    }

    public async Task SyncReservationsForHotelAsync(int hotelId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting reservation synchronization for hotel {HotelId}", hotelId);

        try
        {
            // Validate hotel exists
            await _propertyService.ValidateHotelExistsAsync(hotelId);

            // Set default date range if not provided (last 30 days to next 365 days)
            fromDate ??= DateTime.Today.AddDays(-30);
            toDate ??= DateTime.Today.AddDays(365);

            // Create sync request
            var syncRequest = new ReservationSyncRequest
            {
                Authentication = _authService.GetAuthentication(),
                ReservationData = new ReservationSyncData
                {
                    HotelId = hotelId,
                    FromDate = fromDate.Value.ToString("yyyy-MM-dd"),
                    ToDate = toDate.Value.ToString("yyyy-MM-dd")
                }
            };

            var requestXml = _xmlSerializer.Serialize(syncRequest);
            var response = await _httpClient.SendRequestAsync<ReservationSyncResponse>(
                "reservations/sync", requestXml, cancellationToken);

            if (response.Fault != null)
            {
                _logger.LogError("Booking.com API returned fault during sync: {Code} - {Message}", 
                    response.Fault.Code, response.Fault.Message);
                throw new BookingComApiException($"Sync failed: {response.Fault.Message}");
            }

            _logger.LogInformation("Retrieved {ReservationCount} reservations from Booking.com for hotel {HotelId}", 
                response.Reservations.Count, hotelId);

            // Process each reservation
            var processedCount = 0;
            var errorCount = 0;

            foreach (var externalReservation in response.Reservations)
            {
                try
                {
                    await ProcessExternalReservationAsync(externalReservation, cancellationToken);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process external reservation {BookingReference}", 
                        externalReservation.Id);
                    errorCount++;
                }
            }

            _logger.LogInformation("Completed reservation synchronization for hotel {HotelId}. Processed: {ProcessedCount}, Errors: {ErrorCount}", 
                hotelId, processedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to synchronize reservations for hotel {HotelId}", hotelId);
            throw;
        }
    }

    public async Task PushAvailabilityUpdateAsync(int roomId, DateTime date, int availableCount, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Pushing availability update for room {RoomId} on {Date}: {AvailableCount}", 
            roomId, date, availableCount);

        try
        {
            // Get room and hotel information
            var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
            if (room == null)
            {
                throw new ArgumentException($"Room with ID {roomId} not found");
            }

            // Get room rate (simplified - in real implementation, this might come from a rate management system)
            var baseRate = room.BaseRate;

            // Create availability update request
            var updateRequest = new AvailabilityUpdateRequest
            {
                Authentication = _authService.GetAuthentication(),
                AvailabilityData = new AvailabilityUpdateData
                {
                    HotelId = room.HotelId,
                    Rooms = new List<RoomAvailability>
                    {
                        new RoomAvailability
                        {
                            Id = roomId,
                            Date = date.ToString("yyyy-MM-dd"),
                            Available = availableCount,
                            Price = baseRate
                        }
                    }
                }
            };

            var requestXml = _xmlSerializer.Serialize(updateRequest);
            var response = await _httpClient.SendRequestAsync<BookingComResponse>(
                "availability/update", requestXml, cancellationToken);

            if (response.Fault != null)
            {
                _logger.LogError("Booking.com API returned fault during availability update: {Code} - {Message}", 
                    response.Fault.Code, response.Fault.Message);
                throw new BookingComApiException($"Availability update failed: {response.Fault.Message}");
            }

            _logger.LogInformation("Successfully pushed availability update for room {RoomId} on {Date}", 
                roomId, date);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to push availability update for room {RoomId} on {Date}", 
                roomId, date);
            throw;
        }
    }

    public async Task<BookingComReservation?> FetchReservationAsync(string bookingReference, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching reservation {BookingReference} from Booking.com", bookingReference);

        try
        {
            // Create fetch request (simplified - actual API might have different endpoint)
            var fetchRequest = new BookingComRequest
            {
                Authentication = _authService.GetAuthentication()
            };

            var requestXml = _xmlSerializer.Serialize(fetchRequest);
            var response = await _httpClient.SendRequestAsync<ReservationSyncResponse>(
                $"reservations/{bookingReference}", requestXml, cancellationToken);

            if (response.Fault != null)
            {
                _logger.LogWarning("Booking.com API returned fault when fetching reservation {BookingReference}: {Code} - {Message}", 
                    bookingReference, response.Fault.Code, response.Fault.Message);
                return null;
            }

            var reservation = response.Reservations.FirstOrDefault();
            if (reservation != null)
            {
                _logger.LogInformation("Successfully fetched reservation {BookingReference}", bookingReference);
            }
            else
            {
                _logger.LogWarning("Reservation {BookingReference} not found on Booking.com", bookingReference);
            }

            return reservation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch reservation {BookingReference} from Booking.com", bookingReference);
            throw;
        }
    }

    public async Task HandleWebhookAsync(string xmlPayload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing Booking.com webhook notification");

        try
        {
            if (string.IsNullOrWhiteSpace(xmlPayload))
            {
                throw new ArgumentException("Webhook payload cannot be null or empty");
            }

            // Deserialize webhook notification
            var notification = _xmlSerializer.Deserialize<BookingComWebhookNotification>(xmlPayload);

            _logger.LogInformation("Processing webhook notification of type {NotificationType} at {Timestamp}", 
                notification.Type, notification.Timestamp);

            switch (notification.Type.ToLowerInvariant())
            {
                case "reservation_created":
                case "reservation_updated":
                    if (notification.Reservation != null)
                    {
                        await ProcessExternalReservationAsync(notification.Reservation, cancellationToken);
                    }
                    break;

                case "reservation_cancelled":
                    if (notification.Cancellation != null)
                    {
                        await ProcessReservationCancellationAsync(notification.Cancellation, cancellationToken);
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown webhook notification type: {NotificationType}", notification.Type);
                    break;
            }

            _logger.LogInformation("Successfully processed webhook notification");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process webhook notification. Payload: {Payload}", xmlPayload);
            throw;
        }
    }

    public async Task<ReservationDto> ProcessExternalReservationAsync(BookingComReservation externalReservation, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing external reservation {BookingReference}", externalReservation.Id);

        try
        {
            // Check if reservation already exists
            var existingReservation = await _reservationService.GetReservationByBookingReferenceAsync(externalReservation.Id);

            if (existingReservation != null)
            {
                // Update existing reservation
                return await UpdateExistingReservationAsync(existingReservation, externalReservation, cancellationToken);
            }
            else
            {
                // Create new reservation
                return await CreateNewReservationAsync(externalReservation, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process external reservation {BookingReference}", externalReservation.Id);
            throw;
        }
    }

    private async Task<ReservationDto> CreateNewReservationAsync(BookingComReservation externalReservation, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new reservation from external source {BookingReference}", externalReservation.Id);

        // Parse dates
        if (!DateTime.TryParseExact(externalReservation.CheckIn, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var checkInDate))
        {
            throw new ArgumentException($"Invalid check-in date format: {externalReservation.CheckIn}");
        }

        if (!DateTime.TryParseExact(externalReservation.CheckOut, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var checkOutDate))
        {
            throw new ArgumentException($"Invalid check-out date format: {externalReservation.CheckOut}");
        }

        // Find or create guest
        var guest = await FindOrCreateGuestAsync(externalReservation, cancellationToken);

        // Validate room exists and is available
        await _propertyService.ValidateRoomExistsAsync(externalReservation.RoomId);
        
        // Check availability (allow overbooking for external reservations but log warning)
        var isAvailable = await _reservationService.CheckAvailabilityAsync(externalReservation.RoomId, checkInDate, checkOutDate);
        if (!isAvailable)
        {
            _logger.LogWarning("Room {RoomId} is not available for dates {CheckIn} to {CheckOut} but creating external reservation anyway", 
                externalReservation.RoomId, checkInDate, checkOutDate);
        }

        // Map external status to internal status
        var status = MapExternalStatusToInternal(externalReservation.Status);

        // Create reservation request
        var createRequest = new CreateReservationRequest
        {
            HotelId = externalReservation.HotelId,
            RoomId = externalReservation.RoomId,
            GuestId = guest.Id,
            BookingReference = externalReservation.Id,
            Source = ReservationSource.Booking,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            NumberOfGuests = externalReservation.NumberOfGuests,
            TotalAmount = externalReservation.TotalAmount,
            Status = status,
            SpecialRequests = externalReservation.SpecialRequests,
            InternalNotes = $"Imported from Booking.com on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"
        };

        var reservation = await _reservationService.CreateReservationAsync(createRequest);
        
        _logger.LogInformation("Successfully created reservation {ReservationId} from external source {BookingReference}", 
            reservation.Id, externalReservation.Id);

        return reservation;
    }

    private async Task<ReservationDto> UpdateExistingReservationAsync(ReservationDto existingReservation, BookingComReservation externalReservation, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating existing reservation {ReservationId} from external source {BookingReference}", 
            existingReservation.Id, externalReservation.Id);

        // Parse dates
        if (!DateTime.TryParseExact(externalReservation.CheckIn, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var checkInDate))
        {
            throw new ArgumentException($"Invalid check-in date format: {externalReservation.CheckIn}");
        }

        if (!DateTime.TryParseExact(externalReservation.CheckOut, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var checkOutDate))
        {
            throw new ArgumentException($"Invalid check-out date format: {externalReservation.CheckOut}");
        }

        // Check if update is needed
        var externalStatus = MapExternalStatusToInternal(externalReservation.Status);
        var needsUpdate = existingReservation.CheckInDate != checkInDate ||
                         existingReservation.CheckOutDate != checkOutDate ||
                         existingReservation.NumberOfGuests != externalReservation.NumberOfGuests ||
                         existingReservation.TotalAmount != externalReservation.TotalAmount ||
                         existingReservation.Status != externalStatus ||
                         existingReservation.SpecialRequests != externalReservation.SpecialRequests;

        if (!needsUpdate)
        {
            _logger.LogInformation("No updates needed for reservation {ReservationId}", existingReservation.Id);
            return existingReservation;
        }

        // Update reservation
        var updateRequest = new UpdateReservationRequest
        {
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            NumberOfGuests = externalReservation.NumberOfGuests,
            TotalAmount = externalReservation.TotalAmount,
            Status = externalStatus,
            SpecialRequests = externalReservation.SpecialRequests,
            InternalNotes = $"{existingReservation.InternalNotes}\nUpdated from Booking.com on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"
        };

        var updatedReservation = await _reservationService.UpdateReservationAsync(existingReservation.Id, updateRequest);
        
        _logger.LogInformation("Successfully updated reservation {ReservationId} from external source", existingReservation.Id);

        return updatedReservation;
    }

    private async Task ProcessReservationCancellationAsync(BookingComCancellation cancellation, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing reservation cancellation for {BookingReference}", cancellation.ReservationId);

        try
        {
            var existingReservation = await _reservationService.GetReservationByBookingReferenceAsync(cancellation.ReservationId);
            if (existingReservation == null)
            {
                _logger.LogWarning("Cannot cancel reservation {BookingReference} - not found in local database", 
                    cancellation.ReservationId);
                return;
            }

            if (existingReservation.Status == ReservationStatus.Cancelled)
            {
                _logger.LogInformation("Reservation {BookingReference} is already cancelled", cancellation.ReservationId);
                return;
            }

            var cancelRequest = new CancelReservationRequest
            {
                Reason = $"Cancelled on Booking.com: {cancellation.Reason}"
            };

            await _reservationService.CancelReservationAsync(existingReservation.Id, cancelRequest);
            
            _logger.LogInformation("Successfully cancelled reservation {ReservationId} from external cancellation", 
                existingReservation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process reservation cancellation for {BookingReference}", 
                cancellation.ReservationId);
            throw;
        }
    }

    private async Task<Guest> FindOrCreateGuestAsync(BookingComReservation externalReservation, CancellationToken cancellationToken)
    {
        // Try to find existing guest by email
        Guest? guest = null;
        if (!string.IsNullOrEmpty(externalReservation.GuestEmail))
        {
            guest = await _unitOfWork.Guests.GetGuestByEmailAsync(externalReservation.GuestEmail);
        }

        if (guest != null)
        {
            // Update guest information if needed
            var needsUpdate = false;
            
            if (!string.IsNullOrEmpty(externalReservation.GuestPhone) && guest.Phone != externalReservation.GuestPhone)
            {
                guest.Phone = externalReservation.GuestPhone;
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                guest.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Guests.Update(guest);
                await _unitOfWork.SaveChangesAsync();
            }

            return guest;
        }

        // Create new guest
        var guestNames = ParseGuestName(externalReservation.GuestName);
        guest = new Guest
        {
            FirstName = guestNames.firstName,
            LastName = guestNames.lastName,
            Email = externalReservation.GuestEmail,
            Phone = externalReservation.GuestPhone,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Guests.AddAsync(guest);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created new guest {GuestId} for external reservation", guest.Id);
        return guest;
    }

    private static (string firstName, string lastName) ParseGuestName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return ("Unknown", "Guest");
        }

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return (parts[0], "");
        }

        var firstName = parts[0];
        var lastName = string.Join(" ", parts.Skip(1));
        return (firstName, lastName);
    }

    private static ReservationStatus MapExternalStatusToInternal(string externalStatus)
    {
        return externalStatus.ToLowerInvariant() switch
        {
            "confirmed" => ReservationStatus.Confirmed,
            "pending" => ReservationStatus.Pending,
            "cancelled" => ReservationStatus.Cancelled,
            "checked_in" => ReservationStatus.CheckedIn,
            "checked_out" => ReservationStatus.CheckedOut,
            "no_show" => ReservationStatus.NoShow,
            _ => ReservationStatus.Confirmed // Default to confirmed for unknown statuses
        };
    }
}