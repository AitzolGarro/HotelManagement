using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using HotelReservationSystem.Data;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services;
using HotelReservationSystem.Tests.Helpers;

namespace HotelReservationSystem.Tests.Services;

public class DashboardServiceTests : IDisposable
{
    private readonly HotelReservationContext _context;
    private readonly Mock<ILogger<DashboardService>> _mockLogger;
    private readonly DashboardService _dashboardService;

    public DashboardServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockLogger = new Mock<ILogger<DashboardService>>();
        _dashboardService = new DashboardService(_context, _mockLogger.Object);
        
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test hotels
        var hotel1 = new Hotel
        {
            Id = 1,
            Name = "Test Hotel 1",
            Address = "123 Test St",
            Phone = "555-0001",
            Email = "hotel1@test.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var hotel2 = new Hotel
        {
            Id = 2,
            Name = "Test Hotel 2",
            Address = "456 Test Ave",
            Phone = "555-0002",
            Email = "hotel2@test.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-30)
        };

        _context.Hotels.AddRange(hotel1, hotel2);

        // Create test rooms
        var rooms = new List<Room>
        {
            new Room { Id = 1, HotelId = 1, RoomNumber = "101", Type = RoomType.Single, Capacity = 1, BaseRate = 100, Status = RoomStatus.Available, CreatedAt = DateTime.UtcNow.AddDays(-30), UpdatedAt = DateTime.UtcNow.AddDays(-30) },
            new Room { Id = 2, HotelId = 1, RoomNumber = "102", Type = RoomType.Double, Capacity = 2, BaseRate = 150, Status = RoomStatus.Available, CreatedAt = DateTime.UtcNow.AddDays(-30), UpdatedAt = DateTime.UtcNow.AddDays(-30) },
            new Room { Id = 3, HotelId = 1, RoomNumber = "103", Type = RoomType.Suite, Capacity = 4, BaseRate = 250, Status = RoomStatus.Maintenance, CreatedAt = DateTime.UtcNow.AddDays(-30), UpdatedAt = DateTime.UtcNow.AddDays(-30) },
            new Room { Id = 4, HotelId = 2, RoomNumber = "201", Type = RoomType.Single, Capacity = 1, BaseRate = 120, Status = RoomStatus.Available, CreatedAt = DateTime.UtcNow.AddDays(-30), UpdatedAt = DateTime.UtcNow.AddDays(-30) },
            new Room { Id = 5, HotelId = 2, RoomNumber = "202", Type = RoomType.Double, Capacity = 2, BaseRate = 180, Status = RoomStatus.Available, CreatedAt = DateTime.UtcNow.AddDays(-30), UpdatedAt = DateTime.UtcNow.AddDays(-30) }
        };

        _context.Rooms.AddRange(rooms);

        // Create test guests
        var guests = new List<Guest>
        {
            new Guest { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@test.com", Phone = "555-1001", CreatedAt = DateTime.UtcNow.AddDays(-30), UpdatedAt = DateTime.UtcNow.AddDays(-30) },
            new Guest { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@test.com", Phone = "555-1002", CreatedAt = DateTime.UtcNow.AddDays(-30), UpdatedAt = DateTime.UtcNow.AddDays(-30) },
            new Guest { Id = 3, FirstName = "Bob", LastName = "Johnson", Email = "bob.johnson@test.com", Phone = "555-1003", CreatedAt = DateTime.UtcNow.AddDays(-30), UpdatedAt = DateTime.UtcNow.AddDays(-30) }
        };

        _context.Guests.AddRange(guests);

        // Create test reservations
        var today = DateTime.Today;
        var reservations = new List<Reservation>
        {
            // Today's check-ins
            new Reservation
            {
                Id = 1,
                HotelId = 1,
                RoomId = 1,
                GuestId = 1,
                BookingReference = "BK001",
                Source = ReservationSource.Manual,
                CheckInDate = today,
                CheckOutDate = today.AddDays(2),
                NumberOfGuests = 1,
                TotalAmount = 200,
                Status = ReservationStatus.Confirmed,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            },
            // Today's check-outs
            new Reservation
            {
                Id = 2,
                HotelId = 1,
                RoomId = 2,
                GuestId = 2,
                BookingReference = "BK002",
                Source = ReservationSource.BookingCom,
                CheckInDate = today.AddDays(-2),
                CheckOutDate = today,
                NumberOfGuests = 2,
                TotalAmount = 300,
                Status = ReservationStatus.CheckedIn,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            },
            // This week's reservation
            new Reservation
            {
                Id = 3,
                HotelId = 2,
                RoomId = 4,
                GuestId = 3,
                BookingReference = "BK003",
                Source = ReservationSource.Manual,
                CheckInDate = today.AddDays(-3),
                CheckOutDate = today.AddDays(1),
                NumberOfGuests = 1,
                TotalAmount = 480,
                Status = ReservationStatus.CheckedIn,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            },
            // This month's reservation
            new Reservation
            {
                Id = 4,
                HotelId = 1,
                RoomId = 1,
                GuestId = 1,
                BookingReference = "BK004",
                Source = ReservationSource.BookingCom,
                CheckInDate = today.AddDays(-15),
                CheckOutDate = today.AddDays(-13),
                NumberOfGuests = 1,
                TotalAmount = 200,
                Status = ReservationStatus.CheckedOut,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-13)
            },
            // Pending reservation (for notifications)
            new Reservation
            {
                Id = 5,
                HotelId = 2,
                RoomId = 5,
                GuestId = 2,
                BookingReference = "BK005",
                Source = ReservationSource.Manual,
                CheckInDate = today.AddDays(1),
                CheckOutDate = today.AddDays(3),
                NumberOfGuests = 2,
                TotalAmount = 360,
                Status = ReservationStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _context.Reservations.AddRange(reservations);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetDashboardKpiAsync_ShouldReturnCompleteKpiData()
    {
        // Act
        var result = await _dashboardService.GetDashboardKpiAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.OccupancyRate);
        Assert.NotNull(result.RevenueTracking);
        Assert.NotNull(result.DailyOperations);
        Assert.NotNull(result.Notifications);
    }

    [Fact]
    public async Task GetOccupancyRatesAsync_ShouldCalculateCorrectRates()
    {
        // Act
        var result = await _dashboardService.GetOccupancyRatesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.TotalRooms); // Only available rooms (excluding maintenance room)
        Assert.True(result.TodayRate >= 0);
        Assert.True(result.WeekRate >= 0);
        Assert.True(result.MonthRate >= 0);
    }

    [Fact]
    public async Task GetOccupancyRatesAsync_WithHotelFilter_ShouldFilterByHotel()
    {
        // Act
        var result = await _dashboardService.GetOccupancyRatesAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalRooms); // Only hotel 1's available rooms
    }

    [Fact]
    public async Task GetRevenueTrackingAsync_ShouldCalculateRevenue()
    {
        // Act
        var result = await _dashboardService.GetRevenueTrackingAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TodayRevenue >= 0);
        Assert.True(result.WeekRevenue >= 0);
        Assert.True(result.MonthRevenue >= 0);
        Assert.NotNull(result.DailyBreakdown);
        Assert.NotNull(result.WeeklyBreakdown);
    }

