using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using Microsoft.Extensions.Localization;
using HotelReservationSystem.Resources;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>Widget 4 — Upcoming check-outs for today.</summary>
public class UpcomingCheckOutsWidget : DashboardWidgetBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IStringLocalizer _localizer;

    public UpcomingCheckOutsWidget(IDashboardService dashboardService, IStringLocalizer<HardcodedStringLocalizer> localizer)
        => (_dashboardService, _localizer) = (dashboardService, localizer);

    public override string     WidgetId    => "upcoming-checkouts";
    public override WidgetType Type        => WidgetType.UpcomingCheckOuts;
    public override string     Name        => _localizer["Widget_UpcomingCheckOuts_Title"];
    public override string     Description => _localizer["Widget_UpcomingCheckOuts_Description"];
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
