using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models;

public class WatchlistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid WatchlistId { get; set; }
    
    [Required]
    public int CryptocurrencyId { get; set; }
    
    public int SortOrder { get; set; } = 0;
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Navigation properties
    public UserWatchlist Watchlist { get; set; } = null!;
    public Cryptocurrency Cryptocurrency { get; set; } = null!;
}
