using Xunit;
using FluentAssertions;
using HotelReservationSystem.Data.Repositories;
using HotelReservationSystem.Models;
using HotelReservationSystem.Tests.Helpers;

namespace HotelReservationSystem.Tests.Repositories;

public class HotelRepositoryTests : IDisposable
{
    private readonly HotelReservationSystem.Data.HotelReservationContext _context;
    private readonly HotelRepository _repository;

    public HotelRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new HotelRepository(_context);
    }

    [Fact]
    public async Task GetActiveHotelsAsync_ShouldReturnOnlyActiveHotels()
    {
        // Arrange
        var activeHotel = new Hotel
        {
            Name = "Active Hotel",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var inactiveHotel = new Hotel
        {
            Name = "Inactive Hotel",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Hotels.AddRangeAsync(activeHotel, inactiveHotel);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveHotelsAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Active Hotel");
        result.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetHotelWithRoomsAsync_ShouldReturnHotelWithRooms()
    {
        // Arrange
        var hotel = new Hotel
        {
            Id = 1,
            Name = "Test Hotel",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var room = new Room
        {
            HotelId = 1,
            RoomNumber = "101",
            Type = RoomType.Single,
            Capacity = 1,
            BaseRate = 100.00m,
            Status = RoomStatus.Available,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Hotels.AddAsync(hotel);
        await _context.Rooms.AddAsync(room);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetHotelWithRoomsAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Hotel");
        result.Rooms.Should().HaveCount(1);
        result.Rooms.First().RoomNumber.Should().Be("101");
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrueWhenHotelExists()
    {
        // Arrange
        var hotel = new Hotel
        {
            Id = 1,
            Name = "Test Hotel",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Hotels.AddAsync(hotel);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalseWhenHotelDoesNotExist()
    {
        // Act
        var result = await _repository.ExistsAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetHotelWithReservationsAsync_ShouldReturnHotelWithReservationsInDateRange()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        var fromDate = DateTime.Today;
        var toDate = DateTime.Today.AddDays(10);

        // Act
        var result = await _repository.GetHotelWithReservationsAsync(1, fromDate, toDate);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Grand Hotel");
        result.Reservations.Should().NotBeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}