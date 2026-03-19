using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces;

/// <summary>
/// Contract for a single dashboard widget that can supply its own data.
/// </summary>
public interface IDashboardWidget
{
    string     WidgetId    { get; }
    WidgetType Type        { get; }
    string     Name        { get; }
    string     Description { get; }
    string     Icon        { get; }
    int        DefaultW    { get; }
    int        DefaultH    { get; }
    int        MinW        { get; }
    int        MinH        { get; }

    Task<object?> GetDataAsync(int? hotelId, DateTime? startDate, DateTime? endDate,
                               Dictionary<string, string>? settings = null);
}

/// <summary>
/// Registry that knows about all available widgets and dispatches data requests.
/// </summary>
public interface IWidgetRegistry
{
    IReadOnlyList<IDashboardWidget> GetAll();
    IDashboardWidget? Get(string widgetId);
    IReadOnlyList<WidgetDescriptor> GetDescriptors();
}

/// <summary>
/// Manages per-user dashboard layout persistence and widget data aggregation.
/// </summary>
public interface IDashboardCustomizationService
{
    Task<DashboardLayoutDto>  GetLayoutAsync(int userId);
    Task<DashboardLayoutDto>  SaveLayoutAsync(int userId, SaveLayoutRequest request);
    Task<DashboardLayoutDto>  ResetLayoutAsync(int userId);
    Task<WidgetDataResponse>  GetWidgetDataAsync(string widgetId, int? hotelId,
                                                  DateTime? startDate, DateTime? endDate,
                                                  Dictionary<string, string>? settings = null);
    Task<List<WidgetDataResponse>> GetAllWidgetDataAsync(int userId, int? hotelId,
                                                          DateTime? startDate, DateTime? endDate);
}
