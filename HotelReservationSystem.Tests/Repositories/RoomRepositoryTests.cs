using FluentAssertions;
using HotelReservationSystem.Data.Repositories;
using HotelReservationSystem.Models;
using HotelReservationSystem.Tests.Helpers;

namespace HotelReservationSystem.Tests.Repositories;

public class RoomRepositoryTests : IDisposable
{
    private readonly HotelReservationSystem.Data.HotelReservationContext _context;
    private readonly RoomRepository _repository;

    public RoomRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new RoomRepository(_context);
    }

    [Fact]
    public async Task GetRoomsByHotelAsync_ShouldReturnRoomsForSpecificHotel()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.GetRoomsByHotelAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.All(r => r.HotelId == 1).Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableRoomsAsync_ShouldReturnOnlyAvailableRooms()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        var checkIn = DateTime.Today.AddDays(10);
        var checkOut = DateTime.Today.AddDays(12);

        // Act
        var result = await _repository.GetAvailableRoomsAsync(1, checkIn, checkOut);

        // Assert
        result.Should().HaveCount(2); // Both rooms should be available for future dates
        result.All(r => r.Status == RoomStatus.Available).Should().BeTrue();
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ShouldReturnFalseWhenRoomHasConflictingReservation()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Room 1 has a reservation from Today+1 to Today+3
        var checkIn = DateTime.Today.AddDays(2); // Overlaps with existing reservation
        var checkOut = DateTime.Today.AddDays(4);

        // Act
        var result = await _repository.IsRoomAvailableAsync(1, checkIn, checkOut);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ShouldReturnTrueWhenRoomIsAvailable()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        var checkIn = DateTime.Today.AddDays(10); // No conflicts
        var checkOut = DateTime.Today.AddDays(12);

        // Act
        var result = await _repository.IsRoomAvailableAsync(1, checkIn, checkOut);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsInHotelAsync_ShouldReturnTrueWhenRoomExists()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.ExistsInHotelAsync(1, "101");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsInHotelAsync_ShouldReturnFalseWhenRoomDoesNotExist()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.ExistsInHotelAsync(1, "999");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetRoomsByStatusAsync_ShouldReturnRoomsWithSpecificStatus()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.GetRoomsByStatusAsync(RoomStatus.Available);

        // Assert
        result.Should().HaveCount(3); // All sample rooms are available
        result.All(r => r.Status == RoomStatus.Available).Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}