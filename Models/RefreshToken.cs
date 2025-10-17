using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required, MaxLength(255)]
    public string Token { get; set; } = string.Empty;
    
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAtUtc { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
}
