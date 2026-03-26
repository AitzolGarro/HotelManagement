using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces;

/// <summary>
/// Contrato base del servicio de autenticación de dos factores (2FA)
/// </summary>
public interface ITwoFactorService
{
    Task<(bool Success, IEnumerable<string>? RecoveryCodes)> EnableAsync(User user, string verificationCode);
    Task<bool> DisableAsync(User user, string password);
    Task<bool> VerifyCodeAsync(User user, string code);
    Task<IEnumerable<string>> GenerateRecoveryCodesAsync(User user);
    Task<bool> VerifyRecoveryCodeAsync(User user, string code);
    Task<int> GetRemainingRecoveryCodeCountAsync(User user);
    Task<TwoFactorSetupResponse> GetSetupInfoAsync(User user);

    Task<TwoFactorSetupDto> GenerateSetupAsync(string userId);
    Task<bool> EnableTwoFactorAsync(string userId, string verificationCode);
    Task<bool> DisableTwoFactorAsync(string userId);
    Task<bool> VerifyCodeAsync(string userId, string code);
}
