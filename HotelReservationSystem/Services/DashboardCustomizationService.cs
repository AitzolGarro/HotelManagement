using System.Text.Json;
using HotelReservationSystem.Data;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Services;

public class DashboardCustomizationService : IDashboardCustomizationService
{
    private readonly HotelReservationContext _context;
    private readonly IWidgetRegistry         _registry;
    private readonly ILogger<DashboardCustomizationService> _logger;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public DashboardCustomizationService(
        HotelReservationContext context,
        IWidgetRegistry registry,
        ILogger<DashboardCustomizationService> logger)
    {
        _context  = context;
        _registry = registry;
        _logger   = logger;
    }

    // ── Layout persistence ────────────────────────────────────────────────

    public async Task<DashboardLayoutDto> GetLayoutAsync(int userId)
    {
        var pref = await _context.UserDashboardPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (pref == null)
            return BuildDefaultLayout();

        try
        {
            var widgets = JsonSerializer.Deserialize<List<WidgetConfiguration>>(
                pref.WidgetConfigurationsJson, _jsonOpts) ?? new();

            return new DashboardLayoutDto { Widgets = widgets, UpdatedAt = pref.UpdatedAt };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize dashboard layout for user {UserId}; returning default", userId);
            return BuildDefaultLayout();
        }
    }

    public async Task<DashboardLayoutDto> SaveLayoutAsync(int userId, SaveLayoutRequest request)
    {
        var pref = await _context.UserDashboardPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        var json = JsonSerializer.Serialize(request.Widgets, _jsonOpts);

        if (pref == null)
        {
            pref = new UserDashboardPreference
            {
                UserId                   = userId,
                WidgetConfigurationsJson = json,
                CreatedAt                = DateTime.UtcNow,
                UpdatedAt                = DateTime.UtcNow
            };
            _context.UserDashboardPreferences.Add(pref);
        }
        else
        {
            pref.WidgetConfigurationsJson = json;
            pref.UpdatedAt               = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Saved dashboard layout for user {UserId} ({Count} widgets)", userId, request.Widgets.Count);

        return new DashboardLayoutDto { Widgets = request.Widgets, UpdatedAt = pref.UpdatedAt };
    }

    public async Task<DashboardLayoutDto> ResetLayoutAsync(int userId)
    {
        var pref = await _context.UserDashboardPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (pref != null)
        {
            _context.UserDashboardPreferences.Remove(pref);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Reset dashboard layout for user {UserId}", userId);
        return BuildDefaultLayout();
    }

    // ── Widget data ───────────────────────────────────────────────────────

    public async Task<WidgetDataResponse> GetWidgetDataAsync(
        string widgetId, int? hotelId,
        DateTime? startDate, DateTime? endDate,
        Dictionary<string, string>? settings = null)
    {
        var widget = _registry.Get(widgetId);
        if (widget == null)
        {
            return new WidgetDataResponse
            {
                WidgetId = widgetId,
                Success  = false,
                Error    = $"Widget '{widgetId}' not found"
            };
        }

        try
        {
            var data = await widget.GetDataAsync(hotelId, startDate, endDate, settings);
            return new WidgetDataResponse
            {
                WidgetId = widgetId,
                Type     = widget.Type,
                Success  = true,
                Data     = data,
                LoadedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading widget data for {WidgetId}", widgetId);
            return new WidgetDataResponse
            {
                WidgetId = widgetId,
                Type     = widget.Type,
                Success  = false,
                Error    = "Failed to load widget data"
            };
        }
    }

    public async Task<List<WidgetDataResponse>> GetAllWidgetDataAsync(
        int userId, int? hotelId,
        DateTime? startDate, DateTime? endDate)
    {
        var layout = await GetLayoutAsync(userId);
        var visibleWidgets = layout.Widgets.Where(w => w.IsVisible).ToList();

        var tasks = visibleWidgets.Select(w =>
            GetWidgetDataAsync(w.WidgetId, hotelId, startDate, endDate, w.Settings));

        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    // ── Default layout ────────────────────────────────────────────────────

    private static DashboardLayoutDto BuildDefaultLayout() => new()
    {
        UpdatedAt = DateTime.UtcNow,
        Widgets   = new List<WidgetConfiguration>
        {
            new() { WidgetId = "occupancy-rate",       Type = WidgetType.OccupancyRate,      X = 0,  Y = 0,  W = 3, H = 2, IsVisible = true },
            new() { WidgetId = "revenue",              Type = WidgetType.Revenue,             X = 3,  Y = 0,  W = 3, H = 2, IsVisible = true },
            new() { WidgetId = "upcoming-checkins",    Type = WidgetType.UpcomingCheckIns,    X = 6,  Y = 0,  W = 3, H = 2, IsVisible = true },
            new() { WidgetId = "upcoming-checkouts",   Type = WidgetType.UpcomingCheckOuts,   X = 9,  Y = 0,  W = 3, H = 2, IsVisible = true },
            new() { WidgetId = "occupancy-breakdown",  Type = WidgetType.OccupancyBreakdown,  X = 0,  Y = 2,  W = 4, H = 3, IsVisible = true },
            new() { WidgetId = "revenue-chart",        Type = WidgetType.RevenueChart,        X = 4,  Y = 2,  W = 8, H = 4, IsVisible = true },
            new() { WidgetId = "notifications",        Type = WidgetType.Notifications,       X = 0,  Y = 5,  W = 4, H = 4, IsVisible = true },
            new() { WidgetId = "quick-actions",        Type = WidgetType.QuickActions,        X = 4,  Y = 6,  W = 4, H = 2, IsVisible = true },
            new() { WidgetId = "recent-reservations",  Type = WidgetType.RecentReservations,  X = 0,  Y = 9,  W = 12,H = 4, IsVisible = true },
        }
    };
}
