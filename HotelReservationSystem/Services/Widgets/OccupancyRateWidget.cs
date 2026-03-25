using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using Microsoft.Extensions.Localization;
using HotelReservationSystem.Resources;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>Widget 1 — Occupancy Rate KPI card.</summary>
public class OccupancyRateWidget : DashboardWidgetBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IStringLocalizer _localizer;

    public OccupancyRateWidget(IDashboardService dashboardService, IStringLocalizer<HardcodedStringLocalizer> localizer)
        => (_dashboardService, _localizer) = (dashboardService, localizer);

    public override string     WidgetId    => "occupancy-rate";
    public override WidgetType Type        => WidgetType.OccupancyRate;
    public override string     Name        => _localizer["Widget_OccupancyRate_Title"];
    public override string     Description => _localizer["Widget_OccupancyRate_Description"];
    public override string     Icon        => "bi-graph-up";
    public override int        DefaultW    => 3;
    public override int        DefaultH    => 2;

    protected override async Task<object?> FetchDataAsync(
        int? hotelId, DateTime? startDate, DateTime? endDate,
        Dictionary<string, string>? settings)
        => await _dashboardService.GetOccupancyRatesAsync(hotelId);
}
