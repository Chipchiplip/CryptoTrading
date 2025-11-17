using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    public class Wallet
    {
        [Key]
        [Column(TypeName = "bigint unsigned")]
        public ulong Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(10)]
        public string AssetType { get; set; } = null!; // "FIAT" or "COIN"

        [MaxLength(3)]
        public string? CurrencyCode { get; set; } // For FIAT (USD, EUR, etc.)

        public int? CryptocurrencyId { get; set; } // For COIN

        public bool IsDefault { get; set; } = false;

        [Column(TypeName = "datetime(6)")]
        public DateTime CreatedAt { get; set; }

        [Column(TypeName = "datetime(6)")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public Cryptocurrency? Cryptocurrency { get; set; }
        public ICollection<WalletMovement> Movements { get; set; } = new List<WalletMovement>();
    }

    public class WalletMovement
    {
        [Key]
        [Column(TypeName = "bigint unsigned")]
        public ulong Id { get; set; }

        [Required]
        [Column(TypeName = "bigint unsigned")]
        public ulong WalletId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RefType { get; set; } = null!; // DEPOSIT, WITHDRAW, ORDER_HOLD, etc.

        [Column(TypeName = "bigint unsigned")]
        public ulong? RefId { get; set; }

        [Required]
        [MaxLength(8)]
        public string Direction { get; set; } = null!; // CREDIT / DEBIT

        [Required]
        [Column(TypeName = "decimal(38,18)")]
        public decimal Amount { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }

        [Column(TypeName = "datetime(6)")]
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public Wallet Wallet { get; set; } = null!;
    }
}

