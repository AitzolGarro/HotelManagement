using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>Widget 7 — Quick-action shortcut buttons.</summary>
public class QuickActionsWidget : DashboardWidgetBase
{
    public override string     WidgetId    => "quick-actions";
    public override WidgetType Type        => WidgetType.QuickActions;
    public override string     Name        => "Quick Actions";
    public override string     Description => "Shortcuts to common tasks";
    public override string     Icon        => "bi-lightning-charge";
    public override int        DefaultW    => 3;
    public override int        DefaultH    => 2;

    protected override Task<object?> FetchDataAsync(
        int? hotelId, DateTime? startDate, DateTime? endDate,
        Dictionary<string, string>? settings)
    {
        var actions = new List<QuickActionDto>
        {
            new() { Label = "New Reservation", Icon = "bi-plus-circle",      Url = "/reservations/new",  Color = "primary"  },
            new() { Label = "Check-in Guest",  Icon = "bi-box-arrow-in-right",Url = "/reservations",      Color = "success"  },
            new() { Label = "View Calendar",   Icon = "bi-calendar3",         Url = "/calendar",          Color = "info"     },
            new() { Label = "Reports",         Icon = "bi-graph-up",          Url = "/reports",           Color = "warning"  },
            new() { Label = "Manage Rooms",    Icon = "bi-door-open",         Url = "/rooms",             Color = "secondary"},
            new() { Label = "Guest List",      Icon = "bi-people",            Url = "/guests",            Color = "dark"     }
        };

        return Task.FromResult<object?>(actions);
    }
}
