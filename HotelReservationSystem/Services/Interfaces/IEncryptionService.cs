namespace HotelReservationSystem.Services.Interfaces;

/// <summary>
/// Contrato del servicio de cifrado para datos sensibles del sistema
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Cifra un texto plano usando AES-256-CBC con IV aleatorio
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Descifra un texto cifrado previamente con Encrypt
    /// </summary>
    string Decrypt(string cipherText);

    /// <summary>
    /// Genera un hash SHA-256 unidireccional para búsquedas sin descifrar
    /// </summary>
    string HashSensitiveData(string data);

    /// <summary>
    /// Detecta si un valor ya está cifrado (tiene el prefijo ENC:)
    /// </summary>
    bool IsEncrypted(string value);
}
