using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HotelReservationSystem.Models.DTOs;

// ─── Widget type registry ────────────────────────────────────────────────────

public enum WidgetType
{
    OccupancyRate       = 1,
    Revenue             = 2,
    UpcomingCheckIns    = 3,
    UpcomingCheckOuts   = 4,
    RecentReservations  = 5,
    Notifications       = 6,
    QuickActions        = 7,
    RevenueChart        = 8,
    OccupancyBreakdown  = 9
}

// ─── Persisted widget configuration (stored as JSON in DB) ──────────────────

public class WidgetConfiguration
{
    public string WidgetId   { get; set; } = string.Empty;   // e.g. "occupancy-rate"
    public WidgetType Type   { get; set; }
    public int X             { get; set; }
    public int Y             { get; set; }
    public int W             { get; set; } = 3;
    public int H             { get; set; } = 2;
    public bool IsVisible    { get; set; } = true;

    /// <summary>Widget-specific settings (e.g. chart type, limit).</summary>
    public Dictionary<string, string> Settings { get; set; } = new();
}

// ─── Widget descriptor (returned by registry endpoint) ──────────────────────

public class WidgetDescriptor
{
    public string     WidgetId     { get; set; } = string.Empty;
    public WidgetType Type         { get; set; }
    public string     Name         { get; set; } = string.Empty;
    public string     Description  { get; set; } = string.Empty;
    public string     Icon         { get; set; } = string.Empty;
    public int        DefaultW     { get; set; } = 3;
    public int        DefaultH     { get; set; } = 2;
    public int        MinW         { get; set; } = 2;
    public int        MinH         { get; set; } = 2;
}

// ─── Layout save / load DTOs ─────────────────────────────────────────────────

public class SaveLayoutRequest
{
    [Required]
    public List<WidgetConfiguration> Widgets { get; set; } = new();
}

public class DashboardLayoutDto
{
    public List<WidgetConfiguration> Widgets { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}

// ─── Widget data response wrapper ────────────────────────────────────────────

public class WidgetDataResponse
{
    public string    WidgetId  { get; set; } = string.Empty;
    public WidgetType Type     { get; set; }
    public bool      Success   { get; set; } = true;
    public string?   Error     { get; set; }
    public object?   Data      { get; set; }
    public DateTime  LoadedAt  { get; set; } = DateTime.UtcNow;
}

// ─── Quick-actions widget ────────────────────────────────────────────────────

public class QuickActionDto
{
    public string Label  { get; set; } = string.Empty;
    public string Icon   { get; set; } = string.Empty;
    public string Url    { get; set; } = string.Empty;
    public string Color  { get; set; } = "primary";
}
