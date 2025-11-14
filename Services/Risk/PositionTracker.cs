using CryptoTradingApp.Data;
using CryptoTradingApp.Services.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CryptoTradingApp.Services.Risk;

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
        ILogger<PositionTracker> _logger)
    {
        _context = context;
        _exchangeDataProvider = exchangeDataProvider;
        this._logger = _logger;
    }

    public async Task<List<PositionDto>> GetOpenPositionsAsync(int botId)
    {
        try
        {
            // Get all bot orders that are filled but not exited
            var openOrders = await _context.TradingBotOrders
                .Where(o => o.BotId == botId && o.Status == "Filled" && o.ExitPrice == null)
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();

            if (openOrders.Count == 0)
                return new List<PositionDto>();

            var positions = new List<PositionDto>();

            // Group by symbol
            var bySymbol = openOrders.GroupBy(o => o.Symbol);

            foreach (var symbolGroup in bySymbol)
            {
                var symbol = symbolGroup.Key;

                // Get current price
                var currentPrice = await _exchangeDataProvider.GetMarkPriceAsync(symbol) ?? 0;

                if (currentPrice == 0)
                {
                    _logger.LogWarning(
                        "Could not fetch current price for {Symbol}, skipping position",
                        symbol);
                    continue;
                }

                // Calculate net position (BUY - SELL)
                var buyQuantity = symbolGroup
                    .Where(o => o.Side == "BUY")
                    .Sum(o => o.Quantity);

                var sellQuantity = symbolGroup
                    .Where(o => o.Side == "SELL")
                    .Sum(o => o.Quantity);

                var netQuantity = buyQuantity - sellQuantity;

                if (netQuantity == 0)
                    continue; // Flat position

                // Calculate average entry price
                var totalBuyCost = symbolGroup
                    .Where(o => o.Side == "BUY")
                    .Sum(o => o.Quantity * o.FilledPrice);

                var totalSellRevenue = symbolGroup
                    .Where(o => o.Side == "SELL")
                    .Sum(o => o.Quantity * o.FilledPrice);

                var avgEntryPrice = netQuantity > 0
                    ? totalBuyCost / buyQuantity
                    : totalSellRevenue / sellQuantity;

                // Calculate unrealized PnL
                var positionValue = netQuantity * currentPrice;
                var costBasis = netQuantity * avgEntryPrice;
                var unrealizedPnL = positionValue - costBasis;
                var unrealizedPnLPercent = (unrealizedPnL / costBasis) * 100;

                var entryTime = symbolGroup.Min(o => o.CreatedAt);

                positions.Add(new PositionDto
                {
                    Symbol = symbol,
                    Side = netQuantity > 0 ? "LONG" : "SHORT",
                    Quantity = Math.Abs(netQuantity),
                    EntryPrice = avgEntryPrice,
                    CurrentPrice = currentPrice,
                    UnrealizedPnL = unrealizedPnL,
                    UnrealizedPnLPercent = unrealizedPnLPercent,
                    EntryTime = entryTime,
                    PositionValue = Math.Abs(positionValue)
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

    public async Task<PositionSummary> GetPositionSummaryAsync(int botId)
    {
        try
        {
            var positions = await GetOpenPositionsAsync(botId);
            var realizedPnL = await CalculateRealizedPnLAsync(botId);

            var summary = new PositionSummary
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

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting position summary for bot {BotId}", botId);
            return new PositionSummary { BotId = botId };
        }
    }

    public async Task<decimal> CalculateUnrealizedPnLAsync(int botId, string symbol, decimal currentPrice)
    {
        try
        {
            var positions = await GetOpenPositionsAsync(botId);
            var symbolPosition = positions.FirstOrDefault(p => p.Symbol == symbol);

            return symbolPosition?.UnrealizedPnL ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating unrealized PnL for bot {BotId}, symbol {Symbol}", botId, symbol);
            return 0;
        }
    }

    public async Task<decimal> CalculateRealizedPnLAsync(int botId, DateTime? since = null)
    {
        try
        {
            // Get all completed trades (orders with exit price)
            var query = _context.TradingBotOrders
                .Where(o => o.BotId == botId && o.ExitPrice != null);

            if (since.HasValue)
            {
                query = query.Where(o => o.UpdatedAt >= since.Value);
            }

            var completedOrders = await query.ToListAsync();

            if (completedOrders.Count == 0)
                return 0;

            // Calculate PnL for each completed trade
            decimal totalPnL = 0;

            foreach (var order in completedOrders)
            {
                var entryPrice = order.FilledPrice;
                var exitPrice = order.ExitPrice!.Value;
                var quantity = order.Quantity;

                decimal pnl;
                if (order.Side == "BUY")
                {
                    // BUY order: profit when exit price > entry price
                    pnl = (exitPrice - entryPrice) * quantity;
                }
                else
                {
                    // SELL order: profit when entry price > exit price
                    pnl = (entryPrice - exitPrice) * quantity;
                }

                // Subtract fees
                pnl -= order.Fee;

                totalPnL += pnl;
            }

            return totalPnL;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating realized PnL for bot {BotId}", botId);
            return 0;
        }
    }

    public async Task<decimal> GetTotalExposureAsync(int botId)
    {
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
            // Get all active bots for the user
            var botIds = await _context.TradingBots
                .Where(b => b.UserId == userId && b.Status == "Running")
                .Select(b => b.Id)
                .ToListAsync();

            if (botIds.Count == 0)
                return 0;

            // Sum exposure across all bots
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
}
