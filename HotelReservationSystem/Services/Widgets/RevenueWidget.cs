using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using Microsoft.Extensions.Localization;
using HotelReservationSystem.Resources;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>Widget 2 — Revenue KPI card with daily/weekly chart data.</summary>
public class RevenueWidget : DashboardWidgetBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IStringLocalizer _localizer;

    public RevenueWidget(IDashboardService dashboardService, IStringLocalizer<HardcodedStringLocalizer> localizer)
        => (_dashboardService, _localizer) = (dashboardService, localizer);

    public override string     WidgetId    => "revenue";
    public override WidgetType Type        => WidgetType.Revenue;
    public override string     Name        => _localizer["Widget_Revenue_Title"];
    public override string     Description => _localizer["Widget_Revenue_Description"];
    public override string     Icon        => "bi-currency-dollar";
    public override int        DefaultW    => 3;
    public override int        DefaultH    => 2;

    protected override async Task<object?> FetchDataAsync(
        int? hotelId, DateTime? startDate, DateTime? endDate,
        Dictionary<string, string>? settings)
        => await _dashboardService.GetRevenueTrackingAsync(hotelId, startDate, endDate);
}
