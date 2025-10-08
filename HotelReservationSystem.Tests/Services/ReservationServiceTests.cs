using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HotelReservationSystem.Services;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Exceptions;
using HotelReservationSystem.Tests.TestConfiguration;

namespace HotelReservationSystem.Tests.Services;

public class ReservationServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IReservationRepository> _mockReservationRepository;
    private readonly Mock<IRoomRepository> _mockRoomRepository;
    private readonly Mock<IHotelRepository> _mockHotelRepository;
    private readonly Mock<IGuestRepository> _mockGuestRepository;
    private readonly Mock<IPropertyService> _mockPropertyService;
    private readonly Mock<ILogger<ReservationService>> _mockLogger;
    private readonly ReservationService _reservationService;

    public ReservationServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockReservationRepository = new Mock<IReservationRepository>();
        _mockRoomRepository = new Mock<IRoomRepository>();
        _mockHotelRepository = new Mock<IHotelRepository>();
        _mockGuestRepository = new Mock<IGuestRepository>();
        _mockPropertyService = new Mock<IPropertyService>();
        _mockLogger = new Mock<ILogger<ReservationService>>();

        _mockUnitOfWork.Setup(u => u.Reservations).Returns(_mockReservationRepository.Object);
        _mockUnitOfWork.Setup(u => u.Rooms).Returns(_mockRoomRepository.Object);
        _mockUnitOfWork.Setup(u => u.Hotels).Returns(_mockHotelRepository.Object);
        _mockUnitOfWork.Setup(u => u.Guests).Returns(_mockGuestRepository.Object);

        _reservationService = new ReservationService(_mockUnitOfWork.Object, _mockPropertyService.Object, _mockLogger.Object);
    }

    #region CreateReservationAsync Tests

    [Fact]
    [Trait(TestTraits.Category, TestCategories.Unit)]
    [Trait(TestTraits.Priority, "Critical")]
    [Trait(TestTraits.Feature, "Reservations")]
    [Trait(TestTraits.Duration, TestDurations.Fast)]
    public async Task CreateReservationAsync_ValidRequest_ReturnsReservationDto()
    {
        // Arrange
        var request = new CreateReservationRequest
        {
            HotelId = 1,
            RoomId = 1,
            GuestId = 1,
            BookingReference = "BK123",
            Source = ReservationSource.Manual,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            TotalAmount = 200.00m,
            Status = ReservationStatus.Confirmed
        };

        var guest = new Guest { Id = 1, FirstName = "John", LastName = "Doe" };
        var hotel = new Hotel { Id = 1, Name = "Test Hotel" };
        var room = new Room { Id = 1, RoomNumber = "101", Capacity = 4 };
        var reservation = new Reservation { Id = 1 };

        _mockPropertyService.Setup(s => s.ValidateHotelExistsAsync(1)).Returns(Task.CompletedTask);
        _mockPropertyService.Setup(s => s.ValidateRoomExistsAsync(1)).Returns(Task.CompletedTask);
        _mockGuestRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(guest);
        _mockReservationRepository.Setup(r => r.GetReservationByBookingReferenceAsync("BK123")).ReturnsAsync((Reservation?)null);
        _mockRoomRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);
        _mockReservationRepository.Setup(r => r.HasConflictingReservationsAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null)).ReturnsAsync(false);
        _mockReservationRepository.Setup(r => r.AddAsync(It.IsAny<Reservation>())).Callback<Reservation>(r => r.Id = 1);
        _mockReservationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _mockHotelRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
        _mockGuestRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(guest);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _reservationService.CreateReservationAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        _mockReservationRepository.Verify(r => r.AddAsync(It.IsAny<Reservation>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateReservationAsync_InvalidDates_ThrowsInvalidDateRangeException()
    {
        // Arrange
        var request = new CreateReservationRequest
        {
            CheckInDate = DateTime.Today.AddDays(3),
            CheckOutDate = DateTime.Today.AddDays(1) // Check-out before check-in
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDateRangeException>(
            () => _reservationService.CreateReservationAsync(request));
    }

    [Fact]
    public async Task CreateReservationAsync_PastCheckInDate_ThrowsInvalidDateRangeException()
    {
        // Arrange
        var request = new CreateReservationRequest
        {
            CheckInDate = DateTime.Today.AddDays(-1), // Past date
            CheckOutDate = DateTime.Today.AddDays(1)
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDateRangeException>(
            () => _reservationService.CreateReservationAsync(request));
    }

    [Fact]
    public async Task CreateReservationAsync_NonExistentGuest_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateReservationRequest
        {
            HotelId = 1,
            RoomId = 1,
            GuestId = 999,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2
        };

        _mockPropertyService.Setup(s => s.ValidateHotelExistsAsync(1)).Returns(Task.CompletedTask);
        _mockPropertyService.Setup(s => s.ValidateRoomExistsAsync(1)).Returns(Task.CompletedTask);
        _mockGuestRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Guest?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _reservationService.CreateReservationAsync(request));
    }

    [Fact]
    public async Task CreateReservationAsync_DuplicateBookingReference_ThrowsReservationConflictException()
    {
        // Arrange
        var request = new CreateReservationRequest
        {
            HotelId = 1,
            RoomId = 1,
            GuestId = 1,
            BookingReference = "EXISTING123",
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2
        };

        var existingReservation = new Reservation { Id = 1, BookingReference = "EXISTING123" };
        var guest = new Guest { Id = 1, FirstName = "John", LastName = "Doe" };

        _mockPropertyService.Setup(s => s.ValidateHotelExistsAsync(1)).Returns(Task.CompletedTask);
        _mockPropertyService.Setup(s => s.ValidateRoomExistsAsync(1)).Returns(Task.CompletedTask);
        _mockGuestRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(guest);
        _mockReservationRepository.Setup(r => r.GetReservationByBookingReferenceAsync("EXISTING123")).ReturnsAsync(existingReservation);

        // Act & Assert
        await Assert.ThrowsAsync<ReservationConflictException>(
            () => _reservationService.CreateReservationAsync(request));
    }

    [Fact]
    public async Task CreateReservationAsync_RoomConflict_ThrowsReservationConflictException()
    {
        // Arrange
        var request = new CreateReservationRequest
        {
            HotelId = 1,
            RoomId = 1,
            GuestId = 1,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2
        };

        var guest = new Guest { Id = 1, FirstName = "John", LastName = "Doe" };
        var room = new Room { Id = 1, RoomNumber = "101", Capacity = 4 };

        _mockPropertyService.Setup(s => s.ValidateHotelExistsAsync(1)).Returns(Task.CompletedTask);
        _mockPropertyService.Setup(s => s.ValidateRoomExistsAsync(1)).Returns(Task.CompletedTask);
        _mockGuestRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(guest);
        _mockRoomRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);
        _mockReservationRepository.Setup(r => r.HasConflictingReservationsAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ReservationConflictException>(
            () => _reservationService.CreateReservationAsync(request));
    }

    #endregion

    #region UpdateReservationAsync Tests

    [Fact]
    public async Task UpdateReservationAsync_ValidRequest_ReturnsUpdatedReservationDto()
    {
        // Arrange
        var existingReservation = new Reservation
        {
            Id = 1,
            HotelId = 1,
            RoomId = 1,
            GuestId = 1,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            Status = ReservationStatus.Confirmed
        };

        var updateRequest = new UpdateReservationRequest
        {
            CheckInDate = DateTime.Today.AddDays(2),
            CheckOutDate = DateTime.Today.AddDays(4),
            NumberOfGuests = 3,
            TotalAmount = 300.00m,
            Status = ReservationStatus.Confirmed
        };

        var room = new Room { Id = 1, RoomNumber = "101", Capacity = 4 };
        var hotel = new Hotel { Id = 1, Name = "Test Hotel" };
        var guest = new Guest { Id = 1, FirstName = "John", LastName = "Doe" };

        _mockReservationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingReservation);
        _mockRoomRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);
        _mockReservationRepository.Setup(r => r.HasConflictingReservationsAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1)).ReturnsAsync(false);
        _mockHotelRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
        _mockGuestRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(guest);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _reservationService.UpdateReservationAsync(1, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateRequest.CheckInDate, result.CheckInDate);
        Assert.Equal(updateRequest.CheckOutDate, result.CheckOutDate);
        Assert.Equal(updateRequest.NumberOfGuests, result.NumberOfGuests);
        Assert.Equal(updateRequest.TotalAmount, result.TotalAmount);
        
        _mockReservationRepository.Verify(r => r.Update(existingReservation), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateReservationAsync_NonExistentReservation_ThrowsReservationNotFoundException()
    {
        // Arrange
        var updateRequest = new UpdateReservationRequest
        {
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            Status = ReservationStatus.Confirmed
        };

        _mockReservationRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Reservation?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ReservationNotFoundException>(
            () => _reservationService.UpdateReservationAsync(999, updateRequest));
    }

    #endregion

    #region CancelReservationAsync Tests

    [Fact]
    public async Task CancelReservationAsync_ValidRequest_ReturnsTrue()
    {
        // Arrange
        var reservation = new Reservation
        {
            Id = 1,
            Status = ReservationStatus.Confirmed,
            InternalNotes = "Original notes"
        };

        var cancelRequest = new CancelReservationRequest
        {
            Reason = "Guest requested cancellation"
        };

        _mockReservationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _reservationService.CancelReservationAsync(1, cancelRequest);

        // Assert
        Assert.True(result);
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Contains(cancelRequest.Reason, reservation.InternalNotes);
        
        _mockReservationRepository.Verify(r => r.Update(reservation), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CancelReservationAsync_AlreadyCancelled_ThrowsInvalidReservationStatusException()
    {
        // Arrange
        var reservation = new Reservation
        {
            Id = 1,
            Status = ReservationStatus.Cancelled
        };

        var cancelRequest = new CancelReservationRequest
        {
            Reason = "Test reason"
        };

        _mockReservationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidReservationStatusException>(
            () => _reservationService.CancelReservationAsync(1, cancelRequest));
    }

    [Fact]
    public async Task CancelReservationAsync_CheckedOut_ThrowsInvalidReservationStatusException()
    {
        // Arrange
        var reservation = new Reservation
        {
            Id = 1,
            Status = ReservationStatus.CheckedOut
        };

        var cancelRequest = new CancelReservationRequest
        {
            Reason = "Test reason"
        };

        _mockReservationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidReservationStatusException>(
            () => _reservationService.CancelReservationAsync(1, cancelRequest));
    }

    #endregion

    #region CheckAvailabilityAsync Tests

    [Fact]
    public async Task CheckAvailabilityAsync_AvailableRoom_ReturnsTrue()
    {
        // Arrange
        var request = new AvailabilityCheckRequest
        {
            RoomId = 1,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3)
        };

        _mockPropertyService.Setup(s => s.ValidateRoomExistsAsync(1)).Returns(Task.CompletedTask);
        _mockRoomRepository.Setup(r => r.IsRoomAvailableAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null)).ReturnsAsync(true);

        // Act
        var result = await _reservationService.CheckAvailabilityAsync(request);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_UnavailableRoom_ReturnsFalse()
    {
        // Arrange
        var request = new AvailabilityCheckRequest
        {
            RoomId = 1,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3)
        };

        _mockPropertyService.Setup(s => s.ValidateRoomExistsAsync(1)).Returns(Task.CompletedTask);
        _mockRoomRepository.Setup(r => r.IsRoomAvailableAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null)).ReturnsAsync(false);

        // Act
        var result = await _reservationService.CheckAvailabilityAsync(request);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region DetectConflictsAsync Tests

    [Fact]
    public async Task DetectConflictsAsync_HasConflicts_ReturnsConflictDtos()
    {
        // Arrange
        var roomId = 1;
        var checkIn = DateTime.Today.AddDays(1);
        var checkOut = DateTime.Today.AddDays(3);

        var conflictingReservations = new List<Reservation>
        {
            new Reservation
            {
                Id = 1,
                CheckInDate = DateTime.Today.AddDays(2),
                CheckOutDate = DateTime.Today.AddDays(4),
                Status = ReservationStatus.Confirmed,
                Guest = new Guest { FirstName = "John", LastName = "Doe" }
            }
        };

        _mockPropertyService.Setup(s => s.ValidateRoomExistsAsync(roomId)).Returns(Task.CompletedTask);
        _mockReservationRepository.Setup(r => r.GetConflictingReservationsAsync(roomId, checkIn, checkOut, null))
            .ReturnsAsync(conflictingReservations);

        // Act
        var result = await _reservationService.DetectConflictsAsync(roomId, checkIn, checkOut);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var conflict = result.First();
        Assert.Equal(1, conflict.ReservationId);
        Assert.Equal("DateOverlap", conflict.ConflictType);
        Assert.Equal("John Doe", conflict.GuestName);
    }

    #endregion

    #region Status Management Tests

    [Fact]
    public async Task CheckInReservationAsync_ValidReservation_ReturnsUpdatedDto()
    {
        // Arrange
        var reservation = new Reservation
        {
            Id = 1,
            Status = ReservationStatus.Confirmed,
            CheckInDate = DateTime.Today
        };

        var hotel = new Hotel { Id = 1, Name = "Test Hotel" };
        var room = new Room { Id = 1, RoomNumber = "101" };
        var guest = new Guest { Id = 1, FirstName = "John", LastName = "Doe" };

        _mockReservationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _mockHotelRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(hotel);
        _mockRoomRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(room);
        _mockGuestRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(guest);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _reservationService.CheckInReservationAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ReservationStatus.CheckedIn, result.Status);
        Assert.Equal(ReservationStatus.CheckedIn, reservation.Status);
        
        _mockReservationRepository.Verify(r => r.Update(reservation), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CheckInReservationAsync_NotConfirmed_ThrowsInvalidReservationStatusException()
    {
        // Arrange
        var reservation = new Reservation
        {
            Id = 1,
            Status = ReservationStatus.Pending
        };

        _mockReservationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidReservationStatusException>(
            () => _reservationService.CheckInReservationAsync(1));
    }

    [Fact]
    public async Task CheckInReservationAsync_FutureCheckInDate_ThrowsInvalidReservationStatusException()
    {
        // Arrange
        var reservation = new Reservation
        {
            Id = 1,
            Status = ReservationStatus.Confirmed,
            CheckInDate = DateTime.Today.AddDays(1) // Future date
        };

        _mockReservationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidReservationStatusException>(
            () => _reservationService.CheckInReservationAsync(1));
    }

    [Fact]
    public async Task CheckOutReservationAsync_ValidReservation_ReturnsUpdatedDto()
    {
        // Arrange
        var reservation = new Reservation
        {
            Id = 1,
            Status = ReservationStatus.CheckedIn
        };

        var hotel = new Hotel { Id = 1, Name = "Test Hotel" };
        var room = new Room { Id = 1, RoomNumber = "101" };
        var guest = new Guest { Id = 1, FirstName = "John", LastName = "Doe" };

        _mockReservationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _mockHotelRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(hotel);
        _mockRoomRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(room);
        _mockGuestRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(guest);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _reservationService.CheckOutReservationAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ReservationStatus.CheckedOut, result.Status);
        Assert.Equal(ReservationStatus.CheckedOut, reservation.Status);
        
        _mockReservationRepository.Verify(r => r.Update(reservation), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CheckOutReservationAsync_NotCheckedIn_ThrowsInvalidReservationStatusException()
    {
        // Arrange
        var reservation = new Reservation
        {
            Id = 1,
            Status = ReservationStatus.Confirmed
        };

        _mockReservationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidReservationStatusException>(
            () => _reservationService.CheckOutReservationAsync(1));
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ValidateReservationDatesAsync_ValidDates_DoesNotThrow()
    {
        // Arrange
        var checkIn = DateTime.Today.AddDays(1);
        var checkOut = DateTime.Today.AddDays(3);

        // Act & Assert
        await _reservationService.ValidateReservationDatesAsync(checkIn, checkOut); // Should not throw
    }

    [Fact]
    public async Task ValidateReservationDatesAsync_InvalidDates_ThrowsInvalidDateRangeException()
    {
        // Arrange
        var checkIn = DateTime.Today.AddDays(3);
        var checkOut = DateTime.Today.AddDays(1); // Check-out before check-in

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDateRangeException>(
            () => _reservationService.ValidateReservationDatesAsync(checkIn, checkOut));
    }

    [Fact]
    public async Task ValidateReservationDatesAsync_PastCheckIn_ThrowsInvalidDateRangeException()
    {
        // Arrange
        var checkIn = DateTime.Today.AddDays(-1); // Past date
        var checkOut = DateTime.Today.AddDays(1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDateRangeException>(
            () => _reservationService.ValidateReservationDatesAsync(checkIn, checkOut));
    }

    [Fact]
    public async Task ValidateReservationDatesAsync_TooLongStay_ThrowsInvalidDateRangeException()
    {
        // Arrange
        var checkIn = DateTime.Today.AddDays(1);
        var checkOut = DateTime.Today.AddDays(400); // More than 365 days

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDateRangeException>(
            () => _reservationService.ValidateReservationDatesAsync(checkIn, checkOut));
    }

    [Fact]
    public async Task ValidateRoomCapacityAsync_ValidCapacity_DoesNotThrow()
    {
        // Arrange
        var room = new Room { Id = 1, Capacity = 4 };
        var numberOfGuests = 3;

        _mockRoomRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);

        // Act & Assert
        await _reservationService.ValidateRoomCapacityAsync(1, numberOfGuests); // Should not throw
    }

    [Fact]
    public async Task ValidateRoomCapacityAsync_ExceedsCapacity_ThrowsArgumentException()
    {
        // Arrange
        var room = new Room { Id = 1, Capacity = 2 };
        var numberOfGuests = 4;

        _mockRoomRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _reservationService.ValidateRoomCapacityAsync(1, numberOfGuests));
    }

    [Fact]
    public async Task ValidateNoConflictsAsync_NoConflicts_DoesNotThrow()
    {
        // Arrange
        var roomId = 1;
        var checkIn = DateTime.Today.AddDays(1);
        var checkOut = DateTime.Today.AddDays(3);

        _mockReservationRepository.Setup(r => r.HasConflictingReservationsAsync(roomId, checkIn, checkOut, null)).ReturnsAsync(false);

        // Act & Assert
        await _reservationService.ValidateNoConflictsAsync(roomId, checkIn, checkOut); // Should not throw
    }

    [Fact]
    public async Task ValidateNoConflictsAsync_HasConflicts_ThrowsReservationConflictException()
    {
        // Arrange
        var roomId = 1;
        var checkIn = DateTime.Today.AddDays(1);
        var checkOut = DateTime.Today.AddDays(3);

        _mockReservationRepository.Setup(r => r.HasConflictingReservationsAsync(roomId, checkIn, checkOut, null)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ReservationConflictException>(
            () => _reservationService.ValidateNoConflictsAsync(roomId, checkIn, checkOut));
    }

    #endregion

    #region Additional Edge Case Tests

    [Fact]
    public async Task CreateReservationAsync_ExceedsRoomCapacity_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateReservationRequest
        {
            HotelId = 1,
            RoomId = 1,
            GuestId = 1,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 5 // Exceeds capacity
        };

        var guest = new Guest { Id = 1, FirstName = "John", LastName = "Doe" };
        var room = new Room { Id = 1, RoomNumber = "101", Capacity = 2 }; // Capacity is 2

        _mockPropertyService.Setup(s => s.ValidateHotelExistsAsync(1)).Returns(Task.CompletedTask);
        _mockPropertyService.Setup(s => s.ValidateRoomExistsAsync(1)).Returns(Task.CompletedTask);
        _mockGuestRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(guest);
        _mockRoomRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _reservationService.CreateReservationAsync(request));
    }

    [Fact]
    public async Task UpdateReservationAsync_DateConflictAfterUpdate_ThrowsReservationConflictException()
    {
        // Arrange
        var existingReservation = new Reservation
        {
            Id = 1,
            HotelId = 1,
            RoomId = 1,
            GuestId = 1,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            Status = ReservationStatus.Confirmed
        };

        var updateRequest = new UpdateReservationRequest
        {
            CheckInDate = DateTime.Today.AddDays(5), // Different dates
            CheckOutDate = DateTime.Today.AddDays(7),
            NumberOfGuests = 2,
            TotalAmount = 300.00m,
            Status = ReservationStatus.Confirmed
        };

        var room = new Room { Id = 1, RoomNumber = "101", Capacity = 4 };

        _mockReservationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingReservation);
        _mockRoomRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);
        _mockReservationRepository.Setup(r => r.HasConflictingReservationsAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), 1)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ReservationConflictException>(
            () => _reservationService.UpdateReservationAsync(1, updateRequest));
    }

    [Fact]
    public async Task UpdateReservationAsync_InvalidStatusTransition_ThrowsInvalidReservationStatusException()
    {
        // Arrange
        var existingReservation = new Reservation
        {
            Id = 1,
            HotelId = 1,
            RoomId = 1,
            GuestId = 1,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            Status = ReservationStatus.CheckedOut // Already checked out
        };

        var updateRequest = new UpdateReservationRequest
        {
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            TotalAmount = 300.00m,
            Status = ReservationStatus.Confirmed // Invalid transition
        };

        _mockReservationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingReservation);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidReservationStatusException>(
            () => _reservationService.UpdateReservationAsync(1, updateRequest));
    }

    [Fact]
    public async Task GetReservationsByDateRangeAsync_InvalidDateRange_ThrowsInvalidDateRangeException()
    {
        // Arrange
        var fromDate = DateTime.Today.AddDays(5);
        var toDate = DateTime.Today.AddDays(1); // To date before from date

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDateRangeException>(
            () => _reservationService.GetReservationsByDateRangeAsync(fromDate, toDate));
    }

    [Fact]
    public async Task GetReservationsByRoomAsync_NonExistentRoom_ThrowsException()
    {
        // Arrange
        var roomId = 999;
        _mockPropertyService.Setup(s => s.ValidateRoomExistsAsync(roomId))
            .ThrowsAsync(new RoomNotFoundException($"Room with ID {roomId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<RoomNotFoundException>(
            () => _reservationService.GetReservationsByRoomAsync(roomId));
    }

    [Fact]
    public async Task GetReservationsByGuestAsync_NonExistentGuest_ThrowsArgumentException()
    {
        // Arrange
        var guestId = 999;
        _mockGuestRepository.Setup(r => r.GetByIdAsync(guestId)).ReturnsAsync((Guest?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _reservationService.GetReservationsByGuestAsync(guestId));
    }

    [Fact]
    public async Task GetReservationByBookingReferenceAsync_EmptyReference_ThrowsArgumentException()
    {
        // Arrange
        var emptyReference = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _reservationService.GetReservationByBookingReferenceAsync(emptyReference));
    }

    [Fact]
    public async Task GetReservationByBookingReferenceAsync_NullReference_ThrowsArgumentException()
    {
        // Arrange
        string? nullReference = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _reservationService.GetReservationByBookingReferenceAsync(nullReference!));
    }

    [Fact]
    public async Task DetectConflictsAsync_MultipleConflicts_ReturnsAllConflicts()
    {
        // Arrange
        var roomId = 1;
        var checkIn = DateTime.Today.AddDays(1);
        var checkOut = DateTime.Today.AddDays(5);

        var conflictingReservations = new List<Reservation>
        {
            new Reservation
            {
                Id = 1,
                CheckInDate = DateTime.Today.AddDays(2),
                CheckOutDate = DateTime.Today.AddDays(4),
                Status = ReservationStatus.Confirmed,
                BookingReference = "BK001",
                Guest = new Guest { FirstName = "John", LastName = "Doe" }
            },
            new Reservation
            {
                Id = 2,
                CheckInDate = DateTime.Today.AddDays(3),
                CheckOutDate = DateTime.Today.AddDays(6),
                Status = ReservationStatus.CheckedIn,
                BookingReference = "BK002",
                Guest = new Guest { FirstName = "Jane", LastName = "Smith" }
            }
        };

        _mockPropertyService.Setup(s => s.ValidateRoomExistsAsync(roomId)).Returns(Task.CompletedTask);
        _mockReservationRepository.Setup(r => r.GetConflictingReservationsAsync(roomId, checkIn, checkOut, null))
            .ReturnsAsync(conflictingReservations);

        // Act
        var result = await _reservationService.DetectConflictsAsync(roomId, checkIn, checkOut);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        
        var conflicts = result.ToList();
        Assert.Equal("John Doe", conflicts[0].GuestName);
        Assert.Equal("Jane Smith", conflicts[1].GuestName);
        Assert.Equal("BK001", conflicts[0].BookingReference);
        Assert.Equal("BK002", conflicts[1].BookingReference);
    }

    [Fact]
    public async Task UpdateReservationStatusAsync_ValidTransition_UpdatesStatus()
    {
        // Arrange
        var reservation = new Reservation
        {
            Id = 1,
            Status = ReservationStatus.Pending
        };

        var hotel = new Hotel { Id = 1, Name = "Test Hotel" };
        var room = new Room { Id = 1, RoomNumber = "101" };
        var guest = new Guest { Id = 1, FirstName = "John", LastName = "Doe" };

        _mockReservationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _mockHotelRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(hotel);
        _mockRoomRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(room);
        _mockGuestRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(guest);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _reservationService.UpdateReservationStatusAsync(1, ReservationStatus.Confirmed);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ReservationStatus.Confirmed, result.Status);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        
        _mockReservationRepository.Verify(r => r.Update(reservation), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCheckInsForDateAsync_ReturnsCorrectReservations()
    {
        // Arrange
        var date = DateTime.Today;
        var hotelId = 1;
        var reservations = new List<Reservation>
        {
            new Reservation
            {
                Id = 1,
                CheckInDate = date,
                Status = ReservationStatus.Confirmed,
                Hotel = new Hotel { Id = 1, Name = "Test Hotel" },
                Room = new Room { Id = 1, RoomNumber = "101" },
                Guest = new Guest { Id = 1, FirstName = "John", LastName = "Doe" }
            }
        };

        _mockReservationRepository.Setup(r => r.GetCheckInsForDateAsync(date, hotelId))
            .ReturnsAsync(reservations);

        // Act
        var result = await _reservationService.GetCheckInsForDateAsync(date, hotelId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var checkIn = result.First();
        Assert.Equal(date, checkIn.CheckInDate);
        Assert.Equal("John Doe", checkIn.GuestName);
    }

    [Fact]
    public async Task GetCheckOutsForDateAsync_ReturnsCorrectReservations()
    {
        // Arrange
        var date = DateTime.Today;
        var hotelId = 1;
        var reservations = new List<Reservation>
        {
            new Reservation
            {
                Id = 1,
                CheckOutDate = date,
                Status = ReservationStatus.CheckedIn,
                Hotel = new Hotel { Id = 1, Name = "Test Hotel" },
                Room = new Room { Id = 1, RoomNumber = "101" },
                Guest = new Guest { Id = 1, FirstName = "Jane", LastName = "Smith" }
            }
        };

        _mockReservationRepository.Setup(r => r.GetCheckOutsForDateAsync(date, hotelId))
            .ReturnsAsync(reservations);

        // Act
        var result = await _reservationService.GetCheckOutsForDateAsync(date, hotelId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var checkOut = result.First();
        Assert.Equal(date, checkOut.CheckOutDate);
        Assert.Equal("Jane Smith", checkOut.GuestName);
    }

    [Fact]
    public async Task ValidateRoomCapacityAsync_NonExistentRoom_ThrowsRoomNotFoundException()
    {
        // Arrange
        var roomId = 999;
        var numberOfGuests = 2;

        _mockRoomRepository.Setup(r => r.GetByIdAsync(roomId)).ReturnsAsync((Room?)null);

        // Act & Assert
        await Assert.ThrowsAsync<RoomNotFoundException>(
            () => _reservationService.ValidateRoomCapacityAsync(roomId, numberOfGuests));
    }

    [Fact]
    public async Task CreateReservationAsync_EmptyBookingReference_CreatesReservationSuccessfully()
    {
        // Arrange
        var request = new CreateReservationRequest
        {
            HotelId = 1,
            RoomId = 1,
            GuestId = 1,
            BookingReference = "", // Empty booking reference
            Source = ReservationSource.Manual,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            TotalAmount = 200.00m,
            Status = ReservationStatus.Confirmed
        };

        var guest = new Guest { Id = 1, FirstName = "John", LastName = "Doe" };
        var hotel = new Hotel { Id = 1, Name = "Test Hotel" };
        var room = new Room { Id = 1, RoomNumber = "101", Capacity = 4 };
        var reservation = new Reservation { Id = 1 };

        _mockPropertyService.Setup(s => s.ValidateHotelExistsAsync(1)).Returns(Task.CompletedTask);
        _mockPropertyService.Setup(s => s.ValidateRoomExistsAsync(1)).Returns(Task.CompletedTask);
        _mockGuestRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(guest);
        _mockRoomRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(room);
        _mockReservationRepository.Setup(r => r.HasConflictingReservationsAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null)).ReturnsAsync(false);
        _mockReservationRepository.Setup(r => r.AddAsync(It.IsAny<Reservation>())).Callback<Reservation>(r => r.Id = 1);
        _mockReservationRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservation);
        _mockHotelRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(hotel);
        _mockGuestRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(guest);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _reservationService.CreateReservationAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        _mockReservationRepository.Verify(r => r.AddAsync(It.IsAny<Reservation>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion
}