using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using HotelReservationSystem.Configuration;
using HotelReservationSystem.Data;
using HotelReservationSystem.Data.Repositories;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Tests.Services.Expedia;

/// <summary>
/// Tests verifying the reservation-persistence bug fix in <see cref="ChannelManagerService"/>.
///
/// Before fix: ImportReservationsFromChannelAsync counted Expedia reservations but never saved them.
/// After fix:  PersistReservationsAsync is called for all Expedia reservations returned by
///             GetReservationsAsync, writing them to the database via DbContext.
/// </summary>
public class ExpediaChannelServiceBugFixTests : IDisposable
{
    private readonly HotelReservationContext _dbContext;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IExpediaChannelService> _expediaServiceMock;
    private readonly Mock<IBookingIntegrationService> _bookingComMock;
    private readonly Mock<IEncryptionService> _encryptionMock;
    private readonly ILogger<ChannelManagerService> _logger;

    private const int ExpediaChannelId = 2;

    public ExpediaChannelServiceBugFixTests()
    {
        var dbOptions = new DbContextOptionsBuilder<HotelReservationContext>()
            .UseInMemoryDatabase($"BugFixTests_{Guid.NewGuid()}")
            .Options;
        _dbContext = new HotelReservationContext(dbOptions);

        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _expediaServiceMock = new Mock<IExpediaChannelService>();
        _bookingComMock = new Mock<IBookingIntegrationService>();
        _encryptionMock = new Mock<IEncryptionService>();
        _logger = new Mock<ILogger<ChannelManagerService>>().Object;
    }

    private ChannelManagerService CreateService()
        => new(
            _unitOfWorkMock.Object,
            _bookingComMock.Object,
            _expediaServiceMock.Object,
            _encryptionMock.Object,
            _dbContext,
            _logger);

    private async Task<HotelChannel> SetupExpediaChannelAsync(int hotelId = 1)
    {
        // Seed minimal data so the channel is found
        var hotel = new Hotel { Id = hotelId, Name = "Test Hotel", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _dbContext.Hotels.Add(hotel);
        await _dbContext.SaveChangesAsync();

        var channel = new HotelChannel
        {
            Id = 100,
            HotelId = hotelId,
            ChannelId = ExpediaChannelId,
            ChannelHotelId = "EXP-001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _unitOfWorkMock
            .Setup(u => u.HotelChannels.GetByIdAsync(100))
            .ReturnsAsync(channel);

        _unitOfWorkMock
            .Setup(u => u.ChannelSyncLogs.AddAsync(It.IsAny<ChannelSyncLog>()))
            .ReturnsAsync((ChannelSyncLog log) => log);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        return channel;
    }

    // ── Scenario: Bug fix — 3 Expedia reservations imported and persisted ────

    [Fact]
    public async Task ImportReservationsFromChannelAsync_ExpediaChannel_PersistsAllReservations()
    {
        // Arrange
        await SetupExpediaChannelAsync(hotelId: 1);

        // Seed a guest so MapDtoToEntity can reference it
        var guest = new Guest { Id = 1, FirstName = "Test", LastName = "Guest", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var room = new Room { Id = 1, HotelId = 1, RoomNumber = "101", Type = RoomType.Single, Capacity = 2, BaseRate = 100m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _dbContext.Guests.Add(guest);
        _dbContext.Rooms.Add(room);
        await _dbContext.SaveChangesAsync();

        var expediaReservations = Enumerable.Range(1, 3).Select(i => new ReservationDto
        {
            HotelId = 1,
            RoomId = 1,
            GuestId = 1,
            BookingReference = $"EXP-{i:000}",
            Source = ReservationSource.Expedia,
            CheckInDate = DateTime.Today.AddDays(i),
            CheckOutDate = DateTime.Today.AddDays(i + 3),
            NumberOfGuests = 2,
            TotalAmount = 300m * i,
            Status = ReservationStatus.Confirmed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        _expediaServiceMock
            .Setup(s => s.GetReservationsAsync(1, It.IsAny<DateTime?>()))
            .ReturnsAsync(expediaReservations);

        var service = CreateService();

        // Act
        var result = await service.ImportReservationsFromChannelAsync(100);

        // Assert
        result.Should().BeTrue();

        var persisted = _dbContext.Reservations
            .Where(r => r.Source == ReservationSource.Expedia)
            .ToList();

        persisted.Should().HaveCount(3,
            "all 3 Expedia reservations returned by GetReservationsAsync must be written to the database");

        persisted.Select(r => r.BookingReference)
            .Should().BeEquivalentTo(new[] { "EXP-001", "EXP-002", "EXP-003" });
    }

    // ── Scenario: Zero Expedia reservations — no DB writes ───────────────────

    [Fact]
    public async Task ImportReservationsFromChannelAsync_EmptyExpediaList_NoDbWrites()
    {
        // Arrange
        await SetupExpediaChannelAsync(hotelId: 1);

        _expediaServiceMock
            .Setup(s => s.GetReservationsAsync(1, It.IsAny<DateTime?>()))
            .ReturnsAsync(Enumerable.Empty<ReservationDto>());

        var service = CreateService();

        // Act
        var result = await service.ImportReservationsFromChannelAsync(100);

        // Assert
        result.Should().BeTrue();
        _dbContext.Reservations.Count().Should().Be(0);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
