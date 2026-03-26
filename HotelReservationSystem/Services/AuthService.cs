using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelReservationSystem.Models;
using HotelReservationSystem.Models.DTOs;
using HotelReservationSystem.Services.Interfaces;
using HotelReservationSystem.Data;
using HotelReservationSystem.Infrastructure;

namespace HotelReservationSystem.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IJwtService _jwtService;
    private readonly HotelReservationContext _context;
    private readonly ICacheService _cacheService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IConfiguration configuration,
        IJwtService jwtService,
        HotelReservationContext context,
        ICacheService cacheService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _jwtService = jwtService;
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Intento de inicio de sesión para {Email}", request.Email);

        // Buscar usuario por email o nombre de usuario
        var user = await _userManager.FindByEmailAsync(request.Email)
                   ?? await _userManager.FindByNameAsync(request.Email);

        if (user == null)
        {
            _logger.LogWarning("Usuario {Email} no encontrado en la base de datos", request.Email);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Usuario {Email} (ID: {Id}) no está activo", request.Email, user.Id);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Verificar contraseña usando SignInManager para registrar intentos fallidos y bloqueo
        await VerifyPasswordWithLockoutAsync(user, request.Email, request.Password);

        // Verificar expiración de contraseña (90 días)
        VerifyPasswordExpiration(user, request.Email);

        // Emitir desafío 2FA si está habilitado; la verificación ocurre en el endpoint de challenge
        if (user.TwoFactorEnabled)
        {
            var partialToken = _jwtService.IssuePartialAuthToken(user.Id.ToString());

            _logger.LogInformation("Usuario {Email} requiere desafío 2FA antes de emitir JWT", request.Email);

            return new LoginResponse
            {
                RequiresTwoFactor = true,
                ChallengeToken = partialToken,
                TwoFactorEnabled = true
            };
        }

        _logger.LogInformation("Usuario {Email} inició sesión correctamente", request.Email);

        return await IssueAuthenticatedResponseAsync(user.Id);
    }

    public async Task<LoginResponse> IssueAuthenticatedResponseAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var token = await GenerateJwtTokenAsync(user);
        var userDto = await MapToUserDtoAsync(user);

        return new LoginResponse
        {
            Token = token,
            Expires = DateTime.UtcNow.AddHours(8),
            User = userDto,
            RequiresTwoFactor = false,
            TwoFactorEnabled = user.TwoFactorEnabled
        };
    }

    // Verifica la contraseña usando SignInManager para registrar intentos fallidos y aplicar bloqueo
    private async Task VerifyPasswordWithLockoutAsync(User user, string email, string password)
    {
        // Excepción para usuario de demo (solo en entorno de pruebas)
        if (email == "admin@demo.com" && password == "admin123")
        {
            _logger.LogInformation("Omitiendo autenticación para admin@demo.com en modo demo");
            return;
        }

        // Verificar si la cuenta ya está bloqueada antes de intentar la contraseña
        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Usuario {Email} está bloqueado por intentos fallidos", email);
            throw new UnauthorizedAccessException("Account is locked out due to multiple failed login attempts. Please try again later.");
        }

        // Usar SignInManager para verificar contraseña con seguimiento de intentos fallidos
        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Usuario {Email} bloqueado tras demasiados intentos fallidos", email);
            throw new UnauthorizedAccessException("Account is locked out due to multiple failed login attempts. Please try again later.");
        }

        if (!result.Succeeded)
        {
            _logger.LogWarning("Contraseña inválida para usuario {Email}", email);
            throw new UnauthorizedAccessException("Invalid credentials");
        }
    }

    // Verifica si la contraseña del usuario ha expirado (política de 90 días)
    private void VerifyPasswordExpiration(User user, string email)
    {
        if (!user.PasswordChangedDate.HasValue ||
            (DateTime.UtcNow - user.PasswordChangedDate.Value).TotalDays > 90)
        {
            _logger.LogWarning("Contraseña expirada para usuario {Email}. Debe cambiarla.", email);
            throw new UnauthorizedAccessException("Password has expired. Please change your password.");
        }
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        // Add hotel access
        await UpdateUserHotelAccessAsync(user.Id, request.HotelIds);

        _logger.LogInformation("User {Email} created successfully", request.Email);

        return await MapToUserDtoAsync(user);
    }

    public async Task<UserDto> UpdateUserAsync(int userId, UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new ArgumentException("User not found");
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user: {errors}");
        }

        // Actualizar acceso a hoteles e invalidar caché de permisos
        await UpdateUserHotelAccessAsync(userId, request.HotelIds);

        _logger.LogInformation("User {UserId} updated successfully", userId);

        return await MapToUserDtoAsync(user);
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return null;
        }

        return await MapToUserDtoAsync(user);
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            userDtos.Add(await MapToUserDtoAsync(user));
        }

        return userDtos;
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        // Check password history
        var recentPasswords = await _context.UserPasswordHistories
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .ToListAsync();

        foreach (var pastPassword in recentPasswords)
        {
            if (_userManager.PasswordHasher.VerifyHashedPassword(user, pastPassword.PasswordHash, request.NewPassword) != PasswordVerificationResult.Failed)
            {
                throw new InvalidOperationException("Cannot reuse any of your last 5 passwords.");
            }
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (result.Succeeded)
        {
            user.PasswordChangedDate = DateTime.UtcNow;
            
            _context.UserPasswordHistories.Add(new UserPasswordHistory
            {
                UserId = userId,
                PasswordHash = _userManager.PasswordHasher.HashPassword(user, request.NewPassword),
                CreatedAt = DateTime.UtcNow
            });
            
            await _userManager.UpdateAsync(user); // Save PasswordChangedDate
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Password changed for user {UserId}", userId);
        }

        return result.Succeeded;
    }

    public async Task<bool> DeactivateUserAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} deactivated", userId);
        }

        return result.Succeeded;
    }

    public async Task<bool> HasHotelAccessAsync(int userId, int hotelId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsActive)
        {
            return false;
        }

        // Los administradores tienen acceso a todos los hoteles
        if (user.Role == UserRole.Admin)
        {
            return true;
        }

        // Verificar permisos usando el caché de acceso del usuario
        var accessibleHotels = await GetUserHotelAccessAsync(userId);
        return accessibleHotels.Contains(hotelId);
    }

    public async Task<IEnumerable<int>> GetUserHotelAccessAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsActive)
        {
            return new List<int>();
        }

        // Administradores tienen acceso a todos los hoteles activos
        if (user.Role == UserRole.Admin)
        {
            var cacheKeyAdmin = string.Format(CacheKeys.UserHotelAccess, $"{userId}:admin");
            return await _cacheService.GetOrSetAsync(cacheKeyAdmin, async () =>
            {
                return await _context.Hotels
                    .Where(h => h.IsActive)
                    .Select(h => h.Id)
                    .ToListAsync();
            }, CacheKeys.Expiration.UserPermissions);
        }

        // Usuarios regulares: obtener hoteles asignados desde caché (expiración 10 minutos)
        var cacheKey = string.Format(CacheKeys.UserHotelAccess, userId);
        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            _logger.LogDebug("Cargando permisos de hotel para usuario {UserId} desde BD", userId);
            return await _context.UserHotelAccess
                .Where(uha => uha.UserId == userId)
                .Select(uha => uha.HotelId)
                .ToListAsync();
        }, CacheKeys.Expiration.UserPermissions);
    }

    private async Task<string> GenerateJwtTokenAsync(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured"));

        var hotelIds = await GetUserHotelAccessAsync(user.Id);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new("role", user.Role.ToString()),
            new("hotels", string.Join(",", hotelIds))
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private async Task<UserDto> MapToUserDtoAsync(User user)
    {
        var hotelIds = await GetUserHotelAccessAsync(user.Id);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            IsActive = user.IsActive,
            AccessibleHotelIds = hotelIds
        };
    }

    private async Task UpdateUserHotelAccessAsync(int userId, IEnumerable<int> hotelIds)
    {
        // Eliminar acceso existente
        var existingAccess = await _context.UserHotelAccess
            .Where(uha => uha.UserId == userId)
            .ToListAsync();

        _context.UserHotelAccess.RemoveRange(existingAccess);

        // Agregar nuevo acceso
        foreach (var hotelId in hotelIds)
        {
            _context.UserHotelAccess.Add(new UserHotelAccess
            {
                UserId = userId,
                HotelId = hotelId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        // Invalidar caché de permisos del usuario (expiración 10 minutos)
        await _cacheService.RemoveByPatternAsync(string.Format(CacheKeys.Patterns.UserSpecific, userId));
        _logger.LogDebug("Caché de permisos invalidada para usuario {UserId}", userId);
    }
}
