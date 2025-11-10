using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Timezone { get; set; }
    }

    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Timezone { get; set; }
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}