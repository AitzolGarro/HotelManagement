using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using Microsoft.Extensions.Localization;
using HotelReservationSystem.Resources;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>
/// Widget 1 — Quick action buttons that jump to common areas.
/// </summary>
public class QuickActionsWidget : DashboardWidgetBase
{
    private readonly IStringLocalizer _localizer;

    public QuickActionsWidget(IStringLocalizer<HardcodedStringLocalizer> localizer)
        => _localizer = localizer;

    public override string     WidgetId    => "quick-actions";
    public override WidgetType Type        => WidgetType.QuickActions;
    public override string     Name        => _localizer["Widget_QuickActions_Title"];
    public override string     Description => _localizer["Widget_QuickActions_Description"];
    public override string     Icon        => "bi-speedometer2";
    public override int        DefaultW    => 3;
    public override int        DefaultH    => 2;

    protected override async Task<object?> FetchDataAsync(
        int? hotelId, DateTime? startDate, DateTime? endDate,
        Dictionary<string, string>? settings)
        => await Task.FromResult<object?>(new
        {
            Actions = new[]
            {
                new { Label = _localizer["Widget_QuickActions_NewReservation"], Icon = "bi-plus-lg", Url = "/reservations/create", Color = "primary" },
                new { Label = _localizer["Widget_QuickActions_CheckIn"], Icon = "bi-box-arrow-in", Url = "/reservations/checkin", Color = "success" },
                new { Label = _localizer["Widget_QuickActions_ViewCalendar"], Icon = "bi-calendar3", Url = "/home/calendar", Color = "info" },
                new { Label = _localizer["Widget_QuickActions_Reports"], Icon = "bi-graph-up", Url = "/home/reports", Color = "warning" },
                new { Label = _localizer["Widget_QuickActions_ManageRooms"], Icon = "bi-house-door", Url = "/properties/rooms", Color = "secondary" },
                new { Label = _localizer["Widget_QuickActions_GuestList"], Icon = "bi-people", Url = "/reservations/guests", Color = "dark" }
            }
        });
}
