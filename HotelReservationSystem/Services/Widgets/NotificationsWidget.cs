using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using Microsoft.Extensions.Localization;
using HotelReservationSystem.Resources;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>Widget 6 — System notifications panel.</summary>
public class NotificationsWidget : DashboardWidgetBase
{
    private readonly INotificationService _notificationService;
    private readonly IStringLocalizer _localizer;

    public NotificationsWidget(INotificationService notificationService, IStringLocalizer<HardcodedStringLocalizer> localizer)
        => (_notificationService, _localizer) = (notificationService, localizer);

    public override string     WidgetId    => "notifications";
    public override WidgetType Type        => WidgetType.Notifications;
    public override string     Name        => _localizer["Widget_Notifications_Title"];
    public override string     Description => _localizer["Widget_Notifications_Description"];
    public override string     Icon        => "bi-bell";
    public override int        DefaultW    => 4;
    public override int        DefaultH    => 4;
    public override int        MinH        => 3;

    protected override async Task<object?> FetchDataAsync(
        int? hotelId, DateTime? startDate, DateTime? endDate,
        Dictionary<string, string>? settings)
    {
        var notifications = await _notificationService.GetNotificationsAsync(hotelId, unreadOnly: false);
        var stats         = await _notificationService.GetNotificationStatsAsync(hotelId);

        return new
        {
            Notifications  = notifications.Take(10).ToList(),
            TotalCount     = stats.UnreadCount,
            CriticalCount  = stats.CountByPriority.GetValueOrDefault(NotificationPriority.Critical, 0),
            WarningCount   = stats.CountByPriority.GetValueOrDefault(NotificationPriority.High, 0),
            InfoCount      = stats.CountByPriority.GetValueOrDefault(NotificationPriority.Normal, 0)
                           + stats.CountByPriority.GetValueOrDefault(NotificationPriority.Low, 0)
        };
    }
}
