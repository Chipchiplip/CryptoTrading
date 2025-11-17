namespace CryptoTrading.Services.Risk;

/// <summary>
/// Kill switch service for detecting and responding to risk thresholds
/// </summary>
public interface IKillSwitchService
{
    /// <summary>
    /// Checks if the kill switch should be triggered for a bot
    /// </summary>

    /// <param name="botId">Bot ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>True if bot should be stopped, false otherwise</returns>
    Task<KillSwitchResult> CheckKillSwitchAsync(int botId, int userId);

    /// <summary>
    /// Records a trade result for kill switch monitoring
    /// </summary>
    Task RecordTradeResultAsync(int botId, decimal pnl, bool isProfit);

    /// <summary>
    /// Gets the current risk state for a bot
    /// </summary>
    Task<BotRiskStateDto> GetRiskStateAsync(int botId);

    /// <summary>
    /// Resets daily loss counters (called at start of new trading day)
    /// </summary>
    Task ResetDailyLossAsync(int botId);

    /// <summary>
    /// Triggers the kill switch for a bot
    /// </summary>
    Task TriggerKillSwitchAsync(int botId, string reason, decimal? totalLoss = null);

    /// <summary>
    /// Clears kill switch state (manual reset)
    /// </summary>
    Task ClearKillSwitchAsync(int botId);
}

/// <summary>
/// Result of kill switch check
/// </summary>
public class KillSwitchResult
{
    public bool ShouldStop { get; set; }
    public string Reason { get; set; } = string.Empty;
    public KillSwitchTrigger? Trigger { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();

    public static KillSwitchResult Continue() => new() { ShouldStop = false };

    public static KillSwitchResult Stop(string reason, KillSwitchTrigger trigger)
        => new()
        {
            ShouldStop = true,
            Reason = reason,
            Trigger = trigger
        };
}

/// <summary>
/// Types of kill switch triggers
/// </summary>
public enum KillSwitchTrigger
{
    None = 0,
    ConsecutiveLosses = 1,
    DailyLossLimit = 2,
    MaxDrawdown = 3,
    TotalLossLimit = 4,
    ManualTrigger = 5
}

/// <summary>
/// Bot risk state DTO
/// </summary>
public class BotRiskStateDto
{
    public int BotId { get; set; }
    public int ConsecutiveLosses { get; set; }
    public decimal DailyLoss { get; set; }
    public decimal TotalDrawdown { get; set; }
    public DateTime? LastOrderAt { get; set; }
    public int OrderCountThisCycle { get; set; }
    public bool IsKillSwitchTriggered { get; set; }
    public string? KillSwitchReason { get; set; }
    public DateTime UpdatedAt { get; set; }
}
