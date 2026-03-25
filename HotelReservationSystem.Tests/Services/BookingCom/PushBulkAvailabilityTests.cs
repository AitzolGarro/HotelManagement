using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.BookingCom;
using HotelReservationSystem.Data.Repositories.Interfaces;
using HotelReservationSystem.Services.BookingCom;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Tests.Services.BookingCom;

/// <summary>
/// Unit tests for <see cref="BookingIntegrationService.PushBulkAvailabilityAsync"/>.
/// Covers spec scenarios: N×D call count, inactive rooms excluded, cancellation exits cleanly,
/// empty range is a no-op.
/// </summary>
public class PushBulkAvailabilityTests
{
    private readonly Mock<IBookingComHttpClient> _httpMock;
    private readonly Mock<IXmlSerializationService> _xmlMock;
    private readonly Mock<IBookingComAuthenticationService> _authMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IRoomRepository> _roomRepoMock;
    private readonly BookingComConfiguration _config;
    private readonly BookingIntegrationService _sut;

    public PushBulkAvailabilityTests()
    {
        _httpMock = new Mock<IBookingComHttpClient>();
        _xmlMock = new Mock<IXmlSerializationService>();
        _authMock = new Mock<IBookingComAuthenticationService>();
        _uowMock = new Mock<IUnitOfWork>();
        _roomRepoMock = new Mock<IRoomRepository>();
        _config = new BookingComConfiguration { BulkPushDelayMs = 0 };

        _uowMock.Setup(u => u.Rooms).Returns(_roomRepoMock.Object);
        _authMock.Setup(a => a.GetAuthentication())
            .Returns(new BookingComAuthentication { Username = "u", Password = "p" });
        _xmlMock.Setup(x => x.Serialize(It.IsAny<object>())).Returns("<xml/>");
        _httpMock.Setup(h => h.SendRequestAsync<BookingComResponse>(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookingComResponse { Ok = "success" });

        _sut = new BookingIntegrationService(
            _httpMock.Object,
            _xmlMock.Object,
            _authMock.Object,
            _uowMock.Object,
            Mock.Of<IReservationService>(),
            Mock.Of<IPropertyService>(),
            _config,
            Mock.Of<ILogger<BookingIntegrationService>>());
    }

    [Fact]
    public async Task PushBulkAvailabilityAsync_NRoomsAndDDays_CallsNTimesD()
    {
        // GIVEN 3 active rooms, 2-day range
        const int roomCount = 3;
        const int dayCount = 2;
        var rooms = BuildActiveRooms(hotelId: 1, count: roomCount);

        _roomRepoMock.Setup(r => r.GetRoomsByHotelAsync(1))
            .ReturnsAsync(rooms);

        var range = new DateRange(new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 1 + dayCount));

        // WHEN
        await _sut.PushBulkAvailabilityAsync(1, range);

        // THEN PushAvailabilityUpdateAsync was called for each (room, date) = N × D
        _httpMock.Verify(h => h.SendRequestAsync<BookingComResponse>(
            "availability/update", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(roomCount * dayCount));
    }

    [Fact]
    public async Task PushBulkAvailabilityAsync_InactiveRoomsExcluded()
    {
        // GIVEN 2 active + 2 inactive rooms
        var activeRooms = BuildActiveRooms(hotelId: 1, count: 2);
        var inactiveRooms = new List<Room>
        {
            new Room { Id = 10, HotelId = 1, Status = RoomStatus.Maintenance, BaseRate = 100 },
            new Room { Id = 11, HotelId = 1, Status = RoomStatus.OutOfOrder, BaseRate = 100 }
        };
        var allRooms = activeRooms.Concat(inactiveRooms).ToList();

        _roomRepoMock.Setup(r => r.GetRoomsByHotelAsync(1))
            .ReturnsAsync(allRooms);

        var range = new DateRange(new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 2)); // 1 day

        // WHEN
        await _sut.PushBulkAvailabilityAsync(1, range);

        // THEN only active rooms (Available + Occupied) contribute — 2 rooms × 1 day = 2 calls
        _httpMock.Verify(h => h.SendRequestAsync<BookingComResponse>(
            "availability/update", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task PushBulkAvailabilityAsync_EmptyRange_MakesNoCalls()
    {
        // GIVEN Start >= End
        var rooms = BuildActiveRooms(hotelId: 1, count: 3);
        _roomRepoMock.Setup(r => r.GetRoomsByHotelAsync(1)).ReturnsAsync(rooms);

        var emptyRange = new DateRange(new DateOnly(2025, 6, 5), new DateOnly(2025, 6, 5));

        // WHEN
        await _sut.PushBulkAvailabilityAsync(1, emptyRange);

        // THEN no API calls
        _httpMock.Verify(h => h.SendRequestAsync<BookingComResponse>(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PushBulkAvailabilityAsync_CancellationRequested_ExitsCleanly()
    {
        // GIVEN many rooms and a pre-cancelled token
        var rooms = BuildActiveRooms(hotelId: 1, count: 5);
        _roomRepoMock.Setup(r => r.GetRoomsByHotelAsync(1)).ReturnsAsync(rooms);

        var range = new DateRange(new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 10));

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // already cancelled

        // WHEN / THEN: should throw OperationCanceledException (or exit without making calls)
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.PushBulkAvailabilityAsync(1, range, cts.Token));
    }

    // ─── Helper ──────────────────────────────────────────────────────────────

    private static List<Room> BuildActiveRooms(int hotelId, int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Room
            {
                Id = i,
                HotelId = hotelId,
                RoomNumber = $"10{i}",
                Status = RoomStatus.Available,
                BaseRate = 100m
            })
            .ToList();
    }
}
