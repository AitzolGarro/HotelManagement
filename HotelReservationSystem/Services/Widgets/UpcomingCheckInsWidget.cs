using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>Widget 3 — Upcoming check-ins for today.</summary>
public class UpcomingCheckInsWidget : DashboardWidgetBase
{
    private readonly IDashboardService _dashboardService;

    public UpcomingCheckInsWidget(IDashboardService dashboardService)
        => _dashboardService = dashboardService;

    public override string     WidgetId    => "upcoming-checkins";
    public override WidgetType Type        => WidgetType.UpcomingCheckIns;
    public override string     Name        => "Upcoming Check-ins";
    public override string     Description => "Guests checking in today";
    public override string     Icon        => "bi-box-arrow-in-right";
    public override int        DefaultW    => 4;
    public override int        DefaultH    => 3;
    public override int        MinH        => 2;

    protected override async Task<object?> FetchDataAsync(
        int? hotelId, DateTime? startDate, DateTime? endDate,
        Dictionary<string, string>? settings)
    {
        var date = startDate ?? DateTime.Today;
        var ops  = await _dashboardService.GetDailyOperationsAsync(date, hotelId);
        return new { ops.TodayCheckIns, ops.TotalCheckIns };
    }
}
