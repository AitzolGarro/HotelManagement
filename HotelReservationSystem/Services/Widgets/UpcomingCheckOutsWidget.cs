using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>Widget 4 — Upcoming check-outs for today.</summary>
public class UpcomingCheckOutsWidget : DashboardWidgetBase
{
    private readonly IDashboardService _dashboardService;

    public UpcomingCheckOutsWidget(IDashboardService dashboardService)
        => _dashboardService = dashboardService;

    public override string     WidgetId    => "upcoming-checkouts";
    public override WidgetType Type        => WidgetType.UpcomingCheckOuts;
    public override string     Name        => "Upcoming Check-outs";
    public override string     Description => "Guests checking out today";
    public override string     Icon        => "bi-box-arrow-right";
    public override int        DefaultW    => 4;
    public override int        DefaultH    => 3;
    public override int        MinH        => 2;

    protected override async Task<object?> FetchDataAsync(
        int? hotelId, DateTime? startDate, DateTime? endDate,
        Dictionary<string, string>? settings)
    {
        var date = startDate ?? DateTime.Today;
        var ops  = await _dashboardService.GetDailyOperationsAsync(date, hotelId);
        return new { ops.TodayCheckOuts, ops.TotalCheckOuts };
    }
}
