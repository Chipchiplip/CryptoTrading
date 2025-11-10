using CryptoTrading.Data;
using CryptoTrading.Interfaces.Bot;
using Microsoft.EntityFrameworkCore;

namespace CryptoTrading.Services.Bot
{
    /// <summary>
    /// Portfolio service for bot context
    /// </summary>
    public class PortfolioService : IPortfolioService
    {
        private readonly ApplicationDbContext _context;

        public PortfolioService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetBalanceAsync(
            int userId, 
            string asset, 
            CancellationToken cancellationToken = default)
        {
            // Find wallet for the asset
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => 
                    w.UserId == userId && 
                    (w.CurrencyCode == asset || 
                     (w.Cryptocurrency != null && w.Cryptocurrency.Symbol == asset)),
                    cancellationToken);

            if (wallet == null)
                return 0m;

            // Get total balance from wallet movements
            var totalBalance = await _context.WalletMovements
                .Where(m => m.WalletId == wallet.Id)
                .SumAsync(m => m.Amount, cancellationToken);

            // Get locked balance from active order holds
            var lockedBalance = await _context.OrderHolds
                .Where(h => h.WalletId == wallet.Id && h.ReleasedAt == null)
                .SumAsync(h => h.Amount, cancellationToken);

            return totalBalance - lockedBalance;
        }

        public async Task<List<PositionInfo>> GetOpenPositionsAsync(
            Guid botId, 
            CancellationToken cancellationToken = default)
        {
            // Get bot orders that are filled or partially filled
            var botOrders = await _context.TradingBotOrders
                .Include(bo => bo.Order)
                    .ThenInclude(o => o!.Cryptocurrency)
                .Where(bo => bo.TradingBotId == botId && 
                            (bo.Order!.Status == "FILLED" || bo.Order.Status == "PARTIAL"))
                .ToListAsync(cancellationToken);

            // Group by cryptocurrency and calculate position
            var positions = botOrders
                .GroupBy(bo => bo.Order!.Cryptocurrency!.Symbol)
                .Select(g => new PositionInfo
                {
                    Asset = g.Key,
                    Quantity = g.Sum(bo => 
                        bo.Order!.Side == "BUY" 
                            ? bo.Order.FilledQty 
                            : -bo.Order.FilledQty),
                    AveragePrice = g.Average(bo => bo.Order!.PriceUsd ?? 0),
                    CurrentPrice = 0, // Will be filled by caller
                    UnrealizedPnl = 0 // Will be calculated by caller
                })
                .Where(p => p.Quantity != 0)
                .ToList();

            return positions;
        }
    }
}

