using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.Data;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services;

public class DashboardService : IDashboardService
{
    private readonly HotelReservationContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(HotelReservationContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardKpiDto> GetDashboardKpiAsync(int? hotelId = null)
    {
        _logger.LogInformation("Getting dashboard KPIs for hotel {HotelId}", hotelId);

        var occupancyRate = await GetOccupancyRatesAsync(hotelId);
        var revenueTracking = await GetRevenueTrackingAsync(hotelId);
        var dailyOperations = await GetDailyOperationsAsync(DateTime.Today, hotelId);
        var notifications = await GetNotificationPanelAsync(hotelId);

        return new DashboardKpiDto
        {
            OccupancyRate = occupancyRate,
            RevenueTracking = revenueTracking,
            DailyOperations = dailyOperations,
            Notifications = notifications
        };
    }

    public async Task<OccupancyRateDto> GetOccupancyRatesAsync(int? hotelId = null)
    {
        _logger.LogInformation("Calculating occupancy rates for hotel {HotelId}", hotelId);

        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var roomsQuery = _context.Rooms.AsQueryable();
        if (hotelId.HasValue)
        {
            roomsQuery = roomsQuery.Where(r => r.HotelId == hotelId.Value);
        }
        roomsQuery = roomsQuery.Where(r => r.Status == RoomStatus.Available);

        var totalRooms = await roomsQuery.CountAsync();

        // Today's occupancy
        var todayOccupied = await GetOccupiedRoomsCountAsync(today, today.AddDays(1), hotelId);
        var todayRate = totalRooms > 0 ? (decimal)todayOccupied / totalRooms * 100 : 0;

        // Week occupancy (average)
        var weekOccupied = await GetAverageOccupiedRoomsAsync(weekStart, weekStart.AddDays(7), hotelId);
        var weekRate = totalRooms > 0 ? weekOccupied / totalRooms * 100 : 0;

        // Month occupancy (average)
        var monthOccupied = await GetAverageOccupiedRoomsAsync(monthStart, monthStart.AddMonths(1), hotelId);
        var monthRate = totalRooms > 0 ? monthOccupied / totalRooms * 100 : 0;

        return new OccupancyRateDto
        {
            TodayRate = Math.Round(todayRate, 2),
            WeekRate = Math.Round(weekRate, 2),
            MonthRate = Math.Round(monthRate, 2),
            TotalRooms = totalRooms,
            OccupiedRoomsToday = todayOccupied,
            OccupiedRoomsWeek = (int)Math.Round(weekOccupied),
            OccupiedRoomsMonth = (int)Math.Round(monthOccupied)
        };
    }

    public async Task<RevenueTrackingDto> GetRevenueTrackingAsync(int? hotelId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        _logger.LogInformation("Calculating revenue tracking for hotel {HotelId}", hotelId);

        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var lastMonthStart = monthStart.AddMonths(-1);

        // Today's revenue
        var todayRevenue = await CalculateRevenueAsync(today, today.AddDays(1), hotelId);

        // Week revenue
        var weekRevenue = await CalculateRevenueAsync(weekStart, weekStart.AddDays(7), hotelId);

        // Month revenue
        var monthRevenue = await CalculateRevenueAsync(monthStart, monthStart.AddMonths(1), hotelId);

        // Last month revenue for comparison
        var lastMonthRevenue = await CalculateRevenueAsync(lastMonthStart, monthStart, hotelId);

        // Projected month revenue based on current pace
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var daysPassed = today.Day;
        var projectedMonthRevenue = daysPassed > 0 ? monthRevenue / daysPassed * daysInMonth : 0;

        // Calculate variances
        var weeklyVariance = lastMonthRevenue > 0 ? (weekRevenue - (lastMonthRevenue / 4)) / (lastMonthRevenue / 4) * 100 : 0;
        var monthlyVariance = lastMonthRevenue > 0 ? (monthRevenue - lastMonthRevenue) / lastMonthRevenue * 100 : 0;

        // Get daily breakdown for the current month
        var dailyBreakdown = await GetDailyRevenueBreakdownAsync(monthStart, monthStart.AddMonths(1), hotelId);

        // Get weekly breakdown for the last 8 weeks
        var weeklyBreakdown = await GetWeeklyRevenueBreakdownAsync(weekStart.AddDays(-49), weekStart.AddDays(7), hotelId);

        return new RevenueTrackingDto
        {
            TodayRevenue = todayRevenue,
            WeekRevenue = weekRevenue,
            MonthRevenue = monthRevenue,
            ProjectedMonthRevenue = projectedMonthRevenue,
            LastMonthRevenue = lastMonthRevenue,
            WeeklyVariance = Math.Round(weeklyVariance, 2),
            MonthlyVariance = Math.Round(monthlyVariance, 2),
            DailyBreakdown = dailyBreakdown,
            WeeklyBreakdown = weeklyBreakdown
        };
    }

    public async Task<DailyOperationsDto> GetDailyOperationsAsync(DateTime? date = null, int? hotelId = null)
    {
        var targetDate = date ?? DateTime.Today;
        _logger.LogInformation("Getting daily operations for {Date} and hotel {HotelId}", targetDate, hotelId);

        var checkInsQuery = _context.Reservations
            .Include(r => r.Guest)
            .Include(r => r.Room)
            .Include(r => r.Hotel)
            .Where(r => r.CheckInDate.Date == targetDate.Date)
            .Where(r => r.Status != ReservationStatus.Cancelled);

        var checkOutsQuery = _context.Reservations
            .Include(r => r.Guest)
            .Include(r => r.Room)
            .Include(r => r.Hotel)
            .Where(r => r.CheckOutDate.Date == targetDate.Date)
            .Where(r => r.Status != ReservationStatus.Cancelled);

        if (hotelId.HasValue)
        {
            checkInsQuery = checkInsQuery.Where(r => r.HotelId == hotelId.Value);
            checkOutsQuery = checkOutsQuery.Where(r => r.HotelId == hotelId.Value);
        }

        var checkIns = await checkInsQuery
            .Select(r => new CheckInOutDto
            {
                ReservationId = r.Id,
                BookingReference = r.BookingReference ?? "",
                GuestName = $"{r.Guest.FirstName} {r.Guest.LastName}",
                GuestPhone = r.Guest.Phone ?? "",
                RoomNumber = r.Room.RoomNumber,
                HotelName = r.Hotel.Name,
                CheckInDate = r.CheckInDate,
                CheckOutDate = r.CheckOutDate,
                Status = r.Status,
                SpecialRequests = r.SpecialRequests ?? ""
            })
            .OrderBy(c => c.GuestName)
            .ToListAsync();

        var checkOuts = await checkOutsQuery
            .Select(r => new CheckInOutDto
            {
                ReservationId = r.Id,
                BookingReference = r.BookingReference ?? "",
                GuestName = $"{r.Guest.FirstName} {r.Guest.LastName}",
                GuestPhone = r.Guest.Phone ?? "",
                RoomNumber = r.Room.RoomNumber,
                HotelName = r.Hotel.Name,
                CheckInDate = r.CheckInDate,
                CheckOutDate = r.CheckOutDate,
                Status = r.Status,
                SpecialRequests = r.SpecialRequests ?? ""
            })
            .OrderBy(c => c.GuestName)
            .ToListAsync();

        return new DailyOperationsDto
        {
            TodayCheckIns = checkIns,
            TodayCheckOuts = checkOuts,
            TotalCheckIns = checkIns.Count,
            TotalCheckOuts = checkOuts.Count
        };
    }

    public async Task<NotificationPanelDto> GetNotificationPanelAsync(int? hotelId = null)
    {
        _logger.LogInformation("Getting notification panel for hotel {HotelId}", hotelId);

        // For now, we'll create notifications based on system conditions
        // In a real system, these would be stored in a notifications table
        var notifications = new List<SystemNotificationDto>();

        // Check for overbookings
        await CheckForOverbookings(notifications, hotelId);

        // Check for maintenance conflicts
        await CheckForMaintenanceConflicts(notifications, hotelId);

        // Check for upcoming check-ins without confirmed status
        await CheckForUnconfirmedCheckIns(notifications, hotelId);

        var criticalCount = notifications.Count(n => n.Type == NotificationType.Critical || n.Type == NotificationType.Overbooking);
        var warningCount = notifications.Count(n => n.Type == NotificationType.Warning || n.Type == NotificationType.MaintenanceConflict);
        var infoCount = notifications.Count(n => n.Type == NotificationType.Info);

        return new NotificationPanelDto
        {
            Notifications = notifications.OrderByDescending(n => n.CreatedAt).Take(10).ToList(),
            TotalCount = notifications.Count,
            CriticalCount = criticalCount,
            WarningCount = warningCount,
            InfoCount = infoCount
        };
    }

    public async Task<SystemNotificationDto> CreateNotificationAsync(NotificationType type, string title, string message, string? relatedEntityType = null, int? relatedEntityId = null)
    {
        // In a real implementation, this would save to a notifications table
        return new SystemNotificationDto
        {
            Id = new Random().Next(1000, 9999),
            Type = type,
            Title = title,
            Message = message,
            CreatedAt = DateTime.UtcNow,
            IsRead = false,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId
        };
    }

    public async Task<bool> MarkNotificationAsReadAsync(int notificationId)
    {
        // In a real implementation, this would update the notifications table
        return true;
    }

    public async Task<bool> MarkAllNotificationsAsReadAsync(int? hotelId = null)
    {
        // In a real implementation, this would update all notifications for the hotel
        return true;
    }

    public async Task<List<SystemNotificationDto>> GetUnreadNotificationsAsync(int? hotelId = null)
    {
        // In a real implementation, this would query unread notifications from the database
        return new List<SystemNotificationDto>();
    }

    public async Task<List<DailyRevenueDto>> GetDailyRevenueBreakdownAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var query = _context.Reservations.AsQueryable();

        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        query = query.Where(r => r.CheckInDate >= startDate && r.CheckInDate < endDate)
                    .Where(r => r.Status != ReservationStatus.Cancelled);

        var dailyData = await query
            .GroupBy(r => r.CheckInDate.Date)
            .Select(g => new DailyRevenueDto
            {
                Date = g.Key,
                Revenue = g.Sum(r => r.TotalAmount),
                ReservationCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        return dailyData;
    }

    public async Task<List<WeeklyRevenueDto>> GetWeeklyRevenueBreakdownAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var query = _context.Reservations.AsQueryable();

        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        var reservations = await query
            .Where(r => r.CheckInDate >= startDate && r.CheckInDate < endDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .Select(r => new { r.CheckInDate, r.TotalAmount })
            .ToListAsync();

        var weeklyData = reservations
            .GroupBy(r => GetWeekStart(r.CheckInDate))
            .Select(g => new WeeklyRevenueDto
            {
                WeekStart = g.Key,
                WeekEnd = g.Key.AddDays(6),
                Revenue = g.Sum(r => r.TotalAmount),
                ReservationCount = g.Count()
            })
            .OrderBy(w => w.WeekStart)
            .ToList();

        return weeklyData;
    }

    public async Task<decimal> CalculateOccupancyRateAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var totalRooms = await _context.Rooms
            .Where(r => hotelId == null || r.HotelId == hotelId.Value)
            .Where(r => r.Status == RoomStatus.Available)
            .CountAsync();

        if (totalRooms == 0) return 0;

        var averageOccupied = await GetAverageOccupiedRoomsAsync(startDate, endDate, hotelId);
        return averageOccupied / totalRooms * 100;
    }

    public async Task<decimal> CalculateRevenueAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var query = _context.Reservations.AsQueryable();

        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        return await query
            .Where(r => r.CheckInDate >= startDate && r.CheckInDate < endDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .SumAsync(r => r.TotalAmount);
    }

    public async Task<List<RecentReservationDto>> GetRecentReservationsAsync(int? hotelId = null, int limit = 10)
    {
        _logger.LogInformation("Getting recent reservations for hotel {HotelId}, limit {Limit}", hotelId, limit);

        var query = _context.Reservations
            .Include(r => r.Guest)
            .Include(r => r.Room)
            .Include(r => r.Hotel)
            .AsQueryable();

        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        var recentReservations = await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .Select(r => new RecentReservationDto
            {
                Id = r.Id,
                BookingReference = r.BookingReference ?? "",
                GuestName = $"{r.Guest.FirstName} {r.Guest.LastName}",
                HotelName = r.Hotel.Name,
                RoomNumber = r.Room.RoomNumber,
                CheckInDate = r.CheckInDate,
                CheckOutDate = r.CheckOutDate,
                Status = r.Status,
                Source = r.Source,
                TotalAmount = r.TotalAmount,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return recentReservations;
    }

    // Private helper methods

    private async Task<int> GetOccupiedRoomsCountAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var query = _context.Reservations.AsQueryable();

        if (hotelId.HasValue)
        {
            query = query.Where(r => r.HotelId == hotelId.Value);
        }

        return await query
            .Where(r => r.CheckInDate < endDate && r.CheckOutDate > startDate)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .Select(r => r.RoomId)
            .Distinct()
            .CountAsync();
    }

    private async Task<decimal> GetAverageOccupiedRoomsAsync(DateTime startDate, DateTime endDate, int? hotelId = null)
    {
        var totalDays = (endDate - startDate).Days;
        if (totalDays <= 0) return 0;

        var totalOccupiedRoomDays = 0;

        for (var date = startDate; date < endDate; date = date.AddDays(1))
        {
            var occupiedRooms = await GetOccupiedRoomsCountAsync(date, date.AddDays(1), hotelId);
            totalOccupiedRoomDays += occupiedRooms;
        }

        return (decimal)totalOccupiedRoomDays / totalDays;
    }

    private DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    private async Task CheckForOverbookings(List<SystemNotificationDto> notifications, int? hotelId)
    {
        var today = DateTime.Today;
        var nextWeek = today.AddDays(7);

        var overbookings = await _context.Reservations
            .Where(r => hotelId == null || r.HotelId == hotelId.Value)
            .Where(r => r.CheckInDate >= today && r.CheckInDate <= nextWeek)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .GroupBy(r => new { r.RoomId, r.CheckInDate.Date })
            .Where(g => g.Count() > 1)
            .Select(g => new { g.Key.RoomId, Date = g.Key.Date, Count = g.Count() })
            .ToListAsync();

        foreach (var overbooking in overbookings)
        {
            notifications.Add(new SystemNotificationDto
            {
                Id = new Random().Next(1000, 9999),
                Type = NotificationType.Overbooking,
                Title = "Overbooking Detected",
                Message = $"Room {overbooking.RoomId} has {overbooking.Count} reservations for {overbooking.Date:MMM dd}",
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                RelatedEntityType = "Room",
                RelatedEntityId = overbooking.RoomId
            });
        }
    }

    private async Task CheckForMaintenanceConflicts(List<SystemNotificationDto> notifications, int? hotelId)
    {
        var maintenanceRooms = await _context.Rooms
            .Where(r => hotelId == null || r.HotelId == hotelId.Value)
            .Where(r => r.Status == RoomStatus.Maintenance)
            .Select(r => r.Id)
            .ToListAsync();

        var today = DateTime.Today;
        var nextWeek = today.AddDays(7);

        var conflicts = await _context.Reservations
            .Where(r => maintenanceRooms.Contains(r.RoomId))
            .Where(r => r.CheckInDate >= today && r.CheckInDate <= nextWeek)
            .Where(r => r.Status != ReservationStatus.Cancelled)
            .CountAsync();

        if (conflicts > 0)
        {
            notifications.Add(new SystemNotificationDto
            {
                Id = new Random().Next(1000, 9999),
                Type = NotificationType.MaintenanceConflict,
                Title = "Maintenance Conflicts",
                Message = $"{conflicts} reservations conflict with rooms under maintenance",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });
        }
    }

    private async Task CheckForUnconfirmedCheckIns(List<SystemNotificationDto> notifications, int? hotelId)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var unconfirmedCount = await _context.Reservations
            .Where(r => hotelId == null || r.HotelId == hotelId.Value)
            .Where(r => r.CheckInDate >= today && r.CheckInDate <= tomorrow)
            .Where(r => r.Status == ReservationStatus.Pending)
            .CountAsync();

        if (unconfirmedCount > 0)
        {
            notifications.Add(new SystemNotificationDto
            {
                Id = new Random().Next(1000, 9999),
                Type = NotificationType.Warning,
                Title = "Unconfirmed Check-ins",
                Message = $"{unconfirmedCount} reservations for today/tomorrow are still pending confirmation",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });
        }
    }
}