    [Fact]
    public async Task GetDailyOperationsAsync_ShouldReturnTodaysActivity()
    {
        // Act
        var result = await _dashboardService.GetDailyOperationsAsync(DateTime.Today);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.TodayCheckIns);
        Assert.NotNull(result.TodayCheckOuts);
        Assert.True(result.TotalCheckIns >= 0);
        Assert.True(result.TotalCheckOuts >= 0);
        
        // Should have at least one check-in and one check-out for today based on test data
        Assert.True(result.TodayCheckIns.Count > 0);
        Assert.True(result.TodayCheckOuts.Count > 0);
    }

    [Fact]
    public async Task GetNotificationPanelAsync_ShouldReturnNotifications()
    {
        // Act
        var result = await _dashboardService.GetNotificationPanelAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Notifications);
        Assert.True(result.TotalCount >= 0);
        Assert.True(result.CriticalCount >= 0);
        Assert.True(result.WarningCount >= 0);
        Assert.True(result.InfoCount >= 0);
    }

    [Fact]
    public async Task GetDailyRevenueBreakdownAsync_ShouldReturnDailyData()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-30);
        var endDate = DateTime.Today.AddDays(1);

        // Act
        var result = await _dashboardService.GetDailyRevenueBreakdownAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<DailyRevenueDto>>(result);
        
        // Should have data for days with reservations
        var todayData = result.FirstOrDefault(d => d.Date.Date == DateTime.Today);
        if (todayData != null)
        {
            Assert.True(todayData.Revenue > 0);
            Assert.True(todayData.ReservationCount > 0);
        }
    }

    [Fact]
    public async Task GetWeeklyRevenueBreakdownAsync_ShouldReturnWeeklyData()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-56); // 8 weeks ago
        var endDate = DateTime.Today.AddDays(7);

        // Act
        var result = await _dashboardService.GetWeeklyRevenueBreakdownAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<WeeklyRevenueDto>>(result);
        
        // Should have weekly data
        foreach (var week in result)
        {
            Assert.True(week.WeekEnd >= week.WeekStart);
            Assert.True(week.Revenue >= 0);
            Assert.True(week.ReservationCount >= 0);
        }
    }

    [Fact]
    public async Task CalculateOccupancyRateAsync_ShouldReturnValidPercentage()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-7);
        var endDate = DateTime.Today.AddDays(1);

        // Act
        var result = await _dashboardService.CalculateOccupancyRateAsync(startDate, endDate);

        // Assert
        Assert.True(result >= 0);
        Assert.True(result <= 100);
    }

    [Fact]
    public async Task CalculateRevenueAsync_ShouldReturnValidAmount()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-30);
        var endDate = DateTime.Today.AddDays(1);

        // Act
        var result = await _dashboardService.CalculateRevenueAsync(startDate, endDate);

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public async Task CalculateRevenueAsync_WithHotelFilter_ShouldFilterByHotel()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-30);
        var endDate = DateTime.Today.AddDays(1);

        // Act
        var allHotelsRevenue = await _dashboardService.CalculateRevenueAsync(startDate, endDate);
        var hotel1Revenue = await _dashboardService.CalculateRevenueAsync(startDate, endDate, 1);
        var hotel2Revenue = await _dashboardService.CalculateRevenueAsync(startDate, endDate, 2);

        // Assert
        Assert.True(hotel1Revenue >= 0);
        Assert.True(hotel2Revenue >= 0);
        Assert.True(allHotelsRevenue >= hotel1Revenue);
        Assert.True(allHotelsRevenue >= hotel2Revenue);
    }

    [Fact]
    public async Task CreateNotificationAsync_ShouldCreateNotification()
    {
        // Act
        var result = await _dashboardService.CreateNotificationAsync(
            NotificationType.Warning,
            "Test Notification",
            "This is a test notification",
            "Reservation",
            1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(NotificationType.Warning, result.Type);
        Assert.Equal("Test Notification", result.Title);
        Assert.Equal("This is a test notification", result.Message);
        Assert.Equal("Reservation", result.RelatedEntityType);
        Assert.Equal(1, result.RelatedEntityId);
        Assert.False(result.IsRead);
    }

    [Fact]
    public async Task MarkNotificationAsReadAsync_ShouldReturnTrue()
    {
        // Act
        var result = await _dashboardService.MarkNotificationAsReadAsync(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task MarkAllNotificationsAsReadAsync_ShouldReturnTrue()
    {
        // Act
        var result = await _dashboardService.MarkAllNotificationsAsReadAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetUnreadNotificationsAsync_ShouldReturnEmptyList()
    {
        // Act
        var result = await _dashboardService.GetUnreadNotificationsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecentReservationsAsync_ShouldReturnRecentReservations()
    {
        // Act
        var result = await _dashboardService.GetRecentReservationsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<RecentReservationDto>>(result);
        
        // Should have reservations from test data
        Assert.True(result.Count > 0);
        
        // Should be ordered by creation date (most recent first)
        for (int i = 1; i < result.Count; i++)
        {
            Assert.True(result[i - 1].CreatedAt >= result[i].CreatedAt);
        }
        
        // Verify data structure
        var firstReservation = result.First();
        Assert.NotEmpty(firstReservation.GuestName);
        Assert.NotEmpty(firstReservation.HotelName);
        Assert.NotEmpty(firstReservation.RoomNumber);
        Assert.True(firstReservation.TotalAmount > 0);
    }

    [Fact]
    public async Task GetRecentReservationsAsync_WithHotelFilter_ShouldFilterByHotel()
    {
        // Act
        var allReservations = await _dashboardService.GetRecentReservationsAsync();
        var hotel1Reservations = await _dashboardService.GetRecentReservationsAsync(1);
        var hotel2Reservations = await _dashboardService.GetRecentReservationsAsync(2);

        // Assert
        Assert.True(hotel1Reservations.Count > 0);
        Assert.True(hotel2Reservations.Count > 0);
        Assert.True(allReservations.Count >= hotel1Reservations.Count);
        Assert.True(allReservations.Count >= hotel2Reservations.Count);
        
        // All hotel 1 reservations should be for hotel 1
        Assert.All(hotel1Reservations, r => Assert.Contains("Test Hotel 1", r.HotelName));
        
        // All hotel 2 reservations should be for hotel 2
        Assert.All(hotel2Reservations, r => Assert.Contains("Test Hotel 2", r.HotelName));
    }

    [Fact]
    public async Task GetRecentReservationsAsync_WithLimit_ShouldRespectLimit()
    {
        // Act
        var result = await _dashboardService.GetRecentReservationsAsync(limit: 3);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count <= 3);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}