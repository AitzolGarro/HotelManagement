using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using Microsoft.Extensions.Localization;
using HotelReservationSystem.Resources;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>Widget 3 — Upcoming check-ins for today.</summary>
public class UpcomingCheckInsWidget : DashboardWidgetBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IStringLocalizer _localizer;

    public UpcomingCheckInsWidget(IDashboardService dashboardService, IStringLocalizer<HardcodedStringLocalizer> localizer)
        => (_dashboardService, _localizer) = (dashboardService, localizer);

    public override string     WidgetId    => "upcoming-checkins";
    public override WidgetType Type        => WidgetType.UpcomingCheckIns;
    public override string     Name        => _localizer["Widget_UpcomingCheckIns_Title"];
    public override string     Description => _localizer["Widget_UpcomingCheckIns_Description"];
    public override string     Icon        => "bi-box-arrow-in-right";
    public override int        DefaultW    => 4;
    public override int        DefaultH    => 3;
    public override int        MinH        => 2;

    protected override async Task<object?> FetchDataAsync(
        int? hotelId, DateTime? startDate, DateTime? endDate,
        Dictionary<string, string>? settings)
    {
        var date = startDate ?? DateTime.Today;
        var ops  = await _dashboardService.GetDailyOperationsAsync(date, hotelId);
        return new { ops.TodayCheckIns, ops.TotalCheckIns };
    }
}
