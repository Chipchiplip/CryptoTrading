using CryptoTrading.Models;
using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Interfaces.Bot
{
    /// <summary>
    /// Trading service interface for bot strategies
    /// Wraps the main trading service with bot-specific context
    /// </summary>
    public interface IBotTradingService
    {
        /// <summary>
        /// Places an order on behalf of a bot
        /// </summary>
        Task<ulong> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels an order
        /// </summary>
        Task CancelOrderAsync(ulong orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets order details
        /// </summary>
        Task<OrderDetailDto> GetOrderAsync(ulong orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Estimates required capital for a trade
        /// </summary>
        Task<decimal> EstimateRequiredCapitalAsync(
            string baseAsset, 
            string quoteAsset, 
            decimal quantity, 
            decimal? price = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Market data provider interface
    /// </summary>
    public interface IMarketDataProvider
    {
        /// <summary>
        /// Gets current mid price
        /// </summary>
        Task<decimal> GetMidPriceAsync(string baseAsset, string quoteAsset, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets OHLCV data for analysis
        /// </summary>
        Task<List<OhlcvData>> GetOhlcvAsync(
            string baseAsset, 
            string quoteAsset, 
            DateTime startDate, 
            DateTime endDate,
            string interval = "1h",
            CancellationToken cancellationToken = default);
    }

    public class OhlcvData
    {
        public DateTime Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }

    /// <summary>
    /// Portfolio service for bot context
    /// </summary>
    public interface IPortfolioService
    {
        Task<decimal> GetBalanceAsync(int userId, string asset, CancellationToken cancellationToken = default);
        Task<List<PositionInfo>> GetOpenPositionsAsync(int botId, CancellationToken cancellationToken = default);
    }

    public class PositionInfo
    {
        public string Asset { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal UnrealizedPnl { get; set; }
    }

    /// <summary>
    /// Risk manager for validation
    /// </summary>
    public interface IRiskManager
    {
        Task<bool> CheckLimitsAsync(int userId, decimal requiredCapital, CancellationToken cancellationToken = default);
        Task<decimal> GetMaxExposureAsync(int userId, CancellationToken cancellationToken = default);

        // Phase 3 additions - Dynamic capital and pre-execution guardrails
        Task<decimal> GetBotCapitalLimitAsync(int userId, int botId, CancellationToken cancellationToken = default);
        Task<bool> CheckKillSwitchAsync(int botId, int userId, CancellationToken cancellationToken = default);
        Task<bool> CheckCooldownAsync(int botId, TimeSpan minCooldown, CancellationToken cancellationToken = default);
        Task<bool> CheckRateLimitAsync(int botId, int maxOrdersPerCycle, CancellationToken cancellationToken = default);

        // Phase 3B additions - Order tracking for rate limiting
        Task ResetOrderCountForNewCycleAsync(int botId, CancellationToken cancellationToken = default);
        Task RecordOrderPlacedAsync(int botId, CancellationToken cancellationToken = default);

        // Phase 4 additions - Kill switch trade result tracking
        Task RecordTradeResultAsync(int botId, decimal pnl, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Bot logger
    /// </summary>
    public interface IBotLogger
    {
        void LogInfo(string category, string message, object? payload = null);
        void LogWarning(string category, string message, object? payload = null);
        void LogError(string category, string message, object? payload = null);
    }

    /// <summary>
    /// Event collector for bot events
    /// </summary>
    public interface IEventCollector
    {
        void AddEvent(BotEvent evt);
        List<BotEvent> GetEvents();
        void Clear();
    }
}

