using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Interfaces;

public interface IUserService
{
    // User Profile
    Task<UserProfileDto?> GetProfileAsync(int userId);
    Task<bool> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
    Task<List<LoginActivityDto>> GetLoginActivityAsync(int userId, int limit = 20);

    // Admin - User Management
    Task<List<UserListDto>> GetAllUsersAsync(string? roleFilter = null, bool? isActiveFilter = null);
    Task<UserProfileDto?> GetUserByIdAsync(int userId);
    Task<bool> UpdateRoleAsync(int userId, int roleId);
    Task<bool> UpdateLevelAsync(int userId, int levelId);
    Task<bool> UpdateUserStatusAsync(int userId, bool isActive);
    Task<bool> DeleteUserAsync(int userId); // Soft delete
}