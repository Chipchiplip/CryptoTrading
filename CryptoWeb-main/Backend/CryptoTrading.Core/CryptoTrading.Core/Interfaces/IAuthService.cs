using CryptoTrading.Core.Models.DTOs;

namespace CryptoTrading.Core.Interfaces
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
    }
}