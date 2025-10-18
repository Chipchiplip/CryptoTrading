using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models.DTOs
{
    public class TwoFactorDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;
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
