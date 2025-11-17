using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models;

/// <summary>
/// Tracks kill switch trigger events for audit and analysis
/// </summary>
[Table("KillSwitchEvents")]
public class KillSwitchEvent
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid BotId { get; set; }

    [Required]
    [MaxLength(500)]
    public string TriggerReason { get; set; } = string.Empty;

    [Required]
    public DateTime TriggerTime { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal? TotalLoss { get; set; }

    public int? ConsecutiveLosses { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(BotId))]
    public virtual TradingBot? Bot { get; set; }
}
