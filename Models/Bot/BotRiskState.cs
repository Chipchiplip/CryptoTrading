using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models;

/// <summary>
/// Tracks realtime risk metrics for each bot
/// </summary>
[Table("BotRiskStates")]
public class BotRiskState
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid BotId { get; set; }

    /// <summary>
    /// Number of consecutive losing trades
    /// </summary>
    public int ConsecutiveLosses { get; set; } = 0;

    /// <summary>
    /// Total loss incurred today (resets daily)
    /// </summary>
    [Column(TypeName = "decimal(18,8)")]
    public decimal DailyLoss { get; set; } = 0;

    /// <summary>
    /// Last time daily loss was reset
    /// </summary>
    [Column(TypeName = "datetime(6)")]
    public DateTime DailyLossResetAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total drawdown from peak equity
    /// </summary>
    [Column(TypeName = "decimal(18,8)")]
    public decimal TotalDrawdown { get; set; } = 0;

    /// <summary>
    /// Timestamp of last order placed
    /// </summary>
    [Column(TypeName = "datetime(6)")]
    public DateTime? LastOrderAt { get; set; }

    /// <summary>
    /// Number of orders placed in current execution cycle
    /// </summary>
    public int OrderCountThisCycle { get; set; } = 0;

    [Column(TypeName = "datetime(6)")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(BotId))]
    public virtual TradingBot? Bot { get; set; }
}
