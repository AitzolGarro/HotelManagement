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

namespace HotelReservationSystem.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly HotelReservationContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IConfiguration configuration,
        HotelReservationContext context,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _context = context;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var token = await GenerateJwtTokenAsync(user);
        var userDto = await MapToUserDtoAsync(user);

        _logger.LogInformation("User {Email} logged in successfully", request.Email);

        return new LoginResponse
        {
            Token = token,
            Expires = DateTime.UtcNow.AddHours(8),
            User = userDto
        };
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

        // Update hotel access
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

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (result.Succeeded)
        {
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

        // Admin users have access to all hotels
        if (user.Role == UserRole.Admin)
        {
            return true;
        }

        return await _context.UserHotelAccess
            .AnyAsync(uha => uha.UserId == userId && uha.HotelId == hotelId);
    }

    public async Task<IEnumerable<int>> GetUserHotelAccessAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsActive)
        {
            return new List<int>();
        }

        // Admin users have access to all hotels
        if (user.Role == UserRole.Admin)
        {
            return await _context.Hotels
                .Where(h => h.IsActive)
                .Select(h => h.Id)
                .ToListAsync();
        }

        return await _context.UserHotelAccess
            .Where(uha => uha.UserId == userId)
            .Select(uha => uha.HotelId)
            .ToListAsync();
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
        // Remove existing access
        var existingAccess = await _context.UserHotelAccess
            .Where(uha => uha.UserId == userId)
            .ToListAsync();

        _context.UserHotelAccess.RemoveRange(existingAccess);

        // Add new access
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
    }
}