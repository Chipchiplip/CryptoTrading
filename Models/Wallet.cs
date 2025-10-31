using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    public class Wallet
    {
        [Key]
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(10)]
        public string AssetType { get; set; } = null!; // "FIAT" or "COIN"

        [MaxLength(3)]
        public string? CurrencyCode { get; set; } // For FIAT (USD, EUR, etc.)

        public int? CryptocurrencyId { get; set; } // For COIN

        // Navigation properties
        public User User { get; set; } = null!;
        public Cryptocurrency? Cryptocurrency { get; set; }
    }

    public class WalletMovement
    {
        [Key]
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong Id { get; set; }

        [Required]
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong WalletId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RefType { get; set; } = null!; // DEPOSIT, WITHDRAW, ORDER_HOLD, etc.

        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong? RefId { get; set; }

        [Required]
        [Column(TypeName = "decimal(38,18)")]
        public decimal Amount { get; set; } // Positive = credit, Negative = debit

        [MaxLength(255)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Wallet Wallet { get; set; } = null!;
    }
}

