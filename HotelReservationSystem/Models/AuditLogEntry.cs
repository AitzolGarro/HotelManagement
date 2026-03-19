namespace HotelReservationSystem.Models;

public class AuditLogEntry
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? QueryString { get; set; }
    public string? RequestBody { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; }
}