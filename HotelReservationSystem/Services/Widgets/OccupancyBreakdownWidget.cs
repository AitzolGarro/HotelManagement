using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using Microsoft.Extensions.Localization;
using HotelReservationSystem.Resources;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>Widget 9 — Occupancy breakdown (today / week / month).</summary>
public class OccupancyBreakdownWidget : DashboardWidgetBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IStringLocalizer _localizer;

    public OccupancyBreakdownWidget(IDashboardService dashboardService, IStringLocalizer<HardcodedStringLocalizer> localizer)
        => (_dashboardService, _localizer) = (dashboardService, localizer);

    public override string     WidgetId    => "occupancy-breakdown";
    public override WidgetType Type        => WidgetType.OccupancyBreakdown;
    public override string     Name        => _localizer["Widget_OccupancyBreakdown_Title"];
    public override string     Description => _localizer["Widget_OccupancyBreakdown_Description"];
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
