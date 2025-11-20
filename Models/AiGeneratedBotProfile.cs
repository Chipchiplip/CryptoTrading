using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoTrading.Models;

/// <summary>
/// Stores AI generated bot suggestions for later application.
/// </summary>
[Table("AiGeneratedBotProfiles")]
public class AiGeneratedBotProfile
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public Guid? SessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SymbolsJson { get; set; } = "[]";
    public string StrategyType { get; set; } = string.Empty;
    public string RiskMode { get; set; } = string.Empty;
    public decimal MaxCapitalPerTrade { get; set; }
    public decimal MaxDailyExposure { get; set; }
    public string TimeHorizon { get; set; } = string.Empty;
    public decimal? ExpectedReturnPct { get; set; }
    public string? RiskNote { get; set; }
    public string? SourceRecommendationId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}


