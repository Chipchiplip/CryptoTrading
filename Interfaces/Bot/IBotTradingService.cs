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

        /// <summary>
        /// Gets order book to see limit orders from other users
        /// </summary>
        Task<OrderBookDto> GetOrderBookAsync(string symbol, int depth = 20, CancellationToken cancellationToken = default);
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

        /// <summary>
        /// Gets comprehensive market data for AI recommendations
        /// </summary>
        Task<MarketDataForAi?> GetMarketDataAsync(string symbol, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Market data structure for AI recommendations
    /// </summary>
    public class MarketDataForAi
    {
        public decimal CurrentPrice { get; set; }
        public string? Trend1h { get; set; } // "uptrend", "downtrend", "neutral"
        public string? Trend4h { get; set; }
        public double? VolumeChangePercent { get; set; }
        public double? Volatility { get; set; }
        public decimal? SupportLevel { get; set; }
        public decimal? ResistanceLevel { get; set; }
        public bool? HasBadNews { get; set; }
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
        Task<List<PositionInfo>> GetOpenPositionsAsync(Guid botId, CancellationToken cancellationToken = default);
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

