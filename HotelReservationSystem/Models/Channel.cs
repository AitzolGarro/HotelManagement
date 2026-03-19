namespace HotelReservationSystem.Models;

public class Channel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "Booking.com", "Expedia"
    public bool IsActive { get; set; } = true;
    public string? ApiBaseUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class HotelChannel
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public int ChannelId { get; set; }
    public string ChannelHotelId { get; set; } = string.Empty; // ID of the hotel in the channel's system
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Hotel? Hotel { get; set; }
    public Channel? Channel { get; set; }
}

public class ChannelSyncLog
{
    public int Id { get; set; }
    public int HotelChannelId { get; set; }
    public string SyncType { get; set; } = string.Empty; // "Inventory", "Rates", "Reservations"
    public string Status { get; set; } = string.Empty; // "Success", "Failed", "Warning"
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }

    public HotelChannel? HotelChannel { get; set; }
}