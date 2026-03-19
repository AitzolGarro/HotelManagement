using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>Widget 2 — Revenue KPI card with daily/weekly chart data.</summary>
public class RevenueWidget : DashboardWidgetBase
{
    private readonly IDashboardService _dashboardService;

    public RevenueWidget(IDashboardService dashboardService)
        => _dashboardService = dashboardService;

    public override string     WidgetId    => "revenue";
    public override WidgetType Type        => WidgetType.Revenue;
    public override string     Name        => "Revenue";
    public override string     Description => "Today, week and month revenue with trend chart";
    public override string     Icon        => "bi-currency-dollar";
    public override int        DefaultW    => 3;
    public override int        DefaultH    => 2;

    protected override async Task<object?> FetchDataAsync(
        int? hotelId, DateTime? startDate, DateTime? endDate,
        Dictionary<string, string>? settings)
        => await _dashboardService.GetRevenueTrackingAsync(hotelId, startDate, endDate);
}
