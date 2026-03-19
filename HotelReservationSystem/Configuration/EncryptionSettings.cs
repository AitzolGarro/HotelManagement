namespace HotelReservationSystem.Configuration;

/// <summary>
/// Configuración para el servicio de cifrado de datos sensibles
/// </summary>
public class EncryptionSettings
{
    /// <summary>
    /// Clave de cifrado AES-256 codificada en Base64 (32 bytes)
    /// En producción debe provenir de variables de entorno o Azure Key Vault
    /// </summary>
    public string Key { get; set; } = string.Empty;
}
