using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<string> GeneratePasswordResetTokenAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordDto resetDto);
        Task<AuthResponseDto> LoginWith2FAAsync(string email, string code);
        Task<bool> ConfirmEmailAsync(string email, string token);
        Task TestConfirmEmailAsync(string email);
        Task<string> TestGetResetTokenAsync(string email);
        Task<object> Enable2FAAsync();
        Task<object> Verify2FAAsync(string code);
        Task<string> TestGenerate2FACodeAsync(string email);
    }
}
