using CryptoTrading.Models.DTOs;
using System.Text.Json;

namespace CryptoTrading.Interfaces.Bot
{
    /// <summary>
    /// Core interface for trading strategies
    /// </summary>
    public interface ITradingStrategy
    {
        /// <summary>
        /// Unique strategy key
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Strategy metadata
        /// </summary>
        StrategyMetadata Metadata { get; }

        /// <summary>
        /// Validates bot configuration and parameters
        /// </summary>
        Task<StrategyValidationResult> ValidateAsync(
            BotContext context, 
            BotParameters parameters, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the strategy logic for a single tick
        /// </summary>
        Task<BotExecutionResult> ExecuteAsync(
            BotContext context, 
            BotParameters parameters, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs backtesting simulation
        /// </summary>
        Task<StrategySimulationResult> SimulateAsync(
            BotContext context, 
            SimulationRequest request, 
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Strategy metadata
    /// </summary>
    public class StrategyMetadata
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string Description { get; set; } = string.Empty;
        public int MinIntervalSeconds { get; set; } = 10;
        public int RecommendedIntervalSeconds { get; set; } = 60;
        public int MaxConcurrency { get; set; } = 10;
        public List<string> SupportedAssets { get; set; } = new();
        public Dictionary<string, object>? DefaultParameters { get; set; }
        public string? ParametersSchemaJson { get; set; }
    }

    /// <summary>
    /// Bot execution context
    /// </summary>
    public class BotContext
    {
        public int BotId { get; set; }
        public int UserId { get; set; }
        public string BaseAsset { get; set; } = string.Empty;
        public string QuoteAsset { get; set; } = string.Empty;
        public decimal AllowedCapital { get; set; }
        
        // Services
        public required IBotTradingService TradingService { get; set; }
        public required IMarketDataProvider MarketData { get; set; }
        public required IPortfolioService PortfolioService { get; set; }
        public required IRiskManager RiskManager { get; set; }
        public required IBotLogger Logger { get; set; }
        public required IEventCollector EventCollector { get; set; }

        // State management - use non-generic delegates
        public required Func<Type, CancellationToken, Task<object?>> LoadStateAsyncFunc { get; set; }
        public required Func<object, CancellationToken, Task> SaveStateAsyncFunc { get; set; }

        public async Task<T?> LoadStateAsync<T>(CancellationToken cancellationToken = default) where T : class
        {
            var result = await LoadStateAsyncFunc(typeof(T), cancellationToken);
            return result as T;
        }

        public async Task SaveStateAsync<T>(T state, CancellationToken cancellationToken = default) where T : class
        {
            await SaveStateAsyncFunc(state, cancellationToken);
        }
    }

    /// <summary>
    /// Strategy parameters
    /// </summary>
    public class BotParameters
    {
        public Dictionary<string, object> Values { get; set; } = new();

        public T GetValue<T>(string key, T defaultValue = default!)
        {
            if (Values.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                    return typedValue;
                
                if (value is JsonElement jsonElement)
                {
                    try
                    {
                        object? parsed = jsonElement.ValueKind switch
                        {
                            JsonValueKind.Number when typeof(T) == typeof(int) => jsonElement.TryGetInt32(out var intVal) ? intVal : defaultValue,
                            JsonValueKind.Number when typeof(T) == typeof(long) => jsonElement.TryGetInt64(out var longVal) ? longVal : defaultValue,
                            JsonValueKind.Number when typeof(T) == typeof(decimal) => jsonElement.TryGetDecimal(out var decVal) ? decVal : defaultValue,
                            JsonValueKind.Number when typeof(T) == typeof(double) => jsonElement.TryGetDouble(out var dblVal) ? dblVal : defaultValue,
                            JsonValueKind.Number when typeof(T) == typeof(float) => jsonElement.TryGetSingle(out var floatVal) ? floatVal : defaultValue,
                            JsonValueKind.String => jsonElement.GetString(),
                            JsonValueKind.True when typeof(T) == typeof(bool) => true,
                            JsonValueKind.False when typeof(T) == typeof(bool) => false,
                            _ => jsonElement.Deserialize<T>()
                        };

                        if (parsed is T parsedValue)
                        {
                            return parsedValue;
                        }
                    }
                    catch
                    {
                        // Ignore and fall back to default conversion
                    }
                }

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }

    /// <summary>
    /// Validation result
    /// </summary>
    public class StrategyValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();

        public static StrategyValidationResult Ok() => new() { IsValid = true };
        public static StrategyValidationResult Fail(string error) => new() { IsValid = false, Errors = new List<string> { error } };
        public static StrategyValidationResult Fail(List<string> errors) => new() { IsValid = false, Errors = errors };
    }

    /// <summary>
    /// Execution result
    /// </summary>
    public class BotExecutionResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? NextRunAt { get; set; }
        public TimeSpan? RetryDelay { get; set; }
        public Dictionary<string, object>? Metrics { get; set; }
        public List<BotEvent> Events { get; set; } = new();

        public static BotExecutionResult SuccessResult(DateTime? nextRunAt = null) => 
            new() { Success = true, NextRunAt = nextRunAt };

        public static BotExecutionResult Failure(string error, TimeSpan? retryDelay = null) => 
            new() { Success = false, ErrorMessage = error, RetryDelay = retryDelay };

        public BotExecutionResult WithMetrics(Dictionary<string, object> metrics)
        {
            Metrics = metrics;
            return this;
        }

        public BotExecutionResult WithEvents(List<BotEvent> events)
        {
            Events = events;
            return this;
        }
    }

    /// <summary>
    /// Bot event
    /// </summary>
    public class BotEvent
    {
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object>? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Simulation result
    /// </summary>
    public class StrategySimulationResult
    {
        public bool Success { get; set; }
        public SimulationResultDto? Result { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

