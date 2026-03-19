using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>Widget 5 — Recent reservations table.</summary>
public class RecentReservationsWidget : DashboardWidgetBase
{
    private readonly IDashboardService _dashboardService;

    public RecentReservationsWidget(IDashboardService dashboardService)
        => _dashboardService = dashboardService;

    public override string     WidgetId    => "recent-reservations";
    public override WidgetType Type        => WidgetType.RecentReservations;
    public override string     Name        => "Recent Reservations";
    public override string     Description => "Latest reservations across all properties";
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
