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
    public async Task<TwoFactorSetupResponse> GetSetupInfoAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var secretKey = await ObtenerOGenerarClaveAsync(user);
        var accountName = user.Email ?? user.UserName ?? user.Id.ToString();

        _logger.LogInformation("Configuración 2FA generada para usuario {UserId}", user.Id);

        return new TwoFactorSetupResponse
        {
            ManualEntryKey = FormatearClaveBase32(secretKey),
            AuthenticatorUri = GenerarQrCodeUri(accountName, secretKey),
            AccountName = accountName,
            Issuer = IssuerName,
            IsTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
            RecoveryCodesRemaining = await _userManager.CountRecoveryCodesAsync(user)
        };
    }

    public async Task<TwoFactorSetupDto> GenerateSetupAsync(string userId)
    {
        var user = await ObtenerUsuarioAsync(userId);
        var setupInfo = await GetSetupInfoAsync(user);

        return new TwoFactorSetupDto
        {
            ManualEntryKey = setupInfo.ManualEntryKey,
            AuthenticatorUri = setupInfo.AuthenticatorUri,
            AccountName = setupInfo.AccountName,
            Issuer = setupInfo.Issuer
        };
    }

    /// <summary>
    /// Habilita 2FA verificando que el código TOTP sea válido antes de activar
    /// </summary>
    public async Task<(bool Success, IEnumerable<string>? RecoveryCodes)> EnableAsync(User user, string verificationCode)
    {
        ArgumentNullException.ThrowIfNull(user);

        var codigoValido = await VerificarCodigoTotpAsync(user, verificationCode);
        if (!codigoValido)
        {
            _logger.LogWarning("Código 2FA inválido al intentar habilitar para usuario {UserId}", user.Id);
            return (false, null);
        }

        var resultado = await _userManager.SetTwoFactorEnabledAsync(user, true);
        if (!resultado.Succeeded)
        {
            _logger.LogError("Error al habilitar 2FA para usuario {UserId}: {Errors}",
                user.Id, string.Join(", ", resultado.Errors.Select(e => e.Description)));
            return (false, null);
        }

        var recoveryCodes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 8)).ToArray();

        _logger.LogInformation("2FA habilitado exitosamente para usuario {UserId}", user.Id);
        return (true, recoveryCodes);
    }

    public async Task<bool> EnableTwoFactorAsync(string userId, string verificationCode)
    {
        var user = await ObtenerUsuarioAsync(userId);
        var result = await EnableAsync(user, verificationCode);
        return result.Success;
    }

    /// <summary>
    /// Deshabilita 2FA y elimina el secreto almacenado del usuario
    /// </summary>
    public async Task<bool> DisableAsync(User user, string password)
    {
        ArgumentNullException.ThrowIfNull(user);

        var passwordValida = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValida)
        {
            _logger.LogWarning("Contraseña inválida al intentar deshabilitar 2FA para usuario {UserId}", user.Id);
            return false;
        }

        var resultado = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!resultado.Succeeded)
        {
            _logger.LogError("Error al deshabilitar 2FA para usuario {UserId}: {Errors}",
                user.Id, string.Join(", ", resultado.Errors.Select(e => e.Description)));
            return false;
        }

        _logger.LogInformation("2FA deshabilitado exitosamente para usuario {UserId}", user.Id);
        return true;
    }

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
    public async Task<bool> VerifyCodeAsync(User user, string code)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (await VerificarCodigoTotpAsync(user, code))
        {
            return true;
        }

        return await VerifyRecoveryCodeAsync(user, code);
    }

    public async Task<bool> VerifyCodeAsync(string userId, string code)
    {
        var user = await ObtenerUsuarioAsync(userId);
        return await VerifyCodeAsync(user, code);
    }

    public async Task<IEnumerable<string>> GenerateRecoveryCodesAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 8);
    }

    public async Task<bool> VerifyRecoveryCodeAsync(User user, string code)
    {
        ArgumentNullException.ThrowIfNull(user);
        var result = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, code);
        return result.Succeeded;
    }

    public async Task<int> GetRemainingRecoveryCodeCountAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return await _userManager.CountRecoveryCodesAsync(user);
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
        var normalizedCode = NormalizarCodigo(code);

        // El AuthenticatorTokenProvider de Identity implementa TOTP (RFC 6238)
        // compatible con Google Authenticator, Authy y cualquier app estándar
        var esValido = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            normalizedCode);

        return esValido;
    }

    private static string NormalizarCodigo(string code)
    {
        return (code ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
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
