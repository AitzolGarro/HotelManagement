using FluentAssertions;
using HotelReservationSystem.Data.Repositories;
using HotelReservationSystem.Models;
using HotelReservationSystem.Services;
using HotelReservationSystem.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotelReservationSystem.Tests.Integration;

public class RepositoryIntegrationTests : IDisposable
{
    private readonly HotelReservationSystem.Data.HotelReservationContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly Mock<ILogger<PropertyService>> _propertyLogger;
    private readonly Mock<ILogger<ReservationService>> _reservationLogger;
    private readonly PropertyService _propertyService;
    private readonly ReservationService _reservationService;

    public RepositoryIntegrationTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _unitOfWork = new UnitOfWork(_context);
        _propertyLogger = new Mock<ILogger<PropertyService>>();
        _reservationLogger = new Mock<ILogger<ReservationService>>();
        _propertyService = new PropertyService(_unitOfWork, _propertyLogger.Object);
        _reservationService = new ReservationService(_unitOfWork, _reservationLogger.Object);
    }

    [Fact]
    public async Task EndToEndWorkflow_ShouldWorkWithRepositoryPattern()
    {
        // Arrange & Act - Create a hotel
        var hotel = new Hotel
        {
            Name = "Integration Test Hotel",
            Address = "123 Test Street",
            Phone = "+1234567890",
            Email = "test@hotel.com",
            IsActive = true
        };

        var createdHotel = await _propertyService.CreateHotelAsync(hotel);

        // Act - Create a room
        var room = new Room
        {
            HotelId = createdHotel.Id,
            RoomNumber = "101",
            Type = RoomType.Double,
            Capacity = 2,
            BaseRate = 150.00m,
            Status = RoomStatus.Available,
            Description = "Test room"
        };

        var createdRoom = await _propertyService.CreateRoomAsync(room);

        // Act - Create a guest
        var guest = new Guest
        {
            FirstName = "Integration",
            LastName = "Test",
            Email = "integration@test.com",
            Phone = "+9876543210",
            DocumentNumber = "TEST123"
        };

        await _unitOfWork.Guests.AddAsync(guest);
        await _unitOfWork.SaveChangesAsync();

        // Act - Create a reservation
        var reservation = new Reservation
        {
            HotelId = createdHotel.Id,
            RoomId = createdRoom.Id,
            GuestId = guest.Id,
            BookingReference = "INT001",
            Source = ReservationSource.Direct,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(3),
            NumberOfGuests = 2,
            TotalAmount = 300.00m,
            Status = ReservationStatus.Confirmed,
            SpecialRequests = "Integration test reservation"
        };

        var createdReservation = await _reservationService.CreateReservationAsync(reservation);

        // Assert - Verify all entities were created correctly
        createdHotel.Should().NotBeNull();
        createdHotel.Id.Should().BeGreaterThan(0);
        createdHotel.Name.Should().Be("Integration Test Hotel");

        createdRoom.Should().NotBeNull();
        createdRoom.Id.Should().BeGreaterThan(0);
        createdRoom.RoomNumber.Should().Be("101");
        createdRoom.HotelId.Should().Be(createdHotel.Id);

        guest.Should().NotBeNull();
        guest.Id.Should().BeGreaterThan(0);
        guest.Email.Should().Be("integration@test.com");

        createdReservation.Should().NotBeNull();
        createdReservation.Id.Should().BeGreaterThan(0);
        createdReservation.BookingReference.Should().Be("INT001");

        // Act & Assert - Test repository queries
        var retrievedHotel = await _propertyService.GetHotelByIdAsync(createdHotel.Id);
        retrievedHotel.Should().NotBeNull();
        retrievedHotel!.Rooms.Should().HaveCount(1);

        var hotelRooms = await _propertyService.GetRoomsByHotelIdAsync(createdHotel.Id);
        hotelRooms.Should().HaveCount(1);

        var availableRooms = await _propertyService.GetAvailableRoomsAsync(
            createdHotel.Id, 
            DateTime.Today.AddDays(10), 
            DateTime.Today.AddDays(12));
        availableRooms.Should().HaveCount(1); // Room should be available for future dates

        // Test availability check
        var isAvailable = await _reservationService.CheckAvailabilityAsync(
            createdRoom.Id,
            DateTime.Today.AddDays(2), // Overlaps with existing reservation
            DateTime.Today.AddDays(4));
        isAvailable.Should().BeFalse();

        var isAvailableFuture = await _reservationService.CheckAvailabilityAsync(
            createdRoom.Id,
            DateTime.Today.AddDays(10), // No overlap
            DateTime.Today.AddDays(12));
        isAvailableFuture.Should().BeTrue();

        // Test reservation queries
        var dateRangeReservations = await _reservationService.GetReservationsByDateRangeAsync(
            DateTime.Today,
            DateTime.Today.AddDays(10));
        dateRangeReservations.Should().HaveCount(1);

        // Test cancellation
        var cancelResult = await _reservationService.CancelReservationAsync(
            createdReservation.Id, 
            "Integration test cancellation");
        cancelResult.Should().BeTrue();

        var cancelledReservation = await _reservationService.GetReservationByIdAsync(createdReservation.Id);
        cancelledReservation.Should().NotBeNull();
        cancelledReservation!.Status.Should().Be(ReservationStatus.Cancelled);
    }

    [Fact]
    public async Task TransactionRollback_ShouldWorkCorrectly()
    {
        // Arrange
        var hotel = new Hotel
        {
            Name = "Transaction Test Hotel",
            IsActive = true
        };

        // Act - Start transaction and add hotel
        await _unitOfWork.BeginTransactionAsync();
        
        await _unitOfWork.Hotels.AddAsync(hotel);
        await _unitOfWork.SaveChangesAsync();
        
        // Verify hotel exists within transaction
        var hotelInTransaction = await _unitOfWork.Hotels.GetByIdAsync(hotel.Id);
        hotelInTransaction.Should().NotBeNull();
        
        // Rollback transaction
        await _unitOfWork.RollbackTransactionAsync();

        // Assert - Hotel should not exist after rollback
        var hotelAfterRollback = await _unitOfWork.Hotels.GetByIdAsync(hotel.Id);
        hotelAfterRollback.Should().BeNull();
    }

    [Fact]
    public async Task MultipleRepositoryOperations_ShouldWorkInSameUnitOfWork()
    {
        // Arrange
        var hotel = new Hotel
        {
            Name = "Multi-Op Test Hotel",
            IsActive = true
        };

        var guest = new Guest
        {
            FirstName = "Multi",
            LastName = "Op",
            Email = "multiop@test.com"
        };

        // Act - Use multiple repositories in same unit of work
        await _unitOfWork.BeginTransactionAsync();
        
        await _unitOfWork.Hotels.AddAsync(hotel);
        await _unitOfWork.Guests.AddAsync(guest);
        
        var saveResult = await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.CommitTransactionAsync();

        // Assert
        saveResult.Should().Be(2); // Two entities saved
        
        var savedHotel = await _unitOfWork.Hotels.GetByIdAsync(hotel.Id);
        var savedGuest = await _unitOfWork.Guests.GetByIdAsync(guest.Id);
        
        savedHotel.Should().NotBeNull();
        savedGuest.Should().NotBeNull();
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
    }
}