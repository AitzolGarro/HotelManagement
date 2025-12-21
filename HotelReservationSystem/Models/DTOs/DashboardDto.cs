using HotelReservationSystem.Models;

namespace HotelReservationSystem.Models.DTOs;

public class DashboardKpiDto
{
    public OccupancyRateDto OccupancyRate { get; set; } = new();
    public RevenueTrackingDto RevenueTracking { get; set; } = new();
    public DailyOperationsDto DailyOperations { get; set; } = new();
    public NotificationPanelDto Notifications { get; set; } = new();
}

public class OccupancyRateDto
{
    public decimal TodayRate { get; set; }
    public decimal WeekRate { get; set; }
    public decimal MonthRate { get; set; }
    public int TotalRooms { get; set; }
    public int OccupiedRoomsToday { get; set; }
    public int OccupiedRoomsWeek { get; set; }
    public int OccupiedRoomsMonth { get; set; }
}

public class RevenueTrackingDto
{
    public decimal TodayRevenue { get; set; }
    public decimal WeekRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public decimal ProjectedMonthRevenue { get; set; }
    public decimal LastMonthRevenue { get; set; }
    public decimal WeeklyVariance { get; set; }
    public decimal MonthlyVariance { get; set; }
    public List<DailyRevenueDto> DailyBreakdown { get; set; } = new();
    public List<WeeklyRevenueDto> WeeklyBreakdown { get; set; } = new();
}

// DailyRevenueDto is defined in ReportDto.cs

public class WeeklyRevenueDto
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public decimal Revenue { get; set; }
    public int ReservationCount { get; set; }
}

public class DailyOperationsDto
{
    public List<CheckInOutDto> TodayCheckIns { get; set; } = new();
    public List<CheckInOutDto> TodayCheckOuts { get; set; } = new();
    public int TotalCheckIns { get; set; }
    public int TotalCheckOuts { get; set; }
}

public class CheckInOutDto
{
    public int ReservationId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public ReservationStatus Status { get; set; }
    public string SpecialRequests { get; set; } = string.Empty;
}

public class NotificationPanelDto
{
    public List<SystemNotificationDto> Notifications { get; set; } = new();
    public int TotalCount { get; set; }
    public int CriticalCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
}

// SystemNotificationDto is defined in NotificationDto.cs

public class RecentReservationDto
{
    public int Id { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public ReservationStatus Status { get; set; }
    public ReservationSource Source { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// NotificationType enum is defined in NotificationDto.cs