using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models
{
    public class Watchlist
    {
        public Guid Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;
        
        public bool IsDefault { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public User User { get; set; } = null!;
        public ICollection<WatchlistItem> Items { get; set; } = new List<WatchlistItem>();
    }

    public class WatchlistItem
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid WatchlistId { get; set; }
        
        [Required]
        [MaxLength(10)]
        public string CoinSymbol { get; set; } = null!;
        
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public Watchlist Watchlist { get; set; } = null!;
    }
}
