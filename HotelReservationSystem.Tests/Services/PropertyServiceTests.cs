using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HotelReservationSystem.Services;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Exceptions;

namespace HotelReservationSystem.Tests.Services;

public class PropertyServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IHotelRepository> _mockHotelRepository;
    private readonly Mock<IRoomRepository> _mockRoomRepository;
    private readonly Mock<IReservationRepository> _mockReservationRepository;
    private readonly Mock<ILogger<PropertyService>> _mockLogger;
    private readonly PropertyService _propertyService;

    public PropertyServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockHotelRepository = new Mock<IHotelRepository>();
        _mockRoomRepository = new Mock<IRoomRepository>();
        _mockReservationRepository = new Mock<IReservationRepository>();
        _mockLogger = new Mock<ILogger<PropertyService>>();

        _mockUnitOfWork.Setup(u => u.Hotels).Returns(_mockHotelRepository.Object);
        _mockUnitOfWork.Setup(u => u.Rooms).Returns(_mockRoomRepository.Object);
        _mockUnitOfWork.Setup(u => u.Reservations).Returns(_mockReservationRepository.Object);

        _propertyService = new PropertyService(_mockUnitOfWork.Object, _mockLogger.Object);
    }

    #region Hotel Tests

    [Fact]
    public async Task CreateHotelAsync_ValidRequest_ReturnsHotelDto()
    {
        // Arrange
        var request = new CreateHotelRequest
        {
            Name = "Test Hotel",
            Address = "123 Test St",
            Phone = "555-1234",
            Email = "test@hotel.com",
            IsActive = true
        };

        var createdHotel = new Hotel
        {
            Id = 1,
            Name = request.Name,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockHotelRepository.Setup(r => r.AddAsync(It.IsAny<Hotel>()))
            .Callback<Hotel>(h => h.Id = 1);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _propertyService.CreateHotelAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Address, result.Address);
        Assert.Equal(request.Phone, result.Phone);
        Assert.Equal(request.Email, result.Email);
        Assert.Equal(request.IsActive, result.IsActive);
        
        _mockHotelRepository.Verify(r => r.AddAsync(It.IsAny<Hotel>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetHotelByIdAsync_ExistingHotel_ReturnsHotelDto()
    {
        // Arrange
        var hotel = new Hotel
        {
            Id = 1,
            Name = "Test Hotel",
            Address = "123 Test St",
            Phone = "555-1234",
            Email = "test@hotel.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Rooms = new List<Room>()
        };

        _mockHotelRepository.Setup(r => r.GetHotelWithRoomsAsync(1))
            .ReturnsAsync(hotel);

        // Act
        var result = await _propertyService.GetHotelByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(hotel.Id, result.Id);
        Assert.Equal(hotel.Name, result.Name);
    }

    [Fact]
    public async Task GetHotelByIdAsync_NonExistingHotel_ReturnsNull()
    {
        // Arrange
        _mockHotelRepository.Setup(r => r.GetHotelWithRoomsAsync(999))
            .ReturnsAsync((Hotel?)null);

        // Act
        var result = await _propertyService.GetHotelByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateHotelAsync_ExistingHotel_ReturnsUpdatedHotelDto()
    {
        // Arrange
        var existingHotel = new Hotel
        {
            Id = 1,
            Name = "Old Name",
            Address = "Old Address",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var updateRequest = new UpdateHotelRequest
        {
            Name = "New Name",
            Address = "New Address",
            Phone = "555-9999",
            Email = "new@hotel.com",
            IsActive = false
        };

        _mockHotelRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(existingHotel);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _propertyService.UpdateHotelAsync(1, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateRequest.Name, result.Name);
        Assert.Equal(updateRequest.Address, result.Address);
        Assert.Equal(updateRequest.Phone, result.Phone);
        Assert.Equal(updateRequest.Email, result.Email);
        Assert.Equal(updateRequest.IsActive, result.IsActive);
        
        _mockHotelRepository.Verify(r => r.Update(It.IsAny<Hotel>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateHotelAsync_NonExistingHotel_ThrowsPropertyNotFoundException()
    {
        // Arrange
        var updateRequest = new UpdateHotelRequest { Name = "Test" };
        
        _mockHotelRepository.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Hotel?)null);

        // Act & Assert
        await Assert.ThrowsAsync<PropertyNotFoundException>(
            () => _propertyService.UpdateHotelAsync(999, updateRequest));
    }

    [Fact]
    public async Task DeleteHotelAsync_HotelWithActiveReservations_ThrowsInvalidOperationException()
    {
        // Arrange
        var hotel = new Hotel { Id = 1, Name = "Test Hotel", IsActive = true };
        
        _mockHotelRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(hotel);
        _mockReservationRepository.Setup(r => r.HasActiveReservationsForHotelAsync(1))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _propertyService.DeleteHotelAsync(1));
    }

    #endregion

    #region Room Tests

    [Fact]
    public async Task CreateRoomAsync_ValidRequest_ReturnsRoomDto()
    {
        // Arrange
        var hotel = new Hotel { Id = 1, Name = "Test Hotel", IsActive = true };
        var request = new CreateRoomRequest
        {
            HotelId = 1,
            RoomNumber = "101",
            Type = RoomType.Single,
            Capacity = 2,
            BaseRate = 100.00m,
            Status = RoomStatus.Available,
            Description = "Test room"
        };

        _mockHotelRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(hotel);
        _mockRoomRepository.Setup(r => r.GetRoomByNumberAsync(1, "101"))
            .ReturnsAsync((Room?)null);
        _mockRoomRepository.Setup(r => r.AddAsync(It.IsAny<Room>()))
            .Callback<Room>(r => r.Id = 1);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _propertyService.CreateRoomAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.RoomNumber, result.RoomNumber);
        Assert.Equal(request.Type, result.Type);
        Assert.Equal(request.Capacity, result.Capacity);
        Assert.Equal(request.BaseRate, result.BaseRate);
        Assert.Equal(hotel.Name, result.HotelName);
        
        _mockRoomRepository.Verify(r => r.AddAsync(It.IsAny<Room>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateRoomAsync_NonExistingHotel_ThrowsPropertyNotFoundException()
    {
        // Arrange
        var request = new CreateRoomRequest { HotelId = 999, RoomNumber = "101" };
        
        _mockHotelRepository.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Hotel?)null);

        // Act & Assert
        await Assert.ThrowsAsync<PropertyNotFoundException>(
            () => _propertyService.CreateRoomAsync(request));
    }

    [Fact]
    public async Task CreateRoomAsync_DuplicateRoomNumber_ThrowsDuplicateRoomNumberException()
    {
        // Arrange
        var hotel = new Hotel { Id = 1, Name = "Test Hotel", IsActive = true };
        var existingRoom = new Room { Id = 1, HotelId = 1, RoomNumber = "101" };
        var request = new CreateRoomRequest { HotelId = 1, RoomNumber = "101" };

        _mockHotelRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(hotel);
        _mockRoomRepository.Setup(r => r.GetRoomByNumberAsync(1, "101"))
            .ReturnsAsync(existingRoom);

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateRoomNumberException>(
            () => _propertyService.CreateRoomAsync(request));
    }

    [Fact]
    public async Task GetAvailableRoomsAsync_ValidDateRange_ReturnsAvailableRooms()
    {
        // Arrange
        var hotel = new Hotel { Id = 1, Name = "Test Hotel", IsActive = true };
        var checkIn = DateTime.Today.AddDays(1);
        var checkOut = DateTime.Today.AddDays(3);
        
        var availableRooms = new List<Room>
        {
            new Room { Id = 1, HotelId = 1, RoomNumber = "101", Type = RoomType.Single },
            new Room { Id = 2, HotelId = 1, RoomNumber = "102", Type = RoomType.Double }
        };

        _mockHotelRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(hotel);
        _mockRoomRepository.Setup(r => r.GetAvailableRoomsAsync(1, checkIn, checkOut))
            .ReturnsAsync(availableRooms);

        // Act
        var result = await _propertyService.GetAvailableRoomsAsync(1, checkIn, checkOut);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, room => Assert.Equal(hotel.Name, room.HotelName));
    }

    [Fact]
    public async Task GetAvailableRoomsAsync_InvalidDateRange_ThrowsArgumentException()
    {
        // Arrange
        var hotel = new Hotel { Id = 1, Name = "Test Hotel", IsActive = true };
        var checkIn = DateTime.Today.AddDays(3);
        var checkOut = DateTime.Today.AddDays(1); // Check-out before check-in

        _mockHotelRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(hotel);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _propertyService.GetAvailableRoomsAsync(1, checkIn, checkOut));
    }

    [Fact]
    public async Task GetAvailableRoomsAsync_PastCheckInDate_ThrowsArgumentException()
    {
        // Arrange
        var hotel = new Hotel { Id = 1, Name = "Test Hotel", IsActive = true };
        var checkIn = DateTime.Today.AddDays(-1); // Past date
        var checkOut = DateTime.Today.AddDays(1);

        _mockHotelRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(hotel);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _propertyService.GetAvailableRoomsAsync(1, checkIn, checkOut));
    }

    [Fact]
    public async Task SetRoomStatusAsync_RoomWithActiveReservations_ThrowsInvalidRoomStatusException()
    {
        // Arrange
        var room = new Room { Id = 1, HotelId = 1, RoomNumber = "101", Status = RoomStatus.Available };
        
        _mockRoomRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(room);
        _mockReservationRepository.Setup(r => r.HasActiveReservationsForRoomAsync(1))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidRoomStatusException>(
            () => _propertyService.SetRoomStatusAsync(1, RoomStatus.Maintenance));
    }

    [Fact]
    public async Task SetRoomStatusAsync_ValidStatusChange_ReturnsTrue()
    {
        // Arrange
        var room = new Room { Id = 1, HotelId = 1, RoomNumber = "101", Status = RoomStatus.Available };
        
        _mockRoomRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(room);
        _mockReservationRepository.Setup(r => r.HasActiveReservationsForRoomAsync(1))
            .ReturnsAsync(false);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _propertyService.SetRoomStatusAsync(1, RoomStatus.Maintenance);

        // Assert
        Assert.True(result);
        Assert.Equal(RoomStatus.Maintenance, room.Status);
        
        _mockRoomRepository.Verify(r => r.Update(room), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ValidateHotelExistsAsync_ExistingActiveHotel_DoesNotThrow()
    {
        // Arrange
        var hotel = new Hotel { Id = 1, Name = "Test Hotel", IsActive = true };
        
        _mockHotelRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(hotel);

        // Act & Assert
        await _propertyService.ValidateHotelExistsAsync(1); // Should not throw
    }

    [Fact]
    public async Task ValidateHotelExistsAsync_NonExistingHotel_ThrowsPropertyNotFoundException()
    {
        // Arrange
        _mockHotelRepository.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Hotel?)null);

        // Act & Assert
        await Assert.ThrowsAsync<PropertyNotFoundException>(
            () => _propertyService.ValidateHotelExistsAsync(999));
    }

    [Fact]
    public async Task ValidateHotelExistsAsync_InactiveHotel_ThrowsPropertyNotFoundException()
    {
        // Arrange
        var hotel = new Hotel { Id = 1, Name = "Test Hotel", IsActive = false };
        
        _mockHotelRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(hotel);

        // Act & Assert
        await Assert.ThrowsAsync<PropertyNotFoundException>(
            () => _propertyService.ValidateHotelExistsAsync(1));
    }

    [Fact]
    public async Task ValidateRoomExistsAsync_ExistingRoom_DoesNotThrow()
    {
        // Arrange
        var room = new Room { Id = 1, HotelId = 1, RoomNumber = "101" };
        
        _mockRoomRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(room);

        // Act & Assert
        await _propertyService.ValidateRoomExistsAsync(1); // Should not throw
    }

    [Fact]
    public async Task ValidateRoomExistsAsync_NonExistingRoom_ThrowsRoomNotFoundException()
    {
        // Arrange
        _mockRoomRepository.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Room?)null);

        // Act & Assert
        await Assert.ThrowsAsync<RoomNotFoundException>(
            () => _propertyService.ValidateRoomExistsAsync(999));
    }

    [Fact]
    public async Task ValidateUniqueRoomNumberAsync_UniqueRoomNumber_DoesNotThrow()
    {
        // Arrange
        _mockRoomRepository.Setup(r => r.GetRoomByNumberAsync(1, "101"))
            .ReturnsAsync((Room?)null);

        // Act & Assert
        await _propertyService.ValidateUniqueRoomNumberAsync(1, "101"); // Should not throw
    }

    [Fact]
    public async Task ValidateUniqueRoomNumberAsync_DuplicateRoomNumber_ThrowsDuplicateRoomNumberException()
    {
        // Arrange
        var existingRoom = new Room { Id = 1, HotelId = 1, RoomNumber = "101" };
        
        _mockRoomRepository.Setup(r => r.GetRoomByNumberAsync(1, "101"))
            .ReturnsAsync(existingRoom);

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateRoomNumberException>(
            () => _propertyService.ValidateUniqueRoomNumberAsync(1, "101"));
    }

    [Fact]
    public async Task ValidateUniqueRoomNumberAsync_SameRoomUpdate_DoesNotThrow()
    {
        // Arrange
        var existingRoom = new Room { Id = 1, HotelId = 1, RoomNumber = "101" };
        
        _mockRoomRepository.Setup(r => r.GetRoomByNumberAsync(1, "101"))
            .ReturnsAsync(existingRoom);

        // Act & Assert
        await _propertyService.ValidateUniqueRoomNumberAsync(1, "101", 1); // Should not throw (same room)
    }

    #endregion
}