using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    public class Order
    {
        [Key]
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int CryptocurrencyId { get; set; }

        [Required]
        [MaxLength(4)]
        public string Side { get; set; } = null!; // "BUY" or "SELL"

        [Required]
        [MaxLength(12)]
        public string Type { get; set; } = null!; // "MARKET" or "LIMIT"

        [Required]
        [MaxLength(12)]
        public string Status { get; set; } = "NEW"; // NEW, PARTIAL, FILLED, CANCELED, REJECTED

        [Column(TypeName = "decimal(30,10)")]
        public decimal? PriceUsd { get; set; } // NULL for MARKET orders

        [Required]
        [Column(TypeName = "decimal(38,18)")]
        public decimal QuantityCoin { get; set; }

        [Required]
        [Column(TypeName = "decimal(38,18)")]
        public decimal FilledQty { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public Cryptocurrency Cryptocurrency { get; set; } = null!;
    }

    public class OrderHold
    {
        [Key]
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong Id { get; set; }

        [Required]
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong OrderId { get; set; }

        [Required]
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong WalletId { get; set; }

        [Required]
        [Column(TypeName = "decimal(38,18)")]
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReleasedAt { get; set; }

        // Navigation properties
        public Order Order { get; set; } = null!;
        public Wallet Wallet { get; set; } = null!;
    }

    public class Trade
    {
        [Key]
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong Id { get; set; }

        [Required]
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong OrderId { get; set; }

        [Required]
        public int CryptocurrencyId { get; set; }

        [Required]
        [Column(TypeName = "decimal(30,10)")]
        public decimal PriceUsd { get; set; }

        [Required]
        [Column(TypeName = "decimal(38,18)")]
        public decimal QuantityCoin { get; set; }

        [Required]
        [Column(TypeName = "decimal(30,10)")]
        public decimal FeeUsd { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Order Order { get; set; } = null!;
        public Cryptocurrency Cryptocurrency { get; set; } = null!;
    }
}

