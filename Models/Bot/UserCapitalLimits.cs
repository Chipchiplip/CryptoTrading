using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models;

/// <summary>
/// Defines capital and risk limits for each user
/// </summary>
[Table("UserCapitalLimits")]
public class UserCapitalLimits
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Maximum total exposure across all bots
    /// </summary>
    [Column(TypeName = "decimal(18,8)")]
    public decimal MaxTotalExposure { get; set; } = 10000.00m;

    /// <summary>
    /// Maximum capital allocation per individual bot
    /// </summary>
    [Column(TypeName = "decimal(18,8)")]
    public decimal MaxCapitalPerBot { get; set; } = 5000.00m;

    /// <summary>
    /// Maximum number of concurrent bots allowed
    /// </summary>
    public int MaxBotsAllowed { get; set; } = 5;

    /// <summary>
    /// Maximum loss allowed per day across all bots
    /// </summary>
    [Column(TypeName = "decimal(18,8)")]
    public decimal MaxDailyLoss { get; set; } = 500.00m;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    // Note: Assuming User model exists with Id property
    // If using ASP.NET Identity, this might be AspNetUsers table
}
