using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.Models;

/// <summary>
/// Stores per-user dashboard layout and widget configuration as JSON.
/// </summary>
public class UserDashboardPreference
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// JSON-serialized list of WidgetConfiguration objects.
    /// </summary>
    public string WidgetConfigurationsJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}
