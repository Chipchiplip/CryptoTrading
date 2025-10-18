using CryptoTrading.Core.Models.DTOs;
namespace CryptoTrading.Core.Interfaces
{
    public interface ITwoFactorService
    {
        Task<TwoFactorSetupDto> GenerateSetupAsync(int userId);
        Task<bool> EnableTwoFactorAsync(int userId, string code);
        Task<bool> DisableTwoFactorAsync(int userId, string password);
        Task<bool> VerifyTwoFactorCodeAsync(int userId, string code);
    }
}