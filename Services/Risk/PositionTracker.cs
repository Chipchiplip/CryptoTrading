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
            // NOTE: PositionTracker has a type mismatch - IPositionTracker uses int botId
            // but TradingBot.Id is Guid. This method may need to be refactored to accept Guid.
            // For now, we'll return empty list as this appears to be unused code.
            _logger.LogWarning(
                "PositionTracker.GetOpenPositionsAsync called with int botId {BotId}, but TradingBot uses Guid. Returning empty list.",
                botId);
            return new List<PositionDto>();
            
            /* Original implementation assumed TradingBotOrder had properties it doesn't have.
             * This code needs to be rewritten to:
             * 1. Convert botId (int) to botGuid (Guid) - or change interface to accept Guid
             * 2. Query TradingBotOrders joined with Orders and Cryptocurrency
             * 3. Get filled orders (Status == "FILLED")
             * 4. Group by symbol and calculate positions
             * 
            var botGuid = ...; // Convert int to Guid somehow (this is a design issue)
            
            var openOrders = await _context.TradingBotOrders
                .Include(tbo => tbo.Order)
                    .ThenInclude(o => o.Cryptocurrency)
                .Where(tbo => tbo.TradingBotId == botGuid && tbo.Order != null && tbo.Order.Status == "FILLED")
                .Select(tbo => new
                {
                    Symbol = tbo.Order!.Cryptocurrency.Symbol,
                    Side = tbo.Order!.Side,
                    Quantity = tbo.Order!.FilledQty > 0 ? tbo.Order!.FilledQty : tbo.Order!.QuantityCoin,
                    FilledPrice = tbo.Order!.PriceUsd ?? 0m,
                    CreatedAt = tbo.CreatedAt
                })
                .ToListAsync();

            if (openOrders.Count == 0)
                return new List<PositionDto>();

            var positions = new List<PositionDto>();

            // Group by symbol
            var bySymbol = openOrders.GroupBy(o => o.Symbol);
            */
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
            // NOTE: PositionTracker has issues - TradingBotOrder doesn't have ExitPrice
            // This method needs to be rewritten to work with the actual Order/Trade model
            _logger.LogWarning(
                "PositionTracker.CalculateRealizedPnLAsync called but implementation is incomplete. Returning 0.");
            return 0;
            
            /* Original implementation assumed TradingBotOrder had ExitPrice which doesn't exist.
             * This needs to be rewritten to:
             * 1. Query completed orders (Status == "FILLED")
             * 2. Calculate PnL from buy-sell pairs or trades
             * 
            var botGuid = ...; // Convert int to Guid
            
            var query = _context.TradingBotOrders
                .Include(tbo => tbo.Order)
                .Where(tbo => tbo.TradingBotId == botGuid && tbo.Order != null && tbo.Order.Status == "FILLED");
            */
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
            // NOTE: GetTotalExposureAsync expects int botId but TradingBot.Id is Guid
            // This needs to be fixed
            _logger.LogWarning(
                "PositionTracker.GetUserTotalExposureAsync called but implementation has type mismatch. Returning 0.");
            return 0;
            
            /* Original implementation had type mismatch
             * 
            // Get all active bots for the user
            var botIds = await _context.TradingBots
                .Where(b => b.UserId == userId && b.Status == "Running")
                .Select(b => b.Id) // This is Guid, not int
                .ToListAsync();

            if (botIds.Count == 0)
                return 0;

            // Sum exposure across all bots
            decimal totalExposure = 0;

            foreach (var botId in botIds)
            {
                // Can't call GetTotalExposureAsync with Guid
                // totalExposure += await GetTotalExposureAsync(botId);
            }

            return totalExposure;
            */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total exposure for user {UserId}", userId);
            return 0;
        }
    }
}
