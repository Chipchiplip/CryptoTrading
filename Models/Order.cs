using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public int CryptocurrencyId { get; set; }
    
    [Required, MaxLength(10)]
    public string Side { get; set; } = string.Empty; // BUY, SELL
    
    [Required, MaxLength(20)]
    public string Type { get; set; } = string.Empty; // MARKET, LIMIT, STOP_LOSS
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal Quantity { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal? Price { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal? StopPrice { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal FilledQuantity { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal? AvgFillPrice { get; set; }
    
    [Required, MaxLength(20)]
    public string Status { get; set; } = "PENDING"; // PENDING, FILLED, CANCELLED, REJECTED
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Cryptocurrency Cryptocurrency { get; set; } = null!;
    public ICollection<Trade> Trades { get; set; } = new List<Trade>();
}
