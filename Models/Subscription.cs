using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models;

public class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    public int PlanType { get; set; } // 0=Free, 1=Plus, 2=Pro
    
    [Required, MaxLength(50)]
    public string Status { get; set; } = string.Empty; // ACTIVE, CANCELLED, EXPIRED
    
    [MaxLength(255)]
    public string? StripeSubscriptionId { get; set; }
    
    [MaxLength(255)]
    public string? StripeCustomerId { get; set; }
    
    public DateTime CurrentPeriodStartUtc { get; set; }
    public DateTime CurrentPeriodEndUtc { get; set; }
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Navigation properties
    public User User { get; set; } = null!;
}
