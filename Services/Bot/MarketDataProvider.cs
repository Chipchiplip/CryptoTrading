using System;
using System.Collections.Generic;
using CryptoTrading.Interfaces.Bot;
using CryptoTrading.Services;
using Microsoft.Extensions.Logging;
using System.Linq;

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

        public async Task<List<OhlcvData>> GetOhlcvAsync(
            string baseAsset,
            string quoteAsset,
            DateTime startDate,
            DateTime endDate,
            string interval = "1h",
            CancellationToken cancellationToken = default)
        {
            if (endDate <= startDate)
            {
                return new List<OhlcvData>();
            }

            try
            {
                var marketData = await _coinGeckoService.GetMarketDataAsync();
                var coin = marketData.FirstOrDefault(c =>
                    c.Symbol.Equals(baseAsset, StringComparison.OrdinalIgnoreCase) ||
                    c.Id.Equals(baseAsset, StringComparison.OrdinalIgnoreCase));

                if (coin == null)
                {
                    _logger.LogWarning("Unable to find coin data for asset {Asset}", baseAsset);
                    return new List<OhlcvData>();
                }

                var totalDays = Math.Max(1, (int)Math.Ceiling((endDate - startDate).TotalDays));
                var priceHistory = await _coinGeckoService.GetPriceHistoryAsync(coin.Id ?? coin.Symbol, totalDays);

                if (priceHistory == null || priceHistory.Count == 0)
                {
                    _logger.LogWarning("No price history returned for {Asset}", baseAsset);
                    return new List<OhlcvData>();
                }

                var filtered = priceHistory
                    .Where(p => p.Timestamp >= startDate && p.Timestamp <= endDate)
                    .OrderBy(p => p.Timestamp)
                    .ToList();

                if (filtered.Count == 0)
                {
                    filtered = priceHistory.OrderBy(p => p.Timestamp).ToList();
                }

                var intervalSpan = interval.ToLower() switch
                {
                    "1m" => TimeSpan.FromMinutes(1),
                    "5m" => TimeSpan.FromMinutes(5),
                    "15m" => TimeSpan.FromMinutes(15),
                    "30m" => TimeSpan.FromMinutes(30),
                    "1h" => TimeSpan.FromHours(1),
                    "4h" => TimeSpan.FromHours(4),
                    "1d" => TimeSpan.FromDays(1),
                    _ => TimeSpan.FromHours(1)
                };

                var grouped = filtered
                    .GroupBy(p =>
                    {
                        var ts = DateTime.SpecifyKind(p.Timestamp, DateTimeKind.Utc);
                        var ticks = ts.Ticks - (ts.Ticks % intervalSpan.Ticks);
                        return new DateTime(ticks, DateTimeKind.Utc);
                    })
                    .OrderBy(g => g.Key);

                var candles = new List<OhlcvData>();
                foreach (var group in grouped)
                {
                    var ordered = group.OrderBy(p => p.Timestamp).ToList();
                    var open = ordered.First().Price;
                    var close = ordered.Last().Price;
                    var high = ordered.Max(p => p.Price);
                    var low = ordered.Min(p => p.Price);

                    candles.Add(new OhlcvData
                    {
                        Timestamp = group.Key,
                        Open = open,
                        High = high,
                        Low = low,
                        Close = close,
                        Volume = 0
                    });
                }

                return candles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve OHLCV data for {Asset}/{Quote}", baseAsset, quoteAsset);
                return new List<OhlcvData>();
            }
        }
    }
}

