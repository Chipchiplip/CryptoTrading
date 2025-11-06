using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    public class MarketStat
    {
        [Key]
        public long Id { get; set; }

        [Column(TypeName = "decimal(28,2)")]
        public decimal TotalMarketCap { get; set; }

        [Column(TypeName = "decimal(28,2)")]
        public decimal TotalVolume { get; set; }

        public int ActiveCryptocurrencies { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal MarketCapChangePercentage24h { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal? BtcDominance { get; set; }

        [Column(TypeName = "decimal(10,4)")]
        public decimal? EthDominance { get; set; }

        public DateTime CollectedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

