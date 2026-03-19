using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services.Widgets;

/// <summary>
/// Abstract base class for all dashboard widgets.
/// Provides default metadata and a safe data-fetch wrapper.
/// </summary>
public abstract class DashboardWidgetBase : IDashboardWidget
{
    public abstract string     WidgetId    { get; }
    public abstract WidgetType Type        { get; }
    public abstract string     Name        { get; }
    public abstract string     Description { get; }
    public abstract string     Icon        { get; }

    public virtual int DefaultW => 3;
    public virtual int DefaultH => 2;
    public virtual int MinW     => 2;
    public virtual int MinH     => 2;

    /// <summary>
    /// Subclasses implement this to return their payload.
    /// </summary>
    protected abstract Task<object?> FetchDataAsync(
        int? hotelId,
        DateTime? startDate,
        DateTime? endDate,
        Dictionary<string, string>? settings);

    /// <inheritdoc />
    public async Task<object?> GetDataAsync(
        int? hotelId,
        DateTime? startDate,
        DateTime? endDate,
        Dictionary<string, string>? settings = null)
    {
        return await FetchDataAsync(hotelId, startDate, endDate, settings);
    }
}
