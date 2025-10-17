using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? FirstName { get; set; }
    
    [MaxLength(100)]
    public string? LastName { get; set; }
    
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiresAtUtc { get; set; }
    
    public bool IsTwoFactorEnabled { get; set; } = false;
    public string? TwoFactorSecret { get; set; }
    
    public int SubscriptionTier { get; set; } = 0; // 0=Free, 1=Plus, 2=Pro
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserWatchlist> Watchlists { get; set; } = new List<UserWatchlist>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Position> Positions { get; set; } = new List<Position>();
    public ICollection<Balance> Balances { get; set; } = new List<Balance>();
    public ICollection<Bot> Bots { get; set; } = new List<Bot>();
    public ICollection<Trade> Trades { get; set; } = new List<Trade>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<PaymentHistory> PaymentHistories { get; set; } = new List<PaymentHistory>();
}
