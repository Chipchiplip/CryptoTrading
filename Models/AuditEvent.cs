using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    /// <summary>
    /// Audit trail for all critical operations
    /// </summary>
    public class AuditEvent
    {
        [Key]
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = null!; // ORDER_PLACED, TRADE_EXECUTED, BALANCE_CHANGED, etc.

        [Required]
        public int UserId { get; set; }

        public Guid? BotId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CorrelationId { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = null!; // Order, Trade, Wallet, etc.

        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong? EntityId { get; set; }

        [Column(TypeName = "json")]
        public string? BeforeState { get; set; }

        [Column(TypeName = "json")]
        public string? AfterState { get; set; }

        [Column(TypeName = "text")]
        public string? Metadata { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }
    }

    /// <summary>
    /// Fee ledger for tracking all fees
    /// </summary>
    public class FeeLedger
    {
        [Key]
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong? TradeId { get; set; }

        [Required]
        [MaxLength(10)]
        public string FeeType { get; set; } = null!; // TRADING, WITHDRAWAL, DEPOSIT

        [Required]
        [Column(TypeName = "decimal(30,10)")]
        public decimal FeeAmount { get; set; }

        [Required]
        [MaxLength(10)]
        public string FeeCurrency { get; set; } = null!; // USD, BTC, ETH, etc.

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User User { get; set; } = null!;
    }

    /// <summary>
    /// Configuration for trading system
    /// </summary>
    public class TradingConfiguration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ConfigKey { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string ConfigValue { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(20)]
        public string Environment { get; set; } = "Production"; // Development, Sandbox, Production

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? UpdatedBy { get; set; }
    }

    /// <summary>
    /// Bot configuration parameters
    /// </summary>
    public class BotRiskConfiguration
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; } // NULL for global defaults

        public Guid? BotId { get; set; } // NULL for user defaults

        [Column(TypeName = "decimal(30,10)")]
        public decimal MaxAllowedCapital { get; set; } = 100000m;

        [Column(TypeName = "decimal(10,4)")]
        public decimal MaxSlippage { get; set; } = 0.05m; // 5%

        [Column(TypeName = "decimal(10,4)")]
        public decimal MaxDailyLoss { get; set; } = 0.10m; // 10%

        public int MaxConsecutiveLosses { get; set; } = 5;

        public int CooldownSeconds { get; set; } = 300; // 5 minutes

        public bool KillSwitchEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public User? User { get; set; }
    }

    /// <summary>
    /// Reconciliation results
    /// </summary>
    public class ReconciliationResult
    {
        [Key]
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong Id { get; set; }

        [Required]
        public DateTime ReconciliationTime { get; set; }

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = null!; // ORDERS, TRADES, WALLETS

        public int TotalChecked { get; set; }

        public int MismatchCount { get; set; }

        [Column(TypeName = "json")]
        public string? Mismatches { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "COMPLETED"; // RUNNING, COMPLETED, FAILED

        [Column(TypeName = "text")]
        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

