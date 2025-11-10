using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models.DTOs
{
    // ========== STRATEGY DTOs ==========

    /// <summary>
    /// Strategy definition metadata for UI
    /// </summary>
    public class StrategyDefinitionDto
    {
        public Guid Id { get; set; }
        public string StrategyKey { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public object? ParametersSchema { get; set; } // JSON Schema object
        public int MaxConcurrency { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ========== BOT DTOs ==========

    /// <summary>
    /// Summary for bot list/dashboard
    /// </summary>
    public class TradingBotSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string BaseAsset { get; set; } = string.Empty;
        public string QuoteAsset { get; set; } = string.Empty;
        public StrategyInfoDto Strategy { get; set; } = new();
        public BotRuntimeInfoDto? Runtime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Detailed bot configuration and state
    /// </summary>
    public class TradingBotDetailDto
    {
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? RiskProfile { get; set; }
        public string BaseAsset { get; set; } = string.Empty;
        public string QuoteAsset { get; set; } = string.Empty;
        public StrategyInfoDto Strategy { get; set; } = new();
        public Dictionary<string, object>? Parameters { get; set; }
        public Dictionary<string, object>? PositionSizing { get; set; }
        public int ExecutionIntervalSeconds { get; set; }
        public DateTime? NextRunAt { get; set; }
        public string? LastStatusReason { get; set; }
        public BotRuntimeInfoDto? Runtime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class StrategyInfoDto
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public class BotRuntimeInfoDto
    {
        public DateTime? NextRunAt { get; set; }
        public int OpenPositions { get; set; }
        public decimal UnrealizedPnl { get; set; }
        public decimal RealizedPnl { get; set; }
        public decimal TotalFees { get; set; }
        public int TotalOrders { get; set; }
        public int FilledOrders { get; set; }
        public string? LastSignal { get; set; }
        public DateTime? LastExecutionAt { get; set; }
        public DateTime? HeartbeatAt { get; set; }
    }

    // ========== REQUEST DTOs ==========

    /// <summary>
    /// Create new bot request
    /// </summary>
    public class CreateBotRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public Guid StrategyDefinitionId { get; set; }

        [Required]
        [MaxLength(20)]
        public string BaseAsset { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string QuoteAsset { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? RiskProfile { get; set; }

        [Required]
        public Dictionary<string, object> Parameters { get; set; } = new();

        public Dictionary<string, object>? PositionSizing { get; set; }

        [Range(1, 86400)]
        public int ExecutionIntervalSeconds { get; set; } = 60;
    }

    /// <summary>
    /// Update bot configuration (only when stopped)
    /// </summary>
    public class UpdateBotRequest
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        [MaxLength(50)]
        public string? RiskProfile { get; set; }

        public Dictionary<string, object>? Parameters { get; set; }

        public Dictionary<string, object>? PositionSizing { get; set; }

        [Range(1, 86400)]
        public int? ExecutionIntervalSeconds { get; set; }
    }

    /// <summary>
    /// Start bot request
    /// </summary>
    public class StartBotRequest
    {
        /// <summary>
        /// Execution mode: live, paper
        /// </summary>
        [MaxLength(20)]
        public string Mode { get; set; } = "live";
    }

    /// <summary>
    /// Stop bot request
    /// </summary>
    public class StopBotRequest
    {
        /// <summary>
        /// Reason: user, risk, error, maintenance
        /// </summary>
        [MaxLength(50)]
        public string Reason { get; set; } = "user";
    }

    // ========== LOG DTOs ==========

    public class BotLogDto
    {
        public ulong Id { get; set; }
        public Guid BotId { get; set; }
        public string Level { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Payload { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Query parameters for logs
    /// </summary>
    public class BotLogsQuery
    {
        public string? Level { get; set; }
        public string? Category { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    // ========== ORDER DTOs ==========

    public class BotOrderDto
    {
        public ulong Id { get; set; }
        public Guid BotId { get; set; }
        public ulong OrderId { get; set; }
        public string Intent { get; set; } = string.Empty;
        public string? SignalId { get; set; }
        public OrderDto? OrderDetails { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ========== SIGNALR EVENT DTOs ==========

    /// <summary>
    /// Bot status update event
    /// </summary>
    public class BotStatusUpdatedEvent
    {
        public Guid BotId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public DateTime? NextRunAt { get; set; }
        public DateTime HeartbeatAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Bot metrics update event
    /// </summary>
    public class BotMetricUpdatedEvent
    {
        public Guid BotId { get; set; }
        public decimal Pnl { get; set; }
        public int OpenOrders { get; set; }
        public decimal Exposure { get; set; }
        public string? LastSignal { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Bot execution log appended event
    /// </summary>
    public class BotExecutionLogAppendedEvent
    {
        public Guid BotId { get; set; }
        public string Level { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Payload { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Bot order event
    /// </summary>
    public class BotOrderEvent
    {
        public Guid BotId { get; set; }
        public ulong OrderId { get; set; }
        public string Intent { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal Filled { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Bot alert event
    /// </summary>
    public class BotAlertRaisedEvent
    {
        public Guid BotId { get; set; }
        public string Severity { get; set; } = string.Empty; // info, warn, error, critical
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? SuggestedAction { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Strategy catalog changed event
    /// </summary>
    public class StrategyCatalogChangedEvent
    {
        public string StrategyKey { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty; // added, updated, disabled
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // ========== SIMULATION DTOs ==========

    /// <summary>
    /// Backtest/simulation request
    /// </summary>
    public class SimulationRequest
    {
        [Required]
        public Guid StrategyDefinitionId { get; set; }

        [Required]
        public Dictionary<string, object> Parameters { get; set; } = new();

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public decimal InitialCapital { get; set; }
    }

    /// <summary>
    /// Simulation result
    /// </summary>
    public class SimulationResultDto
    {
        public decimal FinalCapital { get; set; }
        public decimal TotalReturn { get; set; }
        public decimal ReturnPercentage { get; set; }
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal WinRate { get; set; }
        public decimal MaxDrawdown { get; set; }
        public decimal SharpeRatio { get; set; }
        public List<SimulationTradeDto> Trades { get; set; } = new();
        public List<SimulationEquityPoint> EquityCurve { get; set; } = new();
    }

    public class SimulationTradeDto
    {
        public DateTime Timestamp { get; set; }
        public string Side { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal Pnl { get; set; }
    }

    public class SimulationEquityPoint
    {
        public DateTime Timestamp { get; set; }
        public decimal Equity { get; set; }
    }
}

