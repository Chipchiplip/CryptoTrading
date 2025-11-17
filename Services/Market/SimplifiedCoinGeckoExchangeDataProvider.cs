using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTrading.Models.Market;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services.Market;

/// <summary>
/// Simplified exchange data provider using CoinGecko as upstream source
/// Generates Bid/Ask from single spot price with configurable spread
/// </summary>
public class SimplifiedCoinGeckoExchangeDataProvider : IExchangeDataProvider
{
    private readonly IUpstreamPriceSource _upstreamSource;
    private readonly ILogger<SimplifiedCoinGeckoExchangeDataProvider> _logger;

    // Default spread: 0.05% (0.0005)
    // This means Bid = Last * (1 - 0.0005), Ask = Last * (1 + 0.0005)
    private const decimal DEFAULT_SPREAD = 0.0005m;

    public string ExchangeName => "CoinGecko-Simplified";

    public SimplifiedCoinGeckoExchangeDataProvider(
        IUpstreamPriceSource upstreamSource,
        ILogger<SimplifiedCoinGeckoExchangeDataProvider> logger)
    {
        _upstreamSource = upstreamSource;
        _logger = logger;
    }

    /// <summary>
    /// Gets a market quote with Bid/Ask generated from spot price
    /// </summary>
    public async Task<MarketQuote?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            // Fetch spot price from upstream (CoinGecko)
            var spotPrice = await _upstreamSource.GetSpotPriceAsync(symbol, cancellationToken);

            if (spotPrice is null or <= 0)
            {
                _logger.LogWarning("Invalid spot price for {Symbol}: {Price}", symbol, spotPrice);
                return null;
            }

            // Generate Bid and Ask from spot price with spread
            var bid = spotPrice.Value * (1 - DEFAULT_SPREAD);
            var ask = spotPrice.Value * (1 + DEFAULT_SPREAD);

            var quote = new MarketQuote
            {
                Symbol = symbol,
                Bid = bid,
                Ask = ask,
                Last = spotPrice.Value,
                Timestamp = DateTime.UtcNow,
                Source = _upstreamSource.SourceName
            };

            _logger.LogDebug(
                "Generated quote for {Symbol}: Bid={Bid}, Ask={Ask}, Mid={Mid}, Spread={Spread:F4}%",
                symbol, quote.Bid, quote.Ask, quote.Mid, quote.SpreadPercent);

            return quote;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get quote for {Symbol}", symbol);
            return null;
        }
    }

    /// <summary>
    /// Gets mid price: (Bid + Ask) / 2
    /// </summary>
    public async Task<decimal?> GetMidPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var quote = await GetQuoteAsync(symbol, cancellationToken);
        return quote?.Mid;
    }

    /// <summary>
    /// Gets OHLCV candles (not implemented in this simplified version)
    /// </summary>
    public Task<List<OHLCVCandle>> GetCandlesAsync(
        string symbol,
        string interval,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("GetCandlesAsync not implemented in SimplifiedCoinGeckoExchangeDataProvider");
        return Task.FromResult(new List<OHLCVCandle>());
    }

    /// <summary>
    /// Health check
    /// </summary>
    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to fetch a sample price (BTC)
            var price = await _upstreamSource.GetSpotPriceAsync("BTC", cancellationToken);
            var isHealthy = price.HasValue && price.Value > 0;

            _logger.LogDebug("Health check: {Status}", isHealthy ? "OK" : "FAILED");

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return false;
        }
    }

    #region Deprecated Methods (kept for compatibility)

    [Obsolete("Use GetQuoteAsync instead")]
    public async Task<decimal?> GetMarkPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return await _upstreamSource.GetSpotPriceAsync(symbol, cancellationToken);
    }

    [Obsolete("Use GetQuoteAsync instead")]
    public async Task<decimal?> GetOrderBookMidPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return await GetMidPriceAsync(symbol, cancellationToken);
    }

    [Obsolete("Not supported in simplified version")]
    public Task<OrderBook?> GetOrderBookAsync(string symbol, int depth = 20, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<OrderBook?>(null);
    }

    [Obsolete("Not supported in simplified version")]
    public Task<TickerData?> GetTickerAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<TickerData?>(null);
    }

    #endregion
}
