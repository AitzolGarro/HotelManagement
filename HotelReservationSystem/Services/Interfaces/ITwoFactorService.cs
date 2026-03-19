using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces;

/// <summary>
/// Contrato del servicio de autenticación de dos factores (2FA) basado en TOTP
/// </summary>
public interface ITwoFactorService
{
    /// <summary>
    /// Genera la configuración inicial de 2FA para un usuario (secreto + URI QR)
    /// </summary>
    Task<TwoFactorSetupDto> GenerateSetupAsync(string userId);

    /// <summary>
    /// Habilita 2FA para el usuario después de verificar el código TOTP
    /// </summary>
    Task<bool> EnableTwoFactorAsync(string userId, string verificationCode);

    /// <summary>
    /// Deshabilita 2FA para el usuario
    /// </summary>
    Task<bool> DisableTwoFactorAsync(string userId);

    /// <summary>
    /// Verifica un código TOTP para el usuario especificado
    /// </summary>
    Task<bool> VerifyCodeAsync(string userId, string code);
}
