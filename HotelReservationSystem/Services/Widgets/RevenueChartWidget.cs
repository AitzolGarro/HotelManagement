using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using Microsoft.Extensions.Localization;
using HotelReservationSystem.Resources;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>Widget 8 — Revenue trend chart (last 30 days).</summary>
public class RevenueChartWidget : DashboardWidgetBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IStringLocalizer _localizer;

    public RevenueChartWidget(IDashboardService dashboardService, IStringLocalizer<HardcodedStringLocalizer> localizer)
        => (_dashboardService, _localizer) = (dashboardService, localizer);

    public override string     WidgetId    => "revenue-chart";
    public override WidgetType Type        => WidgetType.RevenueChart;
    public override string     Name        => _localizer["Widget_RevenueChart_Title"];
    public override string     Description => _localizer["Widget_RevenueChart_Description"];
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
