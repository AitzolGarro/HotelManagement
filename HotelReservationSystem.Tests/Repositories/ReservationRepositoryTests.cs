using Xunit;
using FluentAssertions;
using HotelReservationSystem.Data.Repositories;
using HotelReservationSystem.Models;
using HotelReservationSystem.Tests.Helpers;

namespace HotelReservationSystem.Tests.Repositories;

public class ReservationRepositoryTests : IDisposable
{
    private readonly HotelReservationSystem.Data.HotelReservationContext _context;
    private readonly ReservationRepository _repository;

    public ReservationRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new ReservationRepository(_context);
    }

    [Fact]
    public async Task GetReservationsByDateRangeAsync_ShouldReturnReservationsInRange()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        var fromDate = DateTime.Today;
        var toDate = DateTime.Today.AddDays(10);

        // Act
        var result = await _repository.GetReservationsByDateRangeAsync(fromDate, toDate);

        // Assert
        result.Should().HaveCount(2); // Both sample reservations are in this range
    }

    [Fact]
    public async Task GetReservationsByDateRangeAsync_WithHotelFilter_ShouldReturnFilteredReservations()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        var fromDate = DateTime.Today;
        var toDate = DateTime.Today.AddDays(10);

        // Act
        var result = await _repository.GetReservationsByDateRangeAsync(fromDate, toDate, hotelId: 1);

        // Assert
        result.Should().HaveCount(2);
        result.All(r => r.HotelId == 1).Should().BeTrue();
    }

    [Fact]
    public async Task GetReservationsByRoomAsync_ShouldReturnReservationsForRoom()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.GetReservationsByRoomAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result.First().RoomId.Should().Be(1);
    }

    [Fact]
    public async Task GetReservationsByGuestAsync_ShouldReturnReservationsForGuest()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.GetReservationsByGuestAsync(1);

        // Assert
        result.Should().HaveCount(1);
        result.First().GuestId.Should().Be(1);
    }

    [Fact]
    public async Task GetReservationByBookingReferenceAsync_ShouldReturnReservationWhenExists()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.GetReservationByBookingReferenceAsync("BK001");

        // Assert
        result.Should().NotBeNull();
        result!.BookingReference.Should().Be("BK001");
        result.GuestId.Should().Be(1);
    }

    [Fact]
    public async Task GetReservationByBookingReferenceAsync_ShouldReturnNullWhenNotExists()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.GetReservationByBookingReferenceAsync("NONEXISTENT");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetConflictingReservationsAsync_ShouldReturnConflictingReservations()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Room 1 has reservation from Today+1 to Today+3
        var checkIn = DateTime.Today.AddDays(2); // Overlaps
        var checkOut = DateTime.Today.AddDays(4);

        // Act
        var result = await _repository.GetConflictingReservationsAsync(1, checkIn, checkOut);

        // Assert
        result.Should().HaveCount(1);
        result.First().RoomId.Should().Be(1);
    }

    [Fact]
    public async Task GetConflictingReservationsAsync_ShouldExcludeSpecifiedReservation()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        var checkIn = DateTime.Today.AddDays(2);
        var checkOut = DateTime.Today.AddDays(4);

        // Act - exclude the existing reservation
        var result = await _repository.GetConflictingReservationsAsync(1, checkIn, checkOut, excludeReservationId: 1);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task HasConflictingReservationsAsync_ShouldReturnTrueWhenConflictsExist()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        var checkIn = DateTime.Today.AddDays(2);
        var checkOut = DateTime.Today.AddDays(4);

        // Act
        var result = await _repository.HasConflictingReservationsAsync(1, checkIn, checkOut);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasConflictingReservationsAsync_ShouldReturnFalseWhenNoConflicts()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        var checkIn = DateTime.Today.AddDays(10);
        var checkOut = DateTime.Today.AddDays(12);

        // Act
        var result = await _repository.HasConflictingReservationsAsync(1, checkIn, checkOut);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetCheckInsForDateAsync_ShouldReturnCheckInsForSpecificDate()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        var checkInDate = DateTime.Today.AddDays(1);

        // Act
        var result = await _repository.GetCheckInsForDateAsync(checkInDate);

        // Assert
        result.Should().HaveCount(1);
        result.First().CheckInDate.Date.Should().Be(checkInDate.Date);
    }

    [Fact]
    public async Task GetCheckOutsForDateAsync_ShouldReturnCheckOutsForSpecificDate()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        var checkOutDate = DateTime.Today.AddDays(3);

        // Act
        var result = await _repository.GetCheckOutsForDateAsync(checkOutDate);

        // Assert
        result.Should().HaveCount(1);
        result.First().CheckOutDate.Date.Should().Be(checkOutDate.Date);
    }

    [Fact]
    public async Task GetReservationsByStatusAsync_ShouldReturnReservationsWithSpecificStatus()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.GetReservationsByStatusAsync(ReservationStatus.Confirmed);

        // Assert
        result.Should().HaveCount(2);
        result.All(r => r.Status == ReservationStatus.Confirmed).Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}