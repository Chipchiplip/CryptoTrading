using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public bool EmailConfirmed { get; set; } = false;
        public string? EmailConfirmationToken { get; set; }
        public DateTime? EmailConfirmationTokenExpiry { get; set; }

        public bool TwoFactorEnabled { get; set; } = false;

        public string? TwoFactorSecret { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiryTime { get; set; }

        public string? PasswordResetToken { get; set; }

        public DateTime? PasswordResetTokenExpiry { get; set; }

        [StringLength(50)]
        public string Role { get; set; } = "User"; 

        [StringLength(50)]
        public string Level { get; set; } = "Free"; 

        public bool IsActive { get; set; } = true; 

        [StringLength(500)]
        public string? AvatarUrl { get; set; } 

        [StringLength(200)]
        public string? Bio { get; set; } 

        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        [StringLength(50)]
        public string? Timezone { get; set; }
    }
}