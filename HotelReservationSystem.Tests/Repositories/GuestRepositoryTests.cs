using FluentAssertions;
using HotelReservationSystem.Data.Repositories;
using HotelReservationSystem.Models;
using HotelReservationSystem.Tests.Helpers;

namespace HotelReservationSystem.Tests.Repositories;

public class GuestRepositoryTests : IDisposable
{
    private readonly HotelReservationSystem.Data.HotelReservationContext _context;
    private readonly GuestRepository _repository;

    public GuestRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new GuestRepository(_context);
    }

    [Fact]
    public async Task GetGuestByEmailAsync_ShouldReturnGuestWhenEmailExists()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.GetGuestByEmailAsync("john.doe@email.com");

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task GetGuestByEmailAsync_ShouldReturnNullWhenEmailDoesNotExist()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.GetGuestByEmailAsync("nonexistent@email.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetGuestByDocumentNumberAsync_ShouldReturnGuestWhenDocumentExists()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.GetGuestByDocumentNumberAsync("ID123456");

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("John");
        result.DocumentNumber.Should().Be("ID123456");
    }

    [Fact]
    public async Task SearchGuestsAsync_ShouldReturnMatchingGuests()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.SearchGuestsAsync("john");

        // Assert
        result.Should().HaveCount(1);
        result.First().FirstName.Should().Be("John");
    }

    [Fact]
    public async Task SearchGuestsAsync_ShouldReturnGuestsByLastName()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.SearchGuestsAsync("smith");

        // Assert
        result.Should().HaveCount(1);
        result.First().LastName.Should().Be("Smith");
    }

    [Fact]
    public async Task SearchGuestsAsync_ShouldReturnGuestsByEmail()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.SearchGuestsAsync("jane.smith");

        // Assert
        result.Should().HaveCount(1);
        result.First().Email.Should().Be("jane.smith@email.com");
    }

    [Fact]
    public async Task ExistsByEmailAsync_ShouldReturnTrueWhenEmailExists()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.ExistsByEmailAsync("john.doe@email.com");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByEmailAsync_ShouldReturnFalseWhenEmailDoesNotExist()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.ExistsByEmailAsync("nonexistent@email.com");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByDocumentNumberAsync_ShouldReturnTrueWhenDocumentExists()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.ExistsByDocumentNumberAsync("ID123456");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetGuestWithReservationsAsync_ShouldReturnGuestWithReservations()
    {
        // Arrange
        await TestDbContextFactory.SeedSampleDataAsync(_context);

        // Act
        var result = await _repository.GetGuestWithReservationsAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("John");
        result.Reservations.Should().HaveCount(1);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}