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

public class ReportingServiceTests : IDisposable
{
    private readonly HotelReservationContext _context;
    private readonly Mock<ILogger<ReportingService>> _mockLogger;
    private readonly ReportingService _reportingService;

    public ReportingServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockLogger = new Mock<ILogger<ReportingService>>();
        _reportingService = new ReportingService(_context, _mockLogger.Object);
        
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
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            UpdatedAt = DateTime.UtcNow.AddDays(-60)
        };

        var hotel2 = new Hotel
        {
            Id = 2,
            Name = "Test Hotel 2",
            Address = "456 Test Ave",
            Phone = "555-0002",
            Email = "hotel2@test.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            UpdatedAt = DateTime.UtcNow.AddDays(-60)
        };

        _context.Hotels.AddRange(hotel1, hotel2);

        // Create test rooms
        var rooms = new List<Room>
        {
            new Room { Id = 1, HotelId = 1, RoomNumber = "101", Type = RoomType.Single, Capacity = 1, BaseRate = 100, Status = RoomStatus.Available, CreatedAt = DateTime.UtcNow.AddDays(-60), UpdatedAt = DateTime.UtcNow.AddDays(-60) },
            new Room { Id = 2, HotelId = 1, RoomNumber = "102", Type = RoomType.Double, Capacity = 2, BaseRate = 150, Status = RoomStatus.Available, CreatedAt = DateTime.UtcNow.AddDays(-60), UpdatedAt = DateTime.UtcNow.AddDays(-60) },
            new Room { Id = 3, HotelId = 1, RoomNumber = "103", Type = RoomType.Suite, Capacity = 4, BaseRate = 250, Status = RoomStatus.Available, CreatedAt = DateTime.UtcNow.AddDays(-60), UpdatedAt = DateTime.UtcNow.AddDays(-60) },
            new Room { Id = 4, HotelId = 2, RoomNumber = "201", Type = RoomType.Single, Capacity = 1, BaseRate = 120, Status = RoomStatus.Available, CreatedAt = DateTime.UtcNow.AddDays(-60), UpdatedAt = DateTime.UtcNow.AddDays(-60) },
            new Room { Id = 5, HotelId = 2, RoomNumber = "202", Type = RoomType.Double, Capacity = 2, BaseRate = 180, Status = RoomStatus.Available, CreatedAt = DateTime.UtcNow.AddDays(-60), UpdatedAt = DateTime.UtcNow.AddDays(-60) }
        };

        _context.Rooms.AddRange(rooms);

        // Create test guests
        var guests = new List<Guest>
        {
            new Guest { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@test.com", Phone = "555-1001", CreatedAt = DateTime.UtcNow.AddDays(-60), UpdatedAt = DateTime.UtcNow.AddDays(-60) },
            new Guest { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@test.com", Phone = "555-1002", CreatedAt = DateTime.UtcNow.AddDays(-60), UpdatedAt = DateTime.UtcNow.AddDays(-60) },
            new Guest { Id = 3, FirstName = "Bob", LastName = "Johnson", Email = "bob.johnson@test.com", Phone = "555-1003", CreatedAt = DateTime.UtcNow.AddDays(-60), UpdatedAt = DateTime.UtcNow.AddDays(-60) },
            new Guest { Id = 4, FirstName = "Alice", LastName = "Brown", Email = "alice.brown@test.com", Phone = "555-1004", CreatedAt = DateTime.UtcNow.AddDays(-60), UpdatedAt = DateTime.UtcNow.AddDays(-60) }
        };

        _context.Guests.AddRange(guests);

        // Create test reservations with varied dates and sources
        var baseDate = DateTime.Today.AddDays(-30);
        var reservations = new List<Reservation>
        {
            // Hotel 1 reservations
            new Reservation
            {
                Id = 1,
                HotelId = 1,
                RoomId = 1,
                GuestId = 1,
                BookingReference = "BK001",
                Source = ReservationSource.Manual,
                CheckInDate = baseDate,
                CheckOutDate = baseDate.AddDays(2),
                NumberOfGuests = 1,
                TotalAmount = 200,
                Status = ReservationStatus.CheckedOut,
                CreatedAt = DateTime.UtcNow.AddDays(-35),
                UpdatedAt = DateTime.UtcNow.AddDays(-28)
            },
            new Reservation
            {
                Id = 2,
                HotelId = 1,
                RoomId = 2,
                GuestId = 2,
                BookingReference = "BK002",
                Source = ReservationSource.BookingCom,
                CheckInDate = baseDate.AddDays(5),
                CheckOutDate = baseDate.AddDays(8),
                NumberOfGuests = 2,
                TotalAmount = 450,
                Status = ReservationStatus.CheckedOut,
                CreatedAt = DateTime.UtcNow.AddDays(-40),
                UpdatedAt = DateTime.UtcNow.AddDays(-22)
            },
            new Reservation
            {
                Id = 3,
                HotelId = 1,
                RoomId = 3,
                GuestId = 3,
                BookingReference = "BK003",
                Source = ReservationSource.Direct,
                CheckInDate = baseDate.AddDays(10),
                CheckOutDate = baseDate.AddDays(15),
                NumberOfGuests = 4,
                TotalAmount = 1250,
                Status = ReservationStatus.CheckedOut,
                CreatedAt = DateTime.UtcNow.AddDays(-45),
                UpdatedAt = DateTime.UtcNow.AddDays(-15)
            },
            // Hotel 2 reservations
            new Reservation
            {
                Id = 4,
                HotelId = 2,
                RoomId = 4,
                GuestId = 1, // Repeat guest
                BookingReference = "BK004",
                Source = ReservationSource.Manual,
                CheckInDate = baseDate.AddDays(3),
                CheckOutDate = baseDate.AddDays(6),
                NumberOfGuests = 1,
                TotalAmount = 360,
                Status = ReservationStatus.CheckedOut,
                CreatedAt = DateTime.UtcNow.AddDays(-38),
                UpdatedAt = DateTime.UtcNow.AddDays(-24)
            },
            new Reservation
            {
                Id = 5,
                HotelId = 2,
                RoomId = 5,
                GuestId = 4,
                BookingReference = "BK005",
                Source = ReservationSource.BookingCom,
                CheckInDate = baseDate.AddDays(12),
                CheckOutDate = baseDate.AddDays(14),
                NumberOfGuests = 2,
                TotalAmount = 360,
                Status = ReservationStatus.CheckedOut,
                CreatedAt = DateTime.UtcNow.AddDays(-42),
                UpdatedAt = DateTime.UtcNow.AddDays(-16)
            },
            // Recent reservations
            new Reservation
            {
                Id = 6,
                HotelId = 1,
                RoomId = 1,
                GuestId = 2, // Repeat guest
                BookingReference = "BK006",
                Source = ReservationSource.Manual,
                CheckInDate = baseDate.AddDays(20),
                CheckOutDate = baseDate.AddDays(23),
                NumberOfGuests = 1,
                TotalAmount = 300,
                Status = ReservationStatus.CheckedOut,
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow.AddDays(-7)
            }
        };

        _context.Reservations.AddRange(reservations);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GenerateOccupancyReportAsync_ShouldReturnValidReport()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-30);
        var endDate = DateTime.Today;

        // Act
        var result = await _reportingService.GenerateOccupancyReportAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(endDate, result.EndDate);
        Assert.True(result.TotalRooms > 0);
        Assert.True(result.OverallOccupancyRate >= 0);
        Assert.NotNull(result.DailyOccupancy);
        Assert.NotNull(result.RoomTypeBreakdown);
        Assert.NotNull(result.HotelBreakdown);
    }

    [Fact]
    public async Task GenerateOccupancyReportAsync_WithHotelFilter_ShouldFilterByHotel()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-30);
        var endDate = DateTime.Today;
        var hotelId = 1;

        // Act
        var result = await _reportingService.GenerateOccupancyReportAsync(startDate, endDate, hotelId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(hotelId, result.HotelId);
        Assert.Equal("Test Hotel 1", result.HotelName);
        Assert.Equal(3, result.TotalRooms); // Hotel 1 has 3 rooms
        Assert.Empty(result.HotelBreakdown); // Should be empty when filtering by hotel
    }

    [Fact]
    public async Task GenerateRevenueReportAsync_ShouldReturnValidReport()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-30);
        var endDate = DateTime.Today;

        // Act
        var result = await _reportingService.GenerateRevenueReportAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(endDate, result.EndDate);
        Assert.True(result.TotalRevenue >= 0);
        Assert.True(result.AverageRevenuePerDay >= 0);
        Assert.True(result.AverageRevenuePerReservation >= 0);
        Assert.NotNull(result.DailyRevenue);
        Assert.NotNull(result.MonthlyRevenue);
        Assert.NotNull(result.RevenueBySource);
        Assert.NotNull(result.RevenueByRoomType);
    }

    [Fact]
    public async Task GenerateGuestPatternReportAsync_ShouldReturnValidReport()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-30);
        var endDate = DateTime.Today;

        // Act
        var result = await _reportingService.GenerateGuestPatternReportAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(endDate, result.EndDate);
        Assert.True(result.TotalGuests >= 0);
        Assert.True(result.UniqueGuests >= 0);
        Assert.True(result.RepeatGuests >= 0);
        Assert.True(result.RepeatGuestPercentage >= 0);
        Assert.NotNull(result.BookingSourcePatterns);
        Assert.NotNull(result.StayDurationPatterns);
        Assert.NotNull(result.SeasonalPatterns);
        Assert.NotNull(result.GuestLoyalty);
    }

    [Fact]
    public async Task GetDailyOccupancyAsync_ShouldReturnDailyData()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-7);
        var endDate = DateTime.Today;

        // Act
        var result = await _reportingService.GetDailyOccupancyAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(7, result.Count); // 7 days
        
        foreach (var day in result)
        {
            Assert.True(day.OccupancyRate >= 0 && day.OccupancyRate <= 100);
            Assert.True(day.TotalRooms >= 0);
            Assert.True(day.OccupiedRooms >= 0);
            Assert.True(day.AvailableRooms >= 0);
            Assert.True(day.Revenue >= 0);
            Assert.True(day.ReservationCount >= 0);
        }
    }

    [Fact]
    public async Task GetMonthlyRevenueAsync_ShouldReturnMonthlyData()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-60);
        var endDate = DateTime.Today;

        // Act
        var result = await _reportingService.GetMonthlyRevenueAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        
        foreach (var month in result)
        {
            Assert.True(month.Year > 0);
            Assert.True(month.Month >= 1 && month.Month <= 12);
            Assert.False(string.IsNullOrEmpty(month.MonthName));
            Assert.True(month.Revenue >= 0);
            Assert.True(month.ReservationCount >= 0);
            Assert.True(month.AverageRevenuePerReservation >= 0);
        }
    }

    [Fact]
    public async Task GetRevenueBySourceAsync_ShouldReturnSourceBreakdown()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-30);
        var endDate = DateTime.Today;

        // Act
        var result = await _reportingService.GetRevenueBySourceAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        
        var totalPercentage = result.Sum(r => r.Percentage);
        Assert.True(Math.Abs(totalPercentage - 100) < 0.01m || totalPercentage == 0); // Should sum to 100% or be 0 if no data
        
        foreach (var source in result)
        {
            Assert.False(string.IsNullOrEmpty(source.SourceName));
            Assert.True(source.Revenue >= 0);
            Assert.True(source.ReservationCount >= 0);
            Assert.True(source.Percentage >= 0);
            Assert.True(source.AverageRevenuePerReservation >= 0);
        }
    }

    [Fact]
    public async Task GetRevenueByRoomTypeAsync_ShouldReturnRoomTypeBreakdown()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-30);
        var endDate = DateTime.Today;

        // Act
        var result = await _reportingService.GetRevenueByRoomTypeAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        
        var totalPercentage = result.Sum(r => r.Percentage);
        Assert.True(Math.Abs(totalPercentage - 100) < 0.01m || totalPercentage == 0); // Should sum to 100% or be 0 if no data
        
        foreach (var roomType in result)
        {
            Assert.False(string.IsNullOrEmpty(roomType.RoomTypeName));
            Assert.True(roomType.Revenue >= 0);
            Assert.True(roomType.ReservationCount >= 0);
            Assert.True(roomType.Percentage >= 0);
            Assert.True(roomType.AverageRate >= 0);
        }
    }

    [Fact]
    public async Task GetBookingSourcePatternsAsync_ShouldReturnPatterns()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-30);
        var endDate = DateTime.Today;

        // Act
        var result = await _reportingService.GetBookingSourcePatternsAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        
        var totalPercentage = result.Sum(r => r.Percentage);
        Assert.True(Math.Abs(totalPercentage - 100) < 0.01m || totalPercentage == 0); // Should sum to 100% or be 0 if no data
        
        foreach (var pattern in result)
        {
            Assert.False(string.IsNullOrEmpty(pattern.SourceName));
            Assert.True(pattern.ReservationCount >= 0);
            Assert.True(pattern.Percentage >= 0);
            Assert.True(pattern.AverageLeadTime >= 0);
            Assert.True(pattern.AverageStayDuration >= 0);
            Assert.True(pattern.AverageRevenue >= 0);
        }
    }

    [Fact]
    public async Task GetGuestLoyaltyAnalysisAsync_ShouldReturnLoyaltyData()
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-30);
        var endDate = DateTime.Today;

        // Act
        var result = await _reportingService.GetGuestLoyaltyAnalysisAsync(startDate, endDate);

        // Assert
        Assert.NotNull(result);
        // Note: May be empty if no repeat guests in test data
        
        foreach (var guest in result)
        {
            Assert.False(string.IsNullOrEmpty(guest.GuestName));
            Assert.True(guest.TotalReservations > 1); // Should only include repeat guests
            Assert.True(guest.TotalRevenue > 0);
            Assert.True(guest.AverageStayDuration >= 0);
            Assert.True(guest.FirstStay <= guest.LastStay);
        }
    }

    [Fact]
    public async Task ExportReportAsync_ShouldReturnExportData()
    {
        // Arrange
        var request = new ReportFilterRequest
        {
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today,
            ReportType = ReportType.Occupancy,
            ExportFormat = ExportFormat.Json
        };

        // Act
        var result = await _reportingService.ExportReportAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.FileName));
        Assert.False(string.IsNullOrEmpty(result.ContentType));
        Assert.True(result.Data.Length > 0);
        Assert.Equal(ExportFormat.Json, result.Format);
    }

    [Fact]
    public async Task CalculateOccupancyVarianceAsync_ShouldReturnVariance()
    {
        // Arrange
        var currentStart = DateTime.Today.AddDays(-15);
        var currentEnd = DateTime.Today;
        var previousStart = DateTime.Today.AddDays(-30);
        var previousEnd = DateTime.Today.AddDays(-15);

        // Act
        var result = await _reportingService.CalculateOccupancyVarianceAsync(
            currentStart, currentEnd, previousStart, previousEnd);

        // Assert
        Assert.True(result >= -100); // Variance can't be less than -100%
    }

    [Fact]
    public async Task CalculateRevenueVarianceAsync_ShouldReturnVariance()
    {
        // Arrange
        var currentStart = DateTime.Today.AddDays(-15);
        var currentEnd = DateTime.Today;
        var previousStart = DateTime.Today.AddDays(-30);
        var previousEnd = DateTime.Today.AddDays(-15);

        // Act
        var result = await _reportingService.CalculateRevenueVarianceAsync(
            currentStart, currentEnd, previousStart, previousEnd);

        // Assert
        Assert.True(result >= -100); // Variance can't be less than -100%
    }

    [Theory]
    [InlineData(ReportType.Occupancy)]
    [InlineData(ReportType.Revenue)]
    [InlineData(ReportType.GuestPattern)]
    public async Task ExportReportAsync_WithDifferentTypes_ShouldSucceed(ReportType reportType)
    {
        // Arrange
        var request = new ReportFilterRequest
        {
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today,
            ReportType = reportType,
            ExportFormat = ExportFormat.Json
        };

        // Act
        var result = await _reportingService.ExportReportAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(reportType.ToString(), result.FileName);
        Assert.True(result.Data.Length > 0);
    }

    [Theory]
    [InlineData(ExportFormat.Json, "application/json")]
    [InlineData(ExportFormat.Pdf, "application/pdf")]
    [InlineData(ExportFormat.Excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData(ExportFormat.Csv, "text/csv")]
    public async Task ExportReportAsync_WithDifferentFormats_ShouldReturnCorrectContentType(
        ExportFormat format, string expectedContentType)
    {
        // Arrange
        var request = new ReportFilterRequest
        {
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today,
            ReportType = ReportType.Occupancy,
            ExportFormat = format
        };

        // Act
        var result = await _reportingService.ExportReportAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedContentType, result.ContentType);
        Assert.Equal(format, result.Format);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}