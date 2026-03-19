using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;

namespace HotelReservationSystem.Services;

/// <summary>
/// Servicio de autenticación de dos factores (2FA) basado en TOTP (RFC 6238).
/// Utiliza HMAC-SHA1 nativo de .NET para generar y verificar códigos compatibles
/// con Google Authenticator, Authy y cualquier app autenticadora estándar.
/// </summary>
public class TwoFactorService : ITwoFactorService
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<TwoFactorService> _logger;

    // Nombre del emisor mostrado en la app autenticadora
    private const string IssuerName = "Hotel Reservation System";

    // Período de validez del código TOTP en segundos (estándar RFC 6238)
    private const int TotpPeriodSeconds = 30;

    // Número de dígitos del código TOTP
    private const int TotpDigits = 6;

    // Ventana de tolerancia: número de períodos anteriores/siguientes aceptados
    private const int TotpWindowSize = 1;

    public TwoFactorService(
        UserManager<User> userManager,
        ILogger<TwoFactorService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Genera la configuración inicial de 2FA: secreto Base32 y URI otpauth://
    /// </summary>
    public async Task<TwoFactorSetupDto> GenerateSetupAsync(string userId)
    {
        var user = await ObtenerUsuarioAsync(userId);

        // Obtener o generar la clave del autenticador de Identity
        var secretKey = await ObtenerOGenerarClaveAsync(user);

        // Construir la URI otpauth:// compatible con Google Authenticator
        var qrCodeUri = GenerarQrCodeUri(user.Email ?? user.UserName ?? userId, secretKey);

        _logger.LogInformation("Configuración 2FA generada para usuario {UserId}", userId);

        return new TwoFactorSetupDto
        {
            SecretKey = FormatearClaveBase32(secretKey),
            QrCodeUri = qrCodeUri,
            AccountName = user.Email ?? user.UserName ?? userId,
            Issuer = IssuerName
        };
    }

    /// <summary>
    /// Habilita 2FA verificando que el código TOTP sea válido antes de activar
    /// </summary>
    public async Task<bool> EnableTwoFactorAsync(string userId, string verificationCode)
    {
        var user = await ObtenerUsuarioAsync(userId);

        // Verificar el código antes de habilitar
        var codigoValido = await VerificarCodigoTotpAsync(user, verificationCode);
        if (!codigoValido)
        {
            _logger.LogWarning("Código 2FA inválido al intentar habilitar para usuario {UserId}", userId);
            return false;
        }

        // Habilitar 2FA en Identity
        var resultado = await _userManager.SetTwoFactorEnabledAsync(user, true);
        if (!resultado.Succeeded)
        {
            _logger.LogError("Error al habilitar 2FA para usuario {UserId}: {Errors}",
                userId, string.Join(", ", resultado.Errors.Select(e => e.Description)));
            return false;
        }

        _logger.LogInformation("2FA habilitado exitosamente para usuario {UserId}", userId);
        return true;
    }

    /// <summary>
    /// Deshabilita 2FA y elimina el secreto almacenado del usuario
    /// </summary>
    public async Task<bool> DisableTwoFactorAsync(string userId)
    {
        var user = await ObtenerUsuarioAsync(userId);

        // Deshabilitar 2FA en Identity
        var resultado = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!resultado.Succeeded)
        {
            _logger.LogError("Error al deshabilitar 2FA para usuario {UserId}: {Errors}",
                userId, string.Join(", ", resultado.Errors.Select(e => e.Description)));
            return false;
        }

        // Limpiar la clave del autenticador y el secreto almacenado
        await _userManager.ResetAuthenticatorKeyAsync(user);
        user.TwoFactorSecret = null;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("2FA deshabilitado exitosamente para usuario {UserId}", userId);
        return true;
    }

    /// <summary>
    /// Verifica un código TOTP de 6 dígitos para el usuario especificado
    /// </summary>
    public async Task<bool> VerifyCodeAsync(string userId, string code)
    {
        var user = await ObtenerUsuarioAsync(userId);
        return await VerificarCodigoTotpAsync(user, code);
    }

    // ─────────────────────────────────────────────
    // Métodos privados auxiliares
    // ─────────────────────────────────────────────

    /// <summary>
    /// Obtiene el usuario por ID o lanza excepción si no existe
    /// </summary>
    private async Task<User> ObtenerUsuarioAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new ArgumentException($"Usuario con ID {userId} no encontrado");
        }
        return user;
    }

    /// <summary>
    /// Obtiene la clave del autenticador existente o genera una nueva
    /// </summary>
    private async Task<string> ObtenerOGenerarClaveAsync(User user)
    {
        var clave = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(clave))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            clave = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        // Guardar también en el campo TwoFactorSecret del usuario
        user.TwoFactorSecret = clave;
        await _userManager.UpdateAsync(user);

        return clave!;
    }

    /// <summary>
    /// Verifica el código TOTP usando el proveedor de tokens de Identity (HMAC-SHA1, RFC 6238)
    /// </summary>
    private async Task<bool> VerificarCodigoTotpAsync(User user, string code)
    {
        // El AuthenticatorTokenProvider de Identity implementa TOTP (RFC 6238)
        // compatible con Google Authenticator, Authy y cualquier app estándar
        var esValido = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            code);

        return esValido;
    }

    /// <summary>
    /// Genera la URI otpauth:// compatible con Google Authenticator y Authy
    /// Formato: otpauth://totp/{issuer}:{account}?secret={secret}&issuer={issuer}&digits=6&period=30
    /// </summary>
    private static string GenerarQrCodeUri(string accountName, string secretKey)
    {
        var issuerEncoded = Uri.EscapeDataString(IssuerName);
        var accountEncoded = Uri.EscapeDataString(accountName);
        var secretLimpio = secretKey.Replace(" ", "").ToUpperInvariant();

        return $"otpauth://totp/{issuerEncoded}:{accountEncoded}" +
               $"?secret={secretLimpio}" +
               $"&issuer={issuerEncoded}" +
               $"&algorithm=SHA1" +
               $"&digits={TotpDigits}" +
               $"&period={TotpPeriodSeconds}";
    }

    /// <summary>
    /// Formatea la clave Base32 en grupos de 4 para mejor legibilidad manual
    /// </summary>
    private static string FormatearClaveBase32(string key)
    {
        var claveLimpia = key.Replace(" ", "").ToUpperInvariant();

        // Agrupar en bloques de 4 caracteres separados por espacios
        return string.Join(" ", Enumerable.Range(0, (claveLimpia.Length + 3) / 4)
            .Select(i => claveLimpia.Substring(i * 4, Math.Min(4, claveLimpia.Length - i * 4))));
    }
}
