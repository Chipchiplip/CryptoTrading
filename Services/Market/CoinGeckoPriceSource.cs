using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTrading.Services;
using Microsoft.Extensions.Logging;

namespace CryptoTradingApp.Services.Market;

/// <summary>
/// Adapter for ICoinGeckoService to provide spot prices
/// </summary>
public class CoinGeckoPriceSource : IUpstreamPriceSource
{
    private readonly ICoinGeckoService _coinGeckoService;
    private readonly ICryptoCacheService _cacheService;
    private readonly ILogger<CoinGeckoPriceSource> _logger;

    public string SourceName => "CoinGecko";

    public CoinGeckoPriceSource(
        ICoinGeckoService coinGeckoService,
        ICryptoCacheService cacheService,
        ILogger<CoinGeckoPriceSource> logger)
    {
        _coinGeckoService = coinGeckoService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<decimal?> GetSpotPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            // Normalize symbol: "BTCUSDT" -> "BTC", "BTC-USDT" -> "BTC"
            var baseAsset = NormalizeSymbol(symbol);

            // Try cache first
            if (_cacheService.TryGetCryptoData(out var cachedData) && cachedData != null)
            {
                var coin = cachedData.FirstOrDefault(c =>
                    c.Symbol.Equals(baseAsset, StringComparison.OrdinalIgnoreCase) ||
                    c.Id.Equals(baseAsset, StringComparison.OrdinalIgnoreCase));

                if (coin?.CurrentPrice > 0)
                {
                    _logger.LogDebug(
                        "CoinGecko spot price from cache for {Symbol}: {Price}",
                        symbol, coin.CurrentPrice);
                    return coin.CurrentPrice;
                }
            }

            // Fallback to API
            var marketData = await _coinGeckoService.GetMarketDataAsync();
            var coinFromApi = marketData.FirstOrDefault(c =>
                c.Symbol.Equals(baseAsset, StringComparison.OrdinalIgnoreCase) ||
                c.Id.Equals(baseAsset, StringComparison.OrdinalIgnoreCase));

            if (coinFromApi?.CurrentPrice > 0)
            {
                _logger.LogDebug(
                    "CoinGecko spot price from API for {Symbol}: {Price}",
                    symbol, coinFromApi.CurrentPrice);
                return coinFromApi.CurrentPrice;
            }

            _logger.LogWarning("No price found for {Symbol} in CoinGecko", symbol);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch spot price from CoinGecko for {Symbol}", symbol);
            return null;
        }
    }

    private static string NormalizeSymbol(string symbol)
    {
        // Remove common quote currencies to get base asset
        var normalized = symbol
            .Replace("USDT", "")
            .Replace("USD", "")
            .Replace("BUSD", "")
            .Replace("-", "")
            .Replace("/", "")
            .Trim()
            .ToUpperInvariant();

        return normalized;
    }
}
