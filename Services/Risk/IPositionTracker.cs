namespace CryptoTradingApp.Services.Risk;

/// <summary>
/// Service for tracking open positions and calculating realtime PnL
/// </summary>
public interface IPositionTracker
{
    /// <summary>
    /// Gets all open positions for a bot
    /// </summary>
    Task<List<PositionDto>> GetOpenPositionsAsync(int botId);

    /// <summary>
    /// Gets aggregated position summary for a bot
    /// </summary>
    Task<PositionSummary> GetPositionSummaryAsync(int botId);

    /// <summary>
    /// Calculates unrealized PnL for all open positions
    /// </summary>
    Task<decimal> CalculateUnrealizedPnLAsync(int botId, string symbol, decimal currentPrice);

    /// <summary>
    /// Calculates realized PnL from completed trades
    /// </summary>
    Task<decimal> CalculateRealizedPnLAsync(int botId, DateTime? since = null);

    /// <summary>
    /// Gets total exposure (open position value) for a bot
    /// </summary>
    Task<decimal> GetTotalExposureAsync(int botId);

    /// <summary>
    /// Gets total exposure across all bots for a user
    /// </summary>
    Task<decimal> GetUserTotalExposureAsync(int userId);
}

/// <summary>
/// Position data transfer object
/// </summary>
public class PositionDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty; // "LONG" or "SHORT"
    public decimal Quantity { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal UnrealizedPnLPercent { get; set; }
    public DateTime EntryTime { get; set; }
    public decimal PositionValue { get; set; }
}

/// <summary>
/// Aggregated position summary
/// </summary>
public class PositionSummary
{
    public int BotId { get; set; }
    public int OpenPositionCount { get; set; }
    public decimal TotalPositionValue { get; set; }
    public decimal TotalUnrealizedPnL { get; set; }
    public decimal TotalRealizedPnL { get; set; }
    public decimal NetPnL { get; set; }
    public decimal TotalExposure { get; set; }
    public List<PositionDto> Positions { get; set; } = new();
}
