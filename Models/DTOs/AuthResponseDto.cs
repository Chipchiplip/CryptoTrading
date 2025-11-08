namespace CryptoTrading.Models.DTOs
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = null!;
        public bool RequiresTwoFactor { get; set; } = false;
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public bool TwoFactorEnabled { get; set; }

        public string Role { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
    }
}