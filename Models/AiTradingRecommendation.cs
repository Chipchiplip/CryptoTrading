using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models
{
    /// <summary>
    /// AI trading recommendation entity
    /// Maps to ai_trading_recommendations table
    /// </summary>
    [Table("ai_trading_recommendations")]
    public class AiTradingRecommendation
    {
        [Key]
        [Column("Id")]
        public long Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("RecommendationId")]
        public string RecommendationId { get; set; } = string.Empty;

        [Column("CreatedAtUtc")]
        public DateTime CreatedAtUtc { get; set; }

        [MaxLength(100)]
        [Column("TradingPlanId")]
        public string? TradingPlanId { get; set; }

        [Column("MarketSnapshotJson", TypeName = "LONGTEXT")]
        public string? MarketSnapshotJson { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("Decision")]
        public string Decision { get; set; } = "NO_TRADE";

        [Required]
        [MaxLength(20)]
        [Column("Symbol")]
        public string Symbol { get; set; } = string.Empty;

        [Column("AmountUsdt", TypeName = "DECIMAL(28,8)")]
        public decimal AmountUsdt { get; set; }

        [Column("Reason", TypeName = "TEXT")]
        public string? Reason { get; set; }

        [Column("Confidence", TypeName = "DECIMAL(5,4)")]
        public decimal Confidence { get; set; }

        [MaxLength(20)]
        [Column("TimeHorizon")]
        public string? TimeHorizon { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("Status")]
        public string Status { get; set; } = "pending";

        [Column("AppliedAtUtc")]
        public DateTime? AppliedAtUtc { get; set; }

        [Column("AppliedOrderId")]
        public long? AppliedOrderId { get; set; }
    }
}

