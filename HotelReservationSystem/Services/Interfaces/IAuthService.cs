using HotelReservationSystem.Models.DTOs;

namespace HotelReservationSystem.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
    Task<UserDto> UpdateUserAsync(int userId, UpdateUserRequest request);
    Task<UserDto?> GetUserByIdAsync(int userId);
    Task<IEnumerable<UserDto>> GetUsersAsync();
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<bool> DeactivateUserAsync(int userId);
    Task<bool> HasHotelAccessAsync(int userId, int hotelId);
    Task<IEnumerable<int>> GetUserHotelAccessAsync(int userId);
    
    // 2FA methods
    Task<string> Enable2FAAsync(int userId);
    Task<bool> Verify2FACodeAsync(int userId, string code);
}