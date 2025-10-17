using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models;

public class Trade
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Guid OrderId { get; set; }
    
    [Required]
    public int CryptocurrencyId { get; set; }
    
    [Required, MaxLength(10)]
    public string Side { get; set; } = string.Empty; // BUY, SELL
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal Quantity { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal Price { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal Fee { get; set; } = 0;
    
    public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Order Order { get; set; } = null!;
    public Cryptocurrency Cryptocurrency { get; set; } = null!;
}
