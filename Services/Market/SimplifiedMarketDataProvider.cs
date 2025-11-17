using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTrading.Models.Market;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services.Market;

/// <summary>
/// Simplified market data provider using exchange data provider and validator
/// Provides clean interface for getting market quotes and mid prices
/// </summary>
public class SimplifiedMarketDataProvider
{
    private readonly IExchangeDataProvider _exchangeDataProvider;
    private readonly ISimplifiedPriceValidator _priceValidator;
    private readonly ILogger<SimplifiedMarketDataProvider> _logger;

    public SimplifiedMarketDataProvider(
        IExchangeDataProvider exchangeDataProvider,
        ISimplifiedPriceValidator priceValidator,
        ILogger<SimplifiedMarketDataProvider> logger)
    {
        _exchangeDataProvider = exchangeDataProvider;
        _priceValidator = priceValidator;
        _logger = logger;
    }

    /// <summary>
    /// Gets a validated market quote for the symbol
    /// Returns null if quote is invalid or unavailable
    /// </summary>
    public async Task<MarketQuote?> GetQuoteAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var quote = await _exchangeDataProvider.GetQuoteAsync(symbol, cancellationToken);

            if (quote == null)
            {
                _logger.LogWarning("No quote available for {Symbol}", symbol);
                return null;
            }

            var validation = _priceValidator.ValidateQuote(quote);
            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "Invalid quote for {Symbol}: {Error}",
                    symbol, validation.ErrorMessage);
                return null;
            }

            _logger.LogDebug(
                "Valid quote for {Symbol}: Bid={Bid}, Ask={Ask}, Mid={Mid}",
                quote.Symbol, quote.Bid, quote.Ask, quote.Mid);

            return quote;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get quote for {Symbol}", symbol);
            return null;
        }
    }

    /// <summary>
    /// Gets the mid price for a symbol
    /// Mid price = (Bid + Ask) / 2
    /// Used for bot calculations, risk management, and PnL
    /// </summary>
    public async Task<decimal?> GetMidPriceAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var quote = await GetQuoteAsync(symbol, cancellationToken);
        return quote?.Mid;
    }

    /// <summary>
    /// Gets the bid price for a symbol
    /// Use for SELL orders (you sell at bid)
    /// </summary>
    public async Task<decimal?> GetBidPriceAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var quote = await GetQuoteAsync(symbol, cancellationToken);
        return quote?.Bid;
    }

    /// <summary>
    /// Gets the ask price for a symbol
    /// Use for BUY orders (you buy at ask)
    /// </summary>
    public async Task<decimal?> GetAskPriceAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var quote = await GetQuoteAsync(symbol, cancellationToken);
        return quote?.Ask;
    }

    /// <summary>
    /// Gets OHLCV candles for backtesting and charting
    /// </summary>
    public async Task<List<OHLCVCandle>> GetCandlesAsync(
        string symbol,
        string interval,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _exchangeDataProvider.GetCandlesAsync(
                symbol,
                interval,
                limit,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get candles for {Symbol}", symbol);
            return new List<OHLCVCandle>();
        }
    }

    /// <summary>
    /// Checks if market data provider is healthy
    /// </summary>
    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _exchangeDataProvider.HealthCheckAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return false;
        }
    }
}
