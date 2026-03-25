using Xunit;
using FluentAssertions;
using HotelReservationSystem.Data.Repositories;
using HotelReservationSystem.Models;
using HotelReservationSystem.Tests.Helpers;

namespace HotelReservationSystem.Tests.Repositories;

public class UnitOfWorkTests : IDisposable
{
    private readonly HotelReservationSystem.Data.HotelReservationContext _context;
    private readonly UnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _unitOfWork = new UnitOfWork(_context);
    }

    [Fact]
    public void Repositories_ShouldReturnSameInstanceOnMultipleCalls()
    {
        // Act
        var hotels1 = _unitOfWork.Hotels;
        var hotels2 = _unitOfWork.Hotels;
        var rooms1 = _unitOfWork.Rooms;
        var rooms2 = _unitOfWork.Rooms;
        var guests1 = _unitOfWork.Guests;
        var guests2 = _unitOfWork.Guests;
        var reservations1 = _unitOfWork.Reservations;
        var reservations2 = _unitOfWork.Reservations;

        // Assert
        hotels1.Should().BeSameAs(hotels2);
        rooms1.Should().BeSameAs(rooms2);
        guests1.Should().BeSameAs(guests2);
        reservations1.Should().BeSameAs(reservations2);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistChangesToDatabase()
    {
        // Arrange
        var hotel = new Hotel
        {
            Name = "Test Hotel",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await _unitOfWork.Hotels.AddAsync(hotel);
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().Be(1); // One entity saved
        
        var savedHotel = await _unitOfWork.Hotels.GetByIdAsync(hotel.Id);
        savedHotel.Should().NotBeNull();
        savedHotel!.Name.Should().Be("Test Hotel");
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldUpdateTimestampsOnModifiedEntities()
    {
        // Arrange
        var hotel = new Hotel
        {
            Name = "Test Hotel",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow.AddDays(-1) // Set to yesterday
        };

        await _unitOfWork.Hotels.AddAsync(hotel);
        await _unitOfWork.SaveChangesAsync();

        var originalUpdateTime = hotel.UpdatedAt;

        // Act
        hotel.Name = "Updated Hotel";
        _unitOfWork.Hotels.Update(hotel);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        hotel.UpdatedAt.Should().BeAfter(originalUpdateTime);
    }

    [Fact]
    public async Task TransactionOperations_ShouldWorkCorrectly()
    {
        // Arrange
        var hotel = new Hotel
        {
            Name = "Test Hotel",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act & Assert
        await _unitOfWork.BeginTransactionAsync();
        
        await _unitOfWork.Hotels.AddAsync(hotel);
        await _unitOfWork.SaveChangesAsync();
        
        await _unitOfWork.CommitTransactionAsync();

        var savedHotel = await _unitOfWork.Hotels.GetByIdAsync(hotel.Id);
        savedHotel.Should().NotBeNull();
    }

    [Fact]
    public async Task RollbackTransaction_ShouldUndoChanges()
    {
        // Arrange
        var hotel = new Hotel
        {
            Name = "Test Hotel",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await _unitOfWork.BeginTransactionAsync();
        
        await _unitOfWork.Hotels.AddAsync(hotel);
        await _unitOfWork.SaveChangesAsync();
        
        await _unitOfWork.RollbackTransactionAsync();

        // Assert
        var savedHotel = await _unitOfWork.Hotels.GetByIdAsync(hotel.Id);
        savedHotel.Should().BeNull(); // Should not exist due to rollback
    }

    [Fact]
    public async Task MultipleRepositoryOperations_ShouldWorkInSameTransaction()
    {
        // Arrange
        var hotel = new Hotel
        {
            Name = "Test Hotel",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var guest = new Guest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await _unitOfWork.BeginTransactionAsync();
        
        await _unitOfWork.Hotels.AddAsync(hotel);
        await _unitOfWork.Guests.AddAsync(guest);
        var result = await _unitOfWork.SaveChangesAsync();
        
        await _unitOfWork.CommitTransactionAsync();

        // Assert
        result.Should().Be(2); // Two entities saved
        
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