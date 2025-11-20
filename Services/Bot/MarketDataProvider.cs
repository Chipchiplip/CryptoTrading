using CryptoTrading.Interfaces.Bot;
using CryptoTrading.Services;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services.Bot
{
    /// <summary>
    /// Market data provider using cache service
    /// </summary>
    public class MarketDataProvider : IMarketDataProvider
    {
        private readonly ICryptoCacheService _cacheService;
        private readonly ICoinGeckoService _coinGeckoService;
        private readonly ILogger<MarketDataProvider> _logger;

        public MarketDataProvider(
            ICryptoCacheService cacheService,
            ICoinGeckoService coinGeckoService,
            ILogger<MarketDataProvider> logger)
        {
            _cacheService = cacheService;
            _coinGeckoService = coinGeckoService;
            _logger = logger;
        }

        public async Task<decimal> GetMidPriceAsync(
            string baseAsset, 
            string quoteAsset, 
            CancellationToken cancellationToken = default)
        {
            // Try cache first
            if (_cacheService.TryGetCryptoData(out var cachedData) && cachedData != null)
            {
                var coin = cachedData.FirstOrDefault(c => 
                    c.Symbol.Equals(baseAsset, StringComparison.OrdinalIgnoreCase));
                
                if (coin?.CurrentPrice > 0)
                {
                    return coin.CurrentPrice ?? 0m;
                }
            }

            // Fallback to API
            try
            {
                var marketData = await _coinGeckoService.GetMarketDataAsync();
                var coin = marketData.FirstOrDefault(c => 
                    c.Symbol.Equals(baseAsset, StringComparison.OrdinalIgnoreCase));
                
                return coin?.CurrentPrice ?? 0m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch price for {BaseAsset}", baseAsset);
                return 0m;
            }
        }

        public Task<List<OhlcvData>> GetOhlcvAsync(
            string baseAsset, 
            string quoteAsset, 
            DateTime startDate, 
            DateTime endDate, 
            string interval = "1h", 
            CancellationToken cancellationToken = default)
        {
            // TODO: Implement OHLCV data retrieval for backtesting
            _logger.LogWarning("OHLCV data retrieval not yet implemented");
            return Task.FromResult(new List<OhlcvData>());
        }

        public async Task<MarketDataForAi?> GetMarketDataAsync(string symbol, CancellationToken cancellationToken = default)
        {
            try
            {
                // Extract base asset from symbol (e.g., "BTCUSDT" -> "BTC")
                var baseAsset = symbol.Replace("USDT", "").Replace("USD", "");

                // Get current price
                var price = await GetMidPriceAsync(baseAsset, "USDT", cancellationToken);
                if (price <= 0)
                {
                    _logger.LogWarning("No price data available for {Symbol}", symbol);
                    return null;
                }

                // Try to get cached data for trend/volume analysis
                MarketDataForAi? marketData = null;
                if (_cacheService.TryGetCryptoData(out var cachedData) && cachedData != null)
                {
                    var coin = cachedData.FirstOrDefault(c => 
                        c.Symbol.Equals(baseAsset, StringComparison.OrdinalIgnoreCase));
                    
                    if (coin != null)
                    {
                        // Calculate trend from price change (more lenient thresholds)
                        var trend1h = coin.PriceChangePercentage1h.HasValue
                            ? (coin.PriceChangePercentage1h.Value > 0.5m
                                ? "uptrend"
                                : coin.PriceChangePercentage1h.Value < -0.5m
                                    ? "downtrend"
                                    : "neutral")
                            : "neutral";
                        
                        // Use 24h as proxy for 4h trend (since we don't have 4h data)
                        var trend4h = coin.PriceChangePercentage24h.HasValue
                            ? (coin.PriceChangePercentage24h.Value > 1m
                                ? "uptrend"
                                : coin.PriceChangePercentage24h.Value < -1m
                                    ? "downtrend"
                                    : "neutral")
                            : "neutral";

                        // Estimate volatility from 24h change
                        var volatility = coin.PriceChangePercentage24h.HasValue
                            ? Math.Abs((double)coin.PriceChangePercentage24h.Value / 100)
                            : 0.05; // Default 5%

                        // Estimate support/resistance (simplified - use price * 0.95 and price * 1.05)
                        var support = price * 0.95m;
                        var resistance = price * 1.05m;

                        marketData = new MarketDataForAi
                        {
                            CurrentPrice = price,
                            Trend1h = trend1h,
                            Trend4h = trend4h,
                            VolumeChangePercent = 0, // TODO: Calculate from volume data
                            Volatility = volatility,
                            SupportLevel = support,
                            ResistanceLevel = resistance,
                            HasBadNews = false // TODO: Check news feed
                        };
                    }
                }

                // Fallback if no cached data
                if (marketData == null)
                {
                    marketData = new MarketDataForAi
                    {
                        CurrentPrice = price,
                        Trend1h = "neutral",
                        Trend4h = "neutral",
                        VolumeChangePercent = 0,
                        Volatility = 0.05,
                        SupportLevel = price * 0.95m,
                        ResistanceLevel = price * 1.05m,
                        HasBadNews = false
                    };
                }

                return marketData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get market data for {Symbol}", symbol);
                return null;
            }
        }
    }
}

