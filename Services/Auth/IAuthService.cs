using CryptoTrading.Controllers;

namespace CryptoTrading.Services.Auth;

public interface IAuthService
{
    Task<AuthResultDto> LoginAsync(LoginDto dto);
    Task<AuthResultDto> RegisterAsync(RegisterDto dto);
    Task<AuthResultDto> RefreshTokenAsync(RefreshTokenDto dto);
    Task LogoutAsync();
    Task<bool> VerifyEmailAsync(string token);
    Task SendPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
    Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);
    Task<bool> EnableTwoFactorAsync();
    Task<bool> DisableTwoFactorAsync(string code);
    Task<string> GenerateJwtTokenAsync(Guid userId);
}
