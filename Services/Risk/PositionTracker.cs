using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTrading.Data;
using CryptoTrading.Services.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services.Risk;

/// <summary>
/// Tracks open positions and calculates realtime PnL
/// </summary>
public class PositionTracker : IPositionTracker
{
    private readonly ApplicationDbContext _context;
    private readonly IExchangeDataProvider _exchangeDataProvider;
    private readonly ILogger<PositionTracker> _logger;

    public PositionTracker(
        ApplicationDbContext context,
        IExchangeDataProvider exchangeDataProvider,
        ILogger<PositionTracker> logger)
    {
        _context = context;
        _exchangeDataProvider = exchangeDataProvider;
        _logger = logger;
    }

    public async Task<List<PositionDto>> GetOpenPositionsAsync(Guid botId)
    {
        if (ShouldSkipBotScopedOperation(botId, nameof(GetOpenPositionsAsync)))
        {
            return new List<PositionDto>();
        }

        try
        {
            var botOrders = await _context.TradingBotOrders
                .Include(tbo => tbo.Order)
                    .ThenInclude(o => o!.Cryptocurrency)
                .Where(tbo => tbo.TradingBotId == botId &&
                              tbo.Order != null &&
                              tbo.Order.Status == "FILLED")
                .ToListAsync();

            if (botOrders.Count == 0)
            {
                return new List<PositionDto>();
            }

            var symbolGroups = botOrders
                .Where(o => o.Order?.Cryptocurrency != null)
                .GroupBy(o => o.Order!.Cryptocurrency!.Symbol);

            var priceMap = await GetCurrentPricesAsync(symbolGroups.Select(g => g.Key));

            var positions = new List<PositionDto>();

            foreach (var group in symbolGroups)
            {
                var netQuantity = group.Sum(o => (o.Order!.Side == "SELL" ? -1 : 1) * o.Order.FilledQty);
                if (netQuantity == 0)
                {
                    continue;
                }

                var totalFilled = group.Sum(o => o.Order!.FilledQty);
                var weightedCost = group.Sum(o => (o.Order!.PriceUsd ?? 0m) * o.Order.FilledQty);
                var avgEntryPrice = totalFilled > 0 ? weightedCost / totalFilled : 0m;

                var symbol = group.Key;
                var currentPrice = priceMap.TryGetValue(symbol, out var price) ? price : 0m;
                var absQty = Math.Abs(netQuantity);
                var side = netQuantity >= 0 ? "LONG" : "SHORT";
                var positionValue = absQty * currentPrice;

                var unrealized = netQuantity >= 0
                    ? (currentPrice - avgEntryPrice) * absQty
                    : (avgEntryPrice - currentPrice) * absQty;

                var unrealizedPercent = avgEntryPrice > 0
                    ? (currentPrice - avgEntryPrice) / avgEntryPrice * 100m * (netQuantity >= 0 ? 1 : -1)
                    : 0m;

                positions.Add(new PositionDto
                {
                    Symbol = symbol,
                    Side = side,
                    Quantity = absQty,
                    EntryPrice = avgEntryPrice,
                    CurrentPrice = currentPrice,
                    EntryTime = group.Min(o => o.CreatedAt),
                    PositionValue = positionValue,
                    UnrealizedPnL = unrealized,
                    UnrealizedPnLPercent = unrealizedPercent
                });
            }

            return positions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting open positions for bot {BotId}", botId);
            return new List<PositionDto>();
        }
    }

