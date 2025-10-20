using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models.DTOs
{
    public class TwoFactorSetupDto
    {
        public string Secret { get; set; } = string.Empty;
        public string QrCodeUri { get; set; } = string.Empty;
        public string ManualEntryKey { get; set; } = string.Empty;
    }

    public class EnableTwoFactorDto
    {
        public string Code { get; set; } = string.Empty;
    }

    public class VerifyTwoFactorDto
    {
        public string Code { get; set; } = string.Empty;
    }

    public class DisableTwoFactorDto
    {
        public string Password { get; set; } = string.Empty;
    }

    public class Login2FADto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }

    public class ConfirmEmailDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;
    }

    public class RefreshTokenDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
