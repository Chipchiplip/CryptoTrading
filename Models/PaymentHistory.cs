using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models;

public class PaymentHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }
    
    [Required, MaxLength(3)]
    public string Currency { get; set; } = "USD";
    
    [Required, MaxLength(50)]
    public string Status { get; set; } = string.Empty; // PENDING, COMPLETED, FAILED, REFUNDED
    
    [MaxLength(255)]
    public string? StripePaymentIntentId { get; set; }
    
    [MaxLength(255)]
    public string? StripeChargeId { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Navigation properties
    public User User { get; set; } = null!;
}
