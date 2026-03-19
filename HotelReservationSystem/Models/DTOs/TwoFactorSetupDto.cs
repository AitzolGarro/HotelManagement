using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.Models.DTOs;

/// <summary>
/// DTO de respuesta para la configuración inicial de 2FA
/// </summary>
public class TwoFactorSetupDto
{
    /// <summary>Clave secreta en formato Base32 para el autenticador</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>URI otpauth:// compatible con Google Authenticator y Authy</summary>
    public string QrCodeUri { get; set; } = string.Empty;

    /// <summary>Nombre de la cuenta mostrado en la app autenticadora</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Nombre del emisor mostrado en la app autenticadora</summary>
    public string Issuer { get; set; } = string.Empty;
}

/// <summary>
/// Solicitud para habilitar 2FA con código de verificación
/// </summary>
public class Enable2FARequest
{
    /// <summary>Código TOTP de 6 dígitos generado por la app autenticadora</summary>
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string VerificationCode { get; set; } = string.Empty;
}

/// <summary>
/// Solicitud para deshabilitar 2FA
/// </summary>
public class Disable2FARequest
{
    /// <summary>Contraseña actual del usuario para confirmar la desactivación</summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}
