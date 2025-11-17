using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    /// <summary>
    /// User-configured trading bot instance
    /// </summary>
    [Table("TradingBots")]
    public class TradingBot
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Owner user ID
        /// </summary>
        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>
        /// Strategy definition ID
        /// </summary>
        [Required]
        public Guid StrategyDefinitionId { get; set; }

        [ForeignKey(nameof(StrategyDefinitionId))]
        public BotStrategyDefinition? StrategyDefinition { get; set; }

        /// <summary>
        /// User-friendly name for the bot
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Bot status: Draft, Starting, Running, Stopping, Stopped, Error, Degraded
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Draft";

        /// <summary>
        /// Risk profile: Conservative, Moderate, Aggressive
        /// </summary>
        [MaxLength(50)]
        public string? RiskProfile { get; set; }

        /// <summary>
        /// Base asset (e.g., "BTC", "ETH")
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string BaseAsset { get; set; } = string.Empty;

        /// <summary>
        /// Quote asset (e.g., "USDT", "USD")
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string QuoteAsset { get; set; } = string.Empty;

        /// <summary>
        /// Position sizing configuration (JSON)
        /// </summary>
        [Column(TypeName = "JSON")]
        public string? PositionSizing { get; set; }

        /// <summary>
        /// Strategy-specific parameters (JSON)
        /// </summary>
        [Column(TypeName = "JSON")]
        public string? Parameters { get; set; }

        /// <summary>
        /// Execution interval in seconds
        /// </summary>
        public int ExecutionIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// Next scheduled execution time
        /// </summary>
        public DateTime? NextRunAt { get; set; }

        /// <summary>
        /// Last status change reason
        /// </summary>
        [MaxLength(1000)]
        public string? LastStatusReason { get; set; }

        [Column(TypeName = "datetime(6)")]
        public DateTime CreatedAt { get; set; }

        [Column(TypeName = "datetime(6)")]
        public DateTime? UpdatedAt { get; set; }
    }
}

