using System.Security.Cryptography;
using System.Text;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services;

public class EncryptionService : IEncryptionService
{
    private readonly string _key;

    public EncryptionService(IConfiguration configuration)
    {
        _key = configuration["Encryption:Key"] ?? "default_mock_key_must_be_32_bytes_long!"; // 32 chars for AES-256
        if (_key.Length != 32)
        {
            _key = _key.PadRight(32, '0').Substring(0, 32);
        }
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var aesAlg = Aes.Create();
        aesAlg.Key = Encoding.UTF8.GetBytes(_key);
        aesAlg.GenerateIV(); // Generate a random IV for each encryption

        var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

        using var msEncrypt = new MemoryStream();
        // Write the IV to the beginning of the stream so it can be read during decryption
        msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);

        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aesAlg = Aes.Create();
        aesAlg.Key = Encoding.UTF8.GetBytes(_key);

        // Read the IV from the beginning of the stream
        var iv = new byte[aesAlg.BlockSize / 8];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aesAlg.IV = iv;

        var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        using var msDecrypt = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        
        return srDecrypt.ReadToEnd();
    }
}