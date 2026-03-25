namespace HotelReservationSystem.Infrastructure;

/// <summary>
/// Contrato para generación de tokens JWT — full tokens y partial-auth (2FA challenge)
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Emite un token JWT parcial de tipo "2fa-challenge" con un jti único y TTL de 5 minutos.
    /// No incluye claims de rol. Solo válido para el endpoint /auth/2fa/challenge.
    /// </summary>
    string IssuePartialAuthToken(string userId);
}
