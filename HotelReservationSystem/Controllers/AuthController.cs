using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Authorization;

namespace HotelReservationSystem.Controllers;
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITwoFactorService _twoFactorService;
    private readonly UserManager<User> _userManager;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<AuthController> _logger;
    private static readonly JsonSerializerOptions CamelCaseJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AuthController(
        IAuthService authService,
        ITwoFactorService twoFactorService,
        UserManager<User> userManager,
        IMemoryCache memoryCache,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _twoFactorService = twoFactorService;
        _userManager = userManager;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return JsonResponse(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Login attempt failed for {Email}: {Message}", request.Email, ex.Message);
            return StatusCodeOnly(StatusCodes.Status401Unauthorized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // With JWT, logout is handled client-side by removing the token
        // We could implement token blacklisting here if needed
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    [HttpPost("users")]
    [RequireRole(UserRole.Admin)]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var user = await _authService.CreateUserAsync(request);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to create user {Email}: {Message}", request.Email, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred while creating the user" });
        }
    }

    [HttpGet("users")]
    [RequireRole(UserRole.Admin, UserRole.Manager)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        try
        {
            var users = await _authService.GetUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    [HttpGet("users/{id}")]
    [RequireRole(UserRole.Admin, UserRole.Manager)]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        try
        {
            var user = await _authService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    [HttpPut("users/{id}")]
    [RequireRole(UserRole.Admin)]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _authService.UpdateUserAsync(id, request);
            return Ok(user);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to update user {UserId}: {Message}", id, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the user" });
        }
    }

    [HttpPost("keep-alive")]
    [Authorize]
    public IActionResult KeepAlive()
    {
        return Ok(new { message = "Session active", timestamp = DateTime.UtcNow });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var success = await _authService.ChangePasswordAsync(userId, request);
            if (!success)
            {
                return BadRequest(new { message = "Failed to change password. Please check your current password." });
            }

            return Ok(new { message = "Password changed successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(500, new { message = "An error occurred while changing the password" });
        }
    }

    [HttpDelete("users/{id}")]
    [RequireRole(UserRole.Admin)]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        try
        {
            var success = await _authService.DeactivateUserAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return Ok(new { message = "User deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while deactivating the user" });
        }
    }

    // ─────────────────────────────────────────────
    // Endpoints dedicados de 2FA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Genera la configuración inicial de 2FA con clave secreta y URI para QR
    /// </summary>
    [HttpPost("2fa/setup")]
    [Authorize]
    public async Task<ActionResult<TwoFactorSetupDto>> Setup2FA()
    {
        try
        {
            var userId = ObtenerUserIdActual();
            if (userId == null) return Unauthorized();

            var setup = await _twoFactorService.GenerateSetupAsync(userId);
            return Ok(setup);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando configuración 2FA");
            return StatusCode(500, new { message = "Error al generar la configuración 2FA" });
        }
    }

    /// <summary>
    /// Habilita 2FA verificando el código TOTP proporcionado
    /// </summary>
    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<IActionResult> Enable2FAWithVerification([FromBody] Enable2FARequest request)
    {
        try
        {
            var userId = ObtenerUserIdActual();
            if (userId == null) return Unauthorized();

            var habilitado = await _twoFactorService.EnableTwoFactorAsync(userId, request.VerificationCode);
            if (!habilitado)
            {
                return BadRequest(new { message = "Código de verificación inválido. Verifique su app autenticadora." });
            }

            return Ok(new { message = "Autenticación de dos factores habilitada exitosamente" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error habilitando 2FA");
            return StatusCode(500, new { message = "Error al habilitar 2FA" });
        }
    }

    /// <summary>
    /// Deshabilita 2FA para el usuario autenticado
    /// </summary>
    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> Disable2FA([FromBody] Disable2FARequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = ObtenerUserIdActual();
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Unauthorized();
            }

            var deshabilitado = await _twoFactorService.DisableAsync(user, request.Password);
            if (!deshabilitado)
            {
                var passwordValida = await _userManager.CheckPasswordAsync(user, request.Password);
                if (!passwordValida)
                {
                    return Unauthorized(new { message = "Contraseña inválida" });
                }

                return BadRequest(new { message = "No se pudo deshabilitar 2FA" });
            }

            return Ok(new { message = "Autenticación de dos factores deshabilitada exitosamente" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deshabilitando 2FA");
            return StatusCode(500, new { message = "Error al deshabilitar 2FA" });
        }
    }

    /// <summary>
    /// Completa el desafío 2FA usando el token parcial emitido durante login
    /// </summary>
    [HttpPost("2fa/challenge")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Complete2FAChallenge([FromBody] TwoFactorChallengeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            JwtSecurityToken challengeToken;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                challengeToken = tokenHandler.ReadJwtToken(request.ChallengeToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token de desafío 2FA inválido o malformado");
                return StatusCodeOnly(StatusCodes.Status401Unauthorized);
            }

            var tokenType = challengeToken.Claims.FirstOrDefault(c => c.Type == "type")?.Value;
            if (!string.Equals(tokenType, "2fa-challenge", StringComparison.Ordinal))
            {
                return StatusCodeOnly(StatusCodes.Status401Unauthorized);
            }

            if (challengeToken.ValidTo <= DateTime.UtcNow)
            {
                return StatusCodeOnly(StatusCodes.Status401Unauthorized);
            }

            var userId = challengeToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub || c.Type == "sub")?.Value;
            var jti = challengeToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti || c.Type == "jti")?.Value;

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(jti))
            {
                return StatusCodeOnly(StatusCodes.Status401Unauthorized);
            }

            var consumedKey = $"auth:2fa:challenge:consumed:{jti}";
            if (_memoryCache.TryGetValue(consumedKey, out _))
            {
                return StatusCodeOnly(StatusCodes.Status401Unauthorized);
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return StatusCodeOnly(StatusCodes.Status401Unauthorized);
            }

            var valido = await _twoFactorService.VerifyCodeAsync(user, request.Code);
            if (!valido)
            {
                return StatusCodeOnly(StatusCodes.Status401Unauthorized);
            }

            _memoryCache.Set(consumedKey, true, TimeSpan.FromMinutes(6));

            var response = await _authService.IssueAuthenticatedResponseAsync(user.Id);
            return JsonResponse(response);
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCodeOnly(StatusCodes.Status401Unauthorized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completando desafío 2FA");
            return StatusCode(500, new { message = "An error occurred while completing 2FA challenge" });
        }
    }

    // ─────────────────────────────────────────────
    // Métodos auxiliares privados
    // ─────────────────────────────────────────────

    /// <summary>
    /// Extrae el ID del usuario autenticado del token JWT
    /// </summary>
    private string? ObtenerUserIdActual()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(claim) ? null : claim;
    }

    private ContentResult JsonResponse<T>(T payload, int statusCode = StatusCodes.Status200OK)
    {
        return new ContentResult
        {
            StatusCode = statusCode,
            ContentType = "application/json",
            Content = JsonSerializer.Serialize(payload, CamelCaseJson)
        };
    }

    private ContentResult StatusCodeOnly(int statusCode)
    {
        return new ContentResult
        {
            StatusCode = statusCode,
            ContentType = "application/json",
            Content = string.Empty
        };
    }
}
