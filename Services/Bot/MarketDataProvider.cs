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
    }
}

