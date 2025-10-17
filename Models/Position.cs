using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models;

public class Position
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public int CryptocurrencyId { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal Quantity { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal AverageCost { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal? UnrealizedPnL { get; set; }
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Cryptocurrency Cryptocurrency { get; set; } = null!;
}
