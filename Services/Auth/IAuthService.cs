using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Services.Auth
{
    public interface IAuthService
    {
        // ========== AUTHENTICATION ==========
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<AuthResponseDto> LoginWith2FAAsync(string email, string code);

        // ========== EMAIL CONFIRMATION ==========
        Task<bool> ConfirmEmailAsync(string email, string token);
        Task TestConfirmEmailAsync(string email);

        // ========== PASSWORD RESET ==========
        Task<string> GeneratePasswordResetTokenAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordDto resetDto);
        Task<string> TestGetResetTokenAsync(string email);

        // ========== TWO-FACTOR AUTHENTICATION ==========
        Task<object> Enable2FAAsync();
        Task<object> Verify2FAAsync(string code);
        Task<bool> Disable2FAAsync(DisableTwoFactorDto dto);
        Task<string> TestGenerate2FACodeAsync(string email);

        // ========== USER PROFILE MANAGEMENT ==========
        Task<UserProfileDto> GetProfileAsync(int userId);
        Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto dto);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);

        // ========== ADMIN - USER MANAGEMENT ==========
        Task<IEnumerable<UserListDto>> GetAllUsersAsync();
        Task<bool> UpdateUserRoleAsync(int userId, UpdateUserRoleDto dto);
        Task<bool> UpdateUserLevelAsync(int userId, UpdateLevelDtoUser dto);
        Task<bool> UpdateUserStatusAsync(int userId, UpdateUserStatusDto dto);
    }
}