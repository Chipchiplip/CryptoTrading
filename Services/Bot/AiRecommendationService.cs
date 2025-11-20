using CryptoTrading.Data;
using CryptoTrading.Interfaces.Bot;
using CryptoTrading.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoTrading.Services.Bot
{
    /// <summary>
    /// Service for building trading plans and market snapshots for AI recommendations
    /// </summary>
    public class AiRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPortfolioService _portfolioService;
        private readonly IMarketDataProvider _marketDataProvider;
        private readonly IRiskManager _riskManager;
        private readonly ILogger<AiRecommendationService> _logger;

        public AiRecommendationService(
            ApplicationDbContext context,
            IPortfolioService portfolioService,
            IMarketDataProvider marketDataProvider,
            IRiskManager riskManager,
            ILogger<AiRecommendationService> logger)
        {
            _context = context;
            _portfolioService = portfolioService;
            _marketDataProvider = marketDataProvider;
            _riskManager = riskManager;
            _logger = logger;
        }

        /// <summary>
        /// Build trading plan from bot configuration
        /// </summary>
        public async Task<TradingPlanDto> BuildTradingPlanAsync(int userId, Guid? botId, CancellationToken ct = default)
        {
            TradingBot? bot = null;
            if (botId.HasValue)
            {
                bot = await _context.TradingBots
                    .Include(b => b.StrategyDefinition)
                    .FirstOrDefaultAsync(b => b.Id == botId.Value && b.UserId == userId, ct);

                if (bot == null)
                {
                    throw new KeyNotFoundException("Bot not found");
                }
            }

            // Get risk limits
            var maxExposure = await _riskManager.GetMaxExposureAsync(userId, ct);
            var maxCapitalPerTrade = maxExposure * 0.2m; // 20% of available exposure per trade

            // Map risk profile to AI risk mode
            var riskProfile = bot?.RiskProfile ?? "Moderate";
            var riskMode = riskProfile switch
            {
                "Conservative" => "low",
                "Moderate" => "normal",
                "Aggressive" => "high",
                _ => "normal"
            };

            // Extract strategy type from bot parameters or use default
            var strategyType = "trend following"; // default
            if (bot != null && !string.IsNullOrEmpty(bot.Parameters))
            {
                try
                {
                    var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(bot.Parameters);
                    if (parameters != null && parameters.TryGetValue("strategy_type", out var strategy))
                    {
                        strategyType = strategy.ToString() ?? "trend following";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse bot parameters for strategy type");
                }
            }

            // Get symbols from bot or default
            var symbols = new List<string>();
            if (bot != null)
            {
                var symbol = $"{bot.BaseAsset}{bot.QuoteAsset}";
                symbols.Add(symbol);
            }
            else
            {
                symbols.AddRange(new[] { "BTCUSDT", "ETHUSDT" });
            }

            // Extract position sizing limits
            decimal? maxCapitalFromSizing = null;
            if (bot != null && !string.IsNullOrEmpty(bot.PositionSizing))
            {
                try
                {
                    var sizing = JsonSerializer.Deserialize<Dictionary<string, object>>(bot.PositionSizing);
                    if (sizing != null && sizing.TryGetValue("maxCapitalPerTrade", out var maxCapital))
                    {
                        maxCapitalFromSizing = Convert.ToDecimal(maxCapital);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse position sizing");
                }
            }

            var planId = botId.HasValue 
                ? $"BOT_{botId.Value}" 
                : $"USER_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}";

            return new TradingPlanDto
            {
                Id = planId,
                PreferredSymbols = symbols.ToArray(),
                StrategyType = strategyType,
                RiskMode = riskMode,
                MaxCapitalPerTrade = (double)(maxCapitalFromSizing ?? (decimal)maxCapitalPerTrade),
                MaxDailyExposure = (double)maxExposure,
                TimeHorizon = "intraday", // Can be extracted from bot parameters
                MinConfidence = 0.6,
                EntryConditions = ExtractEntryConditions(bot),
                ExitConditions = ExtractExitConditions(bot),
                Restrictions = ExtractRestrictions(bot)
            };
        }

        /// <summary>
        /// Build market snapshot from current market data and user holdings
        /// </summary>
        public async Task<MarketSnapshotDto> BuildMarketSnapshotAsync(
            int userId, 
            Guid? botId, 
            string? symbol = null,
            CancellationToken ct = default)
        {
            TradingBot? bot = null;
            if (botId.HasValue)
            {
                bot = await _context.TradingBots
                    .FirstOrDefaultAsync(b => b.Id == botId.Value && b.UserId == userId, ct);

                if (bot == null)
                {
                    throw new KeyNotFoundException("Bot not found");
                }
            }

            // Determine symbol
            var targetSymbol = symbol ?? (bot != null ? $"{bot.BaseAsset}{bot.QuoteAsset}" : "BTCUSDT");

            // Get current market data
            var marketData = await _marketDataProvider.GetMarketDataAsync(targetSymbol, ct);
            if (marketData == null)
            {
                throw new InvalidOperationException($"Market data not available for {targetSymbol}");
            }

            // Get user holdings
            var baseAsset = targetSymbol.Replace("USDT", "").Replace("USD", "");
            var usdtBalance = await _portfolioService.GetBalanceAsync(userId, "USDT", ct);
            var btcHolding = await _portfolioService.GetBalanceAsync(userId, "BTC", ct);
            var ethHolding = await _portfolioService.GetBalanceAsync(userId, "ETH", ct);

            // Get holdings for the specific symbol
            decimal symbolHolding = 0;
            if (baseAsset == "BTC")
            {
                symbolHolding = btcHolding;
            }
            else if (baseAsset == "ETH")
            {
                symbolHolding = ethHolding;
            }
            else
            {
                symbolHolding = await _portfolioService.GetBalanceAsync(userId, baseAsset, ct);
            }

            // Get bot positions if applicable
            if (bot != null)
            {
                var positions = await _portfolioService.GetOpenPositionsAsync(bot.Id, ct);
                var botPosition = positions.FirstOrDefault(p => p.Asset == baseAsset);
                if (botPosition != null && botPosition.Quantity > 0)
                {
                    symbolHolding += botPosition.Quantity;
                }
            }

            return new MarketSnapshotDto
            {
                Symbol = targetSymbol,
                Price = (double)marketData.CurrentPrice,
                Trend1h = marketData.Trend1h,
                Trend4h = marketData.Trend4h,
                VolumeVsMa = marketData.VolumeChangePercent,
                Volatility = marketData.Volatility,
                Support = marketData.SupportLevel.HasValue ? (double?)marketData.SupportLevel.Value : null,
                Resistance = marketData.ResistanceLevel.HasValue ? (double?)marketData.ResistanceLevel.Value : null,
                UsdtBalance = (double)usdtBalance,
                BtcHolding = (double)btcHolding,
                EthHolding = (double)ethHolding,
                HasBadNews = marketData.HasBadNews ?? false,
                Meta = new Dictionary<string, object>
                {
                    ["symbol_holding"] = (double)symbolHolding,
                    ["bot_id"] = botId?.ToString() ?? "none"
                }
            };
        }

        private Dictionary<string, object>? ExtractEntryConditions(TradingBot? bot)
        {
            if (bot == null || string.IsNullOrEmpty(bot.Parameters))
                return null;

            try
            {
                var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(bot.Parameters);
                if (parameters == null)
                    return null;

                var conditions = new Dictionary<string, object>();

                // Extract entry filters if present
                if (parameters.TryGetValue("entryFilters", out var entryFilters))
                {
                    conditions["entryFilters"] = entryFilters;
                }

                return conditions.Count > 0 ? conditions : null;
            }
            catch
            {
                return null;
            }
        }

        private Dictionary<string, object>? ExtractExitConditions(TradingBot? bot)
        {
            if (bot == null || string.IsNullOrEmpty(bot.Parameters))
                return null;

            try
            {
                var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(bot.Parameters);
                if (parameters == null)
                    return null;

                var conditions = new Dictionary<string, object>();

                // Extract exit rules if present
                if (parameters.TryGetValue("exitRules", out var exitRules))
                {
                    conditions["exitRules"] = exitRules;
                }

                return conditions.Count > 0 ? conditions : null;
            }
            catch
            {
                return null;
            }
        }

        private Dictionary<string, object>? ExtractRestrictions(TradingBot? bot)
        {
            if (bot == null || string.IsNullOrEmpty(bot.Parameters))
                return null;

            try
            {
                var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(bot.Parameters);
                if (parameters == null)
                    return null;

                var restrictions = new Dictionary<string, object>();

                // Extract forbidden conditions
                if (parameters.TryGetValue("forbiddenConditions", out var forbidden))
                {
                    restrictions["forbiddenConditions"] = forbidden;
                }

                // High volatility restriction based on risk profile
                if (bot.RiskProfile == "Conservative")
                {
                    restrictions["high_volatility"] = true;
                }

                return restrictions.Count > 0 ? restrictions : null;
            }
            catch
            {
                return null;
            }
        }
    }

    // DTOs for AI service
    public class TradingPlanDto
    {
        public string Id { get; set; } = string.Empty;
        public string[] PreferredSymbols { get; set; } = Array.Empty<string>();
        public string StrategyType { get; set; } = string.Empty;
        public string RiskMode { get; set; } = "normal";
        public double MaxCapitalPerTrade { get; set; }
        public double MaxDailyExposure { get; set; }
        public string TimeHorizon { get; set; } = "intraday";
        public double MinConfidence { get; set; } = 0.6;
        public Dictionary<string, object>? EntryConditions { get; set; }
        public Dictionary<string, object>? ExitConditions { get; set; }
        public Dictionary<string, object>? Restrictions { get; set; }
    }

    public class MarketSnapshotDto
    {
        public string Symbol { get; set; } = string.Empty;
        public double Price { get; set; }
        public string? Trend1h { get; set; }
        public string? Trend4h { get; set; }
        public double? VolumeVsMa { get; set; }
        public double? Volatility { get; set; }
        public double? Support { get; set; }
        public double? Resistance { get; set; }
        public double UsdtBalance { get; set; }
        public double BtcHolding { get; set; }
        public double EthHolding { get; set; }
        public bool HasBadNews { get; set; }
        public Dictionary<string, object>? Meta { get; set; }
    }
}

