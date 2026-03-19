using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>Widget 8 — Revenue trend chart (last 30 days).</summary>
public class RevenueChartWidget : DashboardWidgetBase
{
    private readonly IDashboardService _dashboardService;

    public RevenueChartWidget(IDashboardService dashboardService)
        => _dashboardService = dashboardService;

    public override string     WidgetId    => "revenue-chart";
    public override WidgetType Type        => WidgetType.RevenueChart;
    public override string     Name        => "Revenue Chart";
    public override string     Description => "Daily revenue trend for the selected period";
    public override string     Icon        => "bi-bar-chart-line";
    public override int        DefaultW    => 8;
    public override int        DefaultH    => 4;
    public override int        MinW        => 4;
    public override int        MinH        => 3;

    protected override async Task<object?> FetchDataAsync(
        int? hotelId, DateTime? startDate, DateTime? endDate,
        Dictionary<string, string>? settings)
    {
        var end   = endDate   ?? DateTime.Today.AddDays(1);
        var start = startDate ?? DateTime.Today.AddDays(-29);

        var daily  = await _dashboardService.GetDailyRevenueBreakdownAsync(start, end, hotelId);
        var weekly = await _dashboardService.GetWeeklyRevenueBreakdownAsync(start, end, hotelId);

        return new { DailyBreakdown = daily, WeeklyBreakdown = weekly };
    }
}
