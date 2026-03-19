using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>Widget 9 — Occupancy breakdown (today / week / month).</summary>
public class OccupancyBreakdownWidget : DashboardWidgetBase
{
    private readonly IDashboardService _dashboardService;

    public OccupancyBreakdownWidget(IDashboardService dashboardService)
        => _dashboardService = dashboardService;

    public override string     WidgetId    => "occupancy-breakdown";
    public override WidgetType Type        => WidgetType.OccupancyBreakdown;
    public override string     Name        => "Occupancy Breakdown";
    public override string     Description => "Detailed occupancy metrics across time periods";
    public override string     Icon        => "bi-pie-chart";
    public override int        DefaultW    => 4;
    public override int        DefaultH    => 3;
    public override int        MinW        => 3;
    public override int        MinH        => 2;

    protected override async Task<object?> FetchDataAsync(
        int? hotelId, DateTime? startDate, DateTime? endDate,
        Dictionary<string, string>? settings)
        => await _dashboardService.GetOccupancyRatesAsync(hotelId);
}
