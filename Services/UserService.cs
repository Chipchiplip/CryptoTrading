using CryptoTrading.Data;
using CryptoTrading.Interfaces;
using CryptoTrading.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CryptoTrading.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IRoleService _roleService;
    private readonly ILevelService _levelService;

    public UserService(ApplicationDbContext context, IRoleService roleService, ILevelService levelService)
    {
        _context = context;
        _roleService = roleService;
        _levelService = levelService;
    }

    // =============== USER PROFILE ===============
    public async Task<UserProfileDto?> GetProfileAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            Role = user.Role,
            Level = user.Level,
            EmailConfirmed = user.EmailConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        if (dto.FullName != null) user.FullName = dto.FullName;
        if (dto.AvatarUrl != null) user.AvatarUrl = dto.AvatarUrl;
        if (dto.Bio != null) user.Bio = dto.Bio;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return false;

        // Hash new password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<LoginActivityDto>> GetLoginActivityAsync(int userId, int limit = 20)
    {
        return await _context.Set<Models.LoginActivity>()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .Select(x => new LoginActivityDto
            {
                Id = x.Id,
                Ip = x.Ip,
                UserAgent = x.UserAgent,
                Success = x.Success,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    // =============== ADMIN - USER MANAGEMENT ===============
    public async Task<List<UserListDto>> GetAllUsersAsync(string? roleFilter = null, bool? isActiveFilter = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(roleFilter))
            query = query.Where(u => u.Role == roleFilter);

        if (isActiveFilter.HasValue)
            query = query.Where(u => u.IsActive == isActiveFilter.Value);

        return await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserListDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                Level = u.Level,
                IsActive = u.IsActive,
                EmailConfirmed = u.EmailConfirmed,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync();
    }

    public async Task<UserProfileDto?> GetUserByIdAsync(int userId)
    {
        return await GetProfileAsync(userId);
    }

    public async Task<bool> UpdateRoleAsync(int userId, int roleId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        var role = await _roleService.GetRoleByIdAsync(roleId);
        if (role == null)
        {
            return false;
        }

        user.Role = role.Name;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateLevelAsync(int userId, int levelId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        var level = await _levelService.GetLevelByIdAsync(levelId);
        if (level == null)
        {
            return false;
        }

        user.Level = level.Name;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateUserStatusAsync(int userId, bool isActive)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.IsActive = isActive;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteUserAsync(int userId)
{
    var user = await _context.Users.FindAsync(userId);
    if (user == null) return false;

    _context.Users.Remove(user);
    await _context.SaveChangesAsync();
    return true;
}
}