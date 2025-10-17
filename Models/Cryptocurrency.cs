using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models;

public class Cryptocurrency
{
    public int Id { get; set; }
    
    [Required, MaxLength(50)]
    public string CoinGeckoId { get; set; } = string.Empty;
    
    [Required, MaxLength(10)]
    public string Symbol { get; set; } = string.Empty;
    
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? IconUrl { get; set; }
    
    public int? MarketCapRank { get; set; }
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Navigation properties
    public ICollection<WatchlistItem> WatchlistItems { get; set; } = new List<WatchlistItem>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Position> Positions { get; set; } = new List<Position>();
    public ICollection<Balance> Balances { get; set; } = new List<Balance>();
    public ICollection<CryptoPrice> Prices { get; set; } = new List<CryptoPrice>();
    public ICollection<Trade> Trades { get; set; } = new List<Trade>();
}