    public async Task<PositionSummary> GetPositionSummaryAsync(Guid botId)
    {
        if (ShouldSkipBotScopedOperation(botId, nameof(GetPositionSummaryAsync)))
        {
            return new PositionSummary { BotId = Guid.Empty };
        }

        try
        {
            var positions = await GetOpenPositionsAsync(botId);
            var realizedPnL = await CalculateRealizedPnLAsync(botId);

            return new PositionSummary
            {
                BotId = botId,
                OpenPositionCount = positions.Count,
                TotalPositionValue = positions.Sum(p => p.PositionValue),
                TotalUnrealizedPnL = positions.Sum(p => p.UnrealizedPnL),
                TotalRealizedPnL = realizedPnL,
                NetPnL = positions.Sum(p => p.UnrealizedPnL) + realizedPnL,
                TotalExposure = positions.Sum(p => p.PositionValue),
                Positions = positions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting position summary for bot {BotId}", botId);
            return new PositionSummary { BotId = botId };
        }
    }

    public async Task<decimal> CalculateUnrealizedPnLAsync(Guid botId, string symbol, decimal currentPrice)
    {
        if (ShouldSkipBotScopedOperation(botId, nameof(CalculateUnrealizedPnLAsync)))
        {
            return 0;
        }

        try
        {
            var positions = await GetOpenPositionsAsync(botId);
            var position = positions.FirstOrDefault(p => string.Equals(p.Symbol, symbol, StringComparison.OrdinalIgnoreCase));

            if (position == null)
            {
                return 0;
            }

            if (currentPrice <= 0)
            {
                currentPrice = position.CurrentPrice;
            }

            var qty = position.Quantity;
            var entryPrice = position.EntryPrice;

            return position.Side == "SHORT"
                ? (entryPrice - currentPrice) * qty
                : (currentPrice - entryPrice) * qty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating unrealized PnL for bot {BotId}, symbol {Symbol}", botId, symbol);
            return 0;
        }
    }

    public async Task<decimal> CalculateRealizedPnLAsync(Guid botId, DateTime? since = null)
    {
        if (ShouldSkipBotScopedOperation(botId, nameof(CalculateRealizedPnLAsync)))
        {
            return 0;
        }

        try
        {
            var botOrderIds = await _context.TradingBotOrders
                .Where(tbo => tbo.TradingBotId == botId)
                .Select(tbo => tbo.OrderId)
                .ToListAsync();

            if (botOrderIds.Count == 0)
            {
                return 0;
            }

            var orderSides = await _context.Orders
                .Where(o => botOrderIds.Contains(o.Id))
                .Select(o => new { o.Id, o.Side })
                .ToDictionaryAsync(o => o.Id, o => o.Side);

            var tradesQuery = _context.Trades
                .Where(t => botOrderIds.Contains(t.OrderId));

            if (since.HasValue)
            {
                tradesQuery = tradesQuery.Where(t => t.CreatedAt >= since.Value);
            }

            var trades = await tradesQuery.ToListAsync();

            decimal realized = 0;
            foreach (var trade in trades)
            {
                if (!orderSides.TryGetValue(trade.OrderId, out var side))
                {
                    continue;
                }

                var gross = trade.PriceUsd * trade.QuantityCoin;
                var fee = trade.FeeUsd;

                if (string.Equals(side, "BUY", StringComparison.OrdinalIgnoreCase))
                {
                    realized -= gross + fee;
                }
                else
                {
                    realized += gross - fee;
                }
            }

            return realized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating realized PnL for bot {BotId}", botId);
            return 0;
        }
    }

    public async Task<decimal> GetTotalExposureAsync(Guid botId)
    {
        if (ShouldSkipBotScopedOperation(botId, nameof(GetTotalExposureAsync)))
        {
            return 0;
        }

        try
        {
            var positions = await GetOpenPositionsAsync(botId);
            return positions.Sum(p => p.PositionValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total exposure for bot {BotId}", botId);
            return 0;
        }
    }

    public async Task<decimal> GetUserTotalExposureAsync(int userId)
    {
        try
        {
            var botIds = await _context.TradingBots
                .Where(b => b.UserId == userId && (b.Status == "Running" || b.Status == "Starting"))
                .Select(b => b.Id)
                .ToListAsync();

            if (botIds.Count == 0)
            {
                return 0;
            }

            decimal totalExposure = 0;
            foreach (var botId in botIds)
            {
                totalExposure += await GetTotalExposureAsync(botId);
            }

            return totalExposure;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total exposure for user {UserId}", userId);
            return 0;
        }
    }

    private async Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> symbols)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var symbol in symbols.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            decimal price = 0;
            try
            {
                var ticker = await _exchangeDataProvider.GetTickerAsync(symbol);
                if (ticker?.LastPrice > 0)
                {
                    price = ticker.LastPrice;
                }
                else
                {
                    var cryptoId = await _context.Cryptocurrencies
                        .Where(c => c.Symbol == symbol)
                        .Select(c => c.Id)
                        .FirstOrDefaultAsync();

                    if (cryptoId != 0)
                    {
                        price = await _context.CryptoPrices
                            .Where(p => p.CryptocurrencyId == cryptoId)
                            .OrderByDescending(p => p.CollectedAtUtc)
                            .Select(p => p.PriceUsd)
                            .FirstOrDefaultAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch current price for {Symbol}", symbol);
            }

            result[symbol] = price;
        }

        return result;
    }

    private bool ShouldSkipBotScopedOperation(Guid botId, string operationName)
    {
        if (botId == Guid.Empty)
        {
            _logger.LogDebug("{Operation} skipped because botId is empty (demo guard).", operationName);
            return true;
        }

        return false;
    }
}
