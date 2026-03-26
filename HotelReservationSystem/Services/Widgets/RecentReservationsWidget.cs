using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using Microsoft.Extensions.Localization;
using HotelReservationSystem.Resources;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>Widget 5 — Recent reservations table.</summary>
public class RecentReservationsWidget : DashboardWidgetBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IStringLocalizer _localizer;

    public RecentReservationsWidget(IDashboardService dashboardService, IStringLocalizer<HardcodedStringLocalizer> localizer)
    {
        _dashboardService = dashboardService;
        _localizer = localizer;
    }

    public override string     WidgetId    => "recent-reservations";
    public override WidgetType Type        => WidgetType.RecentReservations;
    public override string     Name        => _localizer["Widget_RecentReservations_Title"];
    public override string     Description => _localizer["Widget_RecentReservations_Description"];
    public override string     Icon        => "bi-clock-history";
    public override int        DefaultW    => 12;
    public override int        DefaultH    => 4;
    public override int        MinW        => 6;
    public override int        MinH        => 3;

    protected override async Task<object?> FetchDataAsync(
        int? hotelId, DateTime? startDate, DateTime? endDate,
        Dictionary<string, string>? settings)
    {
        var limit = 10;
        if (settings != null && settings.TryGetValue("limit", out var limitStr))
            int.TryParse(limitStr, out limit);

        return await _dashboardService.GetRecentReservationsAsync(hotelId, limit);
    }
}
