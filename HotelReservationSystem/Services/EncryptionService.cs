using System.Security.Cryptography;
using System.Text;
using HotelReservationSystem.Configuration;
using HotelReservationSystem.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace HotelReservationSystem.Services;

/// <summary>
/// Servicio de cifrado de datos sensibles usando AES-256-CBC con IV aleatorio por operación.
/// Los valores cifrados se identifican con el prefijo "ENC:" para detección rápida.
/// </summary>
public class EncryptionService : IEncryptionService
{
    // Prefijo para identificar valores ya cifrados
    private const string EncryptedPrefix = "ENC:";

    private readonly byte[] _keyBytes;

    /// <summary>
    /// Constructor que carga la clave de cifrado desde la configuración
    /// </summary>
    public EncryptionService(IConfiguration configuration)
    {
        var base64Key = configuration["EncryptionSettings:Key"];
        _keyBytes = ResolveKeyBytes(base64Key);
    }

    /// <summary>
    /// Cifra un texto plano usando AES-256-CBC con IV aleatorio.
    /// El IV se antepone al texto cifrado y el resultado se codifica en Base64 con prefijo ENC:
    /// </summary>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        // No cifrar si ya está cifrado
        if (IsEncrypted(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = _keyBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        var cipherBytes = EncryptBytes(plainText, aes);
        var result = CombineIvAndCipher(aes.IV, cipherBytes);

        return EncryptedPrefix + Convert.ToBase64String(result);
    }

    /// <summary>
    /// Descifra un texto cifrado previamente con Encrypt.
    /// Extrae el IV del inicio del payload y descifra con AES-256-CBC.
    /// </summary>
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        // Si no tiene el prefijo, devolver tal cual (valor no cifrado)
        if (!IsEncrypted(cipherText))
            return cipherText;

        var base64Payload = cipherText[EncryptedPrefix.Length..];
        var fullBytes = Convert.FromBase64String(base64Payload);

        using var aes = Aes.Create();
        aes.Key = _keyBytes;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var (iv, encryptedBytes) = ExtractIvAndCipher(fullBytes, aes.BlockSize / 8);
        aes.IV = iv;

        return DecryptBytes(encryptedBytes, aes);
    }

    /// <summary>
    /// Genera un hash SHA-256 unidireccional del dato para permitir búsquedas sin descifrar.
    /// El resultado se codifica en Base64 para almacenamiento compacto.
    /// </summary>
    public string HashSensitiveData(string data)
    {
        if (string.IsNullOrEmpty(data))
            return string.Empty;

        var dataBytes = Encoding.UTF8.GetBytes(data.Trim().ToUpperInvariant());
        var hashBytes = SHA256.HashData(dataBytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Detecta si un valor ya fue cifrado verificando el prefijo ENC:
    /// </summary>
    public bool IsEncrypted(string value)
    {
        return !string.IsNullOrEmpty(value) && value.StartsWith(EncryptedPrefix);
    }

    // --- Métodos auxiliares privados ---

    /// <summary>
    /// Resuelve los bytes de la clave desde Base64 o genera una clave de 32 bytes desde texto plano
    /// </summary>
    private static byte[] ResolveKeyBytes(string? base64Key)
    {
        if (string.IsNullOrEmpty(base64Key))
            return GetFallbackKey();

        try
        {
            var decoded = Convert.FromBase64String(base64Key);
            if (decoded.Length == 32)
                return decoded;
        }
        catch (FormatException)
        {
            // No es Base64 válido, tratar como texto plano
        }

        // Derivar 32 bytes desde texto plano usando SHA-256
        return SHA256.HashData(Encoding.UTF8.GetBytes(base64Key));
    }

    /// <summary>
    /// Clave de respaldo para desarrollo cuando no hay configuración
    /// </summary>
    private static byte[] GetFallbackKey()
    {
        const string devKey = "HotelReservationDevKey2024!Secure";
        return SHA256.HashData(Encoding.UTF8.GetBytes(devKey));
    }

    /// <summary>
    /// Cifra el texto plano y retorna los bytes cifrados
    /// </summary>
    private static byte[] EncryptBytes(string plainText, Aes aes)
    {
        using var ms = new MemoryStream();
        using var encryptor = aes.CreateEncryptor();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var sw = new StreamWriter(cs, Encoding.UTF8);
        sw.Write(plainText);
        sw.Flush();
        cs.FlushFinalBlock();
        return ms.ToArray();
    }

    /// <summary>
    /// Descifra los bytes cifrados y retorna el texto plano
    /// </summary>
    private static string DecryptBytes(byte[] encryptedBytes, Aes aes)
    {
        using var ms = new MemoryStream(encryptedBytes);
        using var decryptor = aes.CreateDecryptor();
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);
        return sr.ReadToEnd();
    }

    /// <summary>
    /// Combina el IV y los bytes cifrados en un único array
    /// </summary>
    private static byte[] CombineIvAndCipher(byte[] iv, byte[] cipher)
    {
        var combined = new byte[iv.Length + cipher.Length];
        Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
        Buffer.BlockCopy(cipher, 0, combined, iv.Length, cipher.Length);
        return combined;
    }

    /// <summary>
    /// Extrae el IV y los bytes cifrados del array combinado
    /// </summary>
    private static (byte[] iv, byte[] cipher) ExtractIvAndCipher(byte[] combined, int ivLength)
    {
        var iv = new byte[ivLength];
        var cipher = new byte[combined.Length - ivLength];
        Buffer.BlockCopy(combined, 0, iv, 0, ivLength);
        Buffer.BlockCopy(combined, ivLength, cipher, 0, cipher.Length);
        return (iv, cipher);
    }
}
