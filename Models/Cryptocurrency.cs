using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    [Table("Cryptocurrencies", Schema = "market")]
    public class Cryptocurrency
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string CoinGeckoId { get; set; } = string.Empty;

        [Required]
        [MaxLength(24)]
        public string Symbol { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? IconUrl { get; set; }

        public int? MarketCapRank { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<CryptoPrice> Prices { get; set; } = new List<CryptoPrice>();
    }
}

