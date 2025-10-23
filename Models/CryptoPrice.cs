using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    [Table("CryptoPrices", Schema = "market")]
    public class CryptoPrice
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public int CryptocurrencyId { get; set; }

        [Required]
        [Column(TypeName = "decimal(28,8)")]
        public decimal PriceUsd { get; set; }

        [Column(TypeName = "decimal(28,2)")]
        public decimal? MarketCap { get; set; }

        [Column(TypeName = "decimal(28,2)")]
        public decimal? Volume24h { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal? PercentChange1h { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal? PercentChange24h { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal? PercentChange7d { get; set; }

        [Column(TypeName = "decimal(28,2)")]
        public decimal? CirculatingSupply { get; set; }

        [Column(TypeName = "decimal(28,2)")]
        public decimal? TotalSupply { get; set; }

        public DateTime CollectedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("CryptocurrencyId")]
        public virtual Cryptocurrency? Cryptocurrency { get; set; }
    }
}

