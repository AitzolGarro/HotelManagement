using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>
/// Singleton registry of all available dashboard widgets.
/// Widgets are injected via DI so each one can have its own dependencies.
/// </summary>
public class WidgetRegistry : IWidgetRegistry
{
    private readonly IReadOnlyDictionary<string, IDashboardWidget> _widgets;

    public WidgetRegistry(IEnumerable<IDashboardWidget> widgets)
    {
        _widgets = widgets.ToDictionary(w => w.WidgetId, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<IDashboardWidget> GetAll() => _widgets.Values.ToList();

    public IDashboardWidget? Get(string widgetId)
        => _widgets.TryGetValue(widgetId, out var w) ? w : null;

    public IReadOnlyList<WidgetDescriptor> GetDescriptors()
        => _widgets.Values.Select(w => new WidgetDescriptor
        {
            WidgetId    = w.WidgetId,
            Type        = w.Type,
            Name        = w.Name,
            Description = w.Description,
            Icon        = w.Icon,
            DefaultW    = w.DefaultW,
            DefaultH    = w.DefaultH,
            MinW        = w.MinW,
            MinH        = w.MinH
        }).ToList();
}
