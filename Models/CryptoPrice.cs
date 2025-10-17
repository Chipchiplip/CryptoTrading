using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models;

public class CryptoPrice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public int CryptocurrencyId { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal Price { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal? MarketCap { get; set; }
    
    [Column(TypeName = "decimal(18,8)")]
    public decimal? Volume24h { get; set; }
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal? PercentChange24h { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Cryptocurrency Cryptocurrency { get; set; } = null!;
}
