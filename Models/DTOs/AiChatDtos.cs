using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace CryptoTrading.Models.DTOs;

/// <summary>
/// Request payload from frontend when user sends a chat message.
/// </summary>
public class AiChatMessageRequest
{
    [Required]
    public int UserId { get; set; }

    [MaxLength(36)]
    public string? SessionId { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response payload sent back to frontend chat UI.
/// </summary>
public class AiChatResponseDto
{
    public string SessionId { get; set; } = string.Empty;
    public string Reply { get; set; } = string.Empty;
    public List<AiChatBotSuggestionDto> Bots { get; set; } = new();
    public AiTradeSuggestion? TradeSuggestion { get; set; }
}

/// <summary>
/// Bot suggestion returned by AI chat.
/// </summary>
public class AiChatBotSuggestionDto
{
    public Guid SuggestionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string[] Symbols { get; set; } = Array.Empty<string>();
    public string StrategyType { get; set; } = string.Empty;
    public string RiskMode { get; set; } = "BALANCED";
    public decimal MaxCapitalPerTrade { get; set; }
    public decimal MaxDailyExposure { get; set; }
    public string TimeHorizon { get; set; } = "intraday";
    public decimal? ExpectedReturnPct { get; set; }
    public string? RiskNote { get; set; }
}

/// <summary>
/// Trade suggestion structure reused across services.
/// </summary>
public class AiTradeSuggestion
{
    public string Decision { get; set; } = "NO_TRADE";
    public string Symbol { get; set; } = string.Empty;
    public decimal AmountUsdt { get; set; }
    public decimal? ExpectedReturnPct { get; set; }
    public double Confidence { get; set; }
    public string TimeHorizon { get; set; } = "intraday";
}

/// <summary>
/// Request to create a trading bot based on an AI chat suggestion.
/// </summary>
public class CreateBotFromAiSuggestionRequest
{
    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string BotName { get; set; } = string.Empty;

    /// <summary>
    /// Trading symbol, e.g., BTCUSDT or BTC/USDT.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Symbol { get; set; } = string.Empty;

    [Range(1, double.MaxValue)]
    public decimal CapitalPerTrade { get; set; }

    [MaxLength(50)]
    public string? RiskProfile { get; set; }

    /// <summary>
    /// Preferred strategy key to map to StrategyDefinition.
    /// </summary>
    public string? StrategyKey { get; set; }

    /// <summary>
    /// Optional strategy parameters overrides.
    /// </summary>
    public Dictionary<string, object>? StrategyParameters { get; set; }

    public Dictionary<string, object>? PositionSizing { get; set; }

    [Range(1, 86400)]
    public int? ExecutionIntervalSeconds { get; set; }
}

