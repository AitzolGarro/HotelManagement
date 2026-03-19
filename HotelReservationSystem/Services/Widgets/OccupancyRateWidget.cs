using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>Widget 1 — Occupancy Rate KPI card.</summary>
public class OccupancyRateWidget : DashboardWidgetBase
{
    private readonly IDashboardService _dashboardService;

    public OccupancyRateWidget(IDashboardService dashboardService)
        => _dashboardService = dashboardService;

    public override string     WidgetId    => "occupancy-rate";
    public override WidgetType Type        => WidgetType.OccupancyRate;
    public override string     Name        => "Occupancy Rate";
    public override string     Description => "Today, week and month occupancy percentages";
    public override string     Icon        => "bi-graph-up";
    public override int        DefaultW    => 3;
    public override int        DefaultH    => 2;

    protected override async Task<object?> FetchDataAsync(
        int? hotelId, DateTime? startDate, DateTime? endDate,
        Dictionary<string, string>? settings)
        => await _dashboardService.GetOccupancyRatesAsync(hotelId);
}
