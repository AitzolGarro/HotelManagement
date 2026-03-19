namespace HotelReservationSystem.Models;

/// <summary>
/// Entidad principal de huésped con información de contacto, documentación y estado VIP
/// </summary>
public class Guest
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? DocumentNumber { get; set; }

    /// <summary>
    /// Hash SHA-256 del número de documento para búsquedas sin necesidad de descifrar
    /// </summary>
    public string? DocumentNumberHash { get; set; }

    // Campos adicionales de perfil del huésped
    public string? Nationality { get; set; }
    public string? DocumentType { get; set; } // "Passport", "ID", "Driver License"
    public string? Company { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PreferredLanguage { get; set; }

    // Estado VIP y preferencias de marketing
    public bool IsVip { get; set; }
    public string? VipStatus { get; set; } // "Regular", "VIP", "VVIP"
    public bool MarketingOptIn { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Propiedades de navegación
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<GuestPreference> Preferences { get; set; } = new List<GuestPreference>();
    public ICollection<GuestNote> Notes { get; set; } = new List<GuestNote>();
}
