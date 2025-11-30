using CryptoTrading.Data;
using CryptoTrading.Interfaces.Bot;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Services.Trading;

namespace CryptoTrading.Services.Bot
{
    /// <summary>
    /// Wrapper around main TradingService for bot context
    /// </summary>
    public class BotTradingServiceWrapper : IBotTradingService
    {
        private readonly ITradingService _tradingService;
        private readonly int _userId;
        private readonly Guid _botId;
        private readonly ApplicationDbContext _db;

        public BotTradingServiceWrapper(
            ITradingService tradingService,
            ApplicationDbContext db,
            int userId,
            Guid botId)
        {
            _tradingService = tradingService;
            _db = db;
            _userId = userId;
            _botId = botId;
        }

        public async Task<ulong> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default)
        {
            var order = await _tradingService.PlaceOrderAsync(_userId, request);

            // Map order to bot for P&L/positions tracking
            _db.TradingBotOrders.Add(new Models.TradingBotOrder
            {
                TradingBotId = _botId,
                OrderId = ulong.Parse(order.Id),
                Intent = "Grid"
            });
            await _db.SaveChangesAsync(cancellationToken);

            return ulong.Parse(order.Id);
        }

        public async Task CancelOrderAsync(ulong orderId, CancellationToken cancellationToken = default)
        {
            await _tradingService.CancelOrderAsync(_userId, orderId);
        }

        public async Task<OrderDetailDto> GetOrderAsync(ulong orderId, CancellationToken cancellationToken = default)
        {
            return await _tradingService.GetOrderAsync(_userId, orderId);
        }

        public Task<decimal> EstimateRequiredCapitalAsync(
            string baseAsset, 
            string quoteAsset, 
            decimal quantity, 
            decimal? price = null, 
            CancellationToken cancellationToken = default)
        {
            // Estimate: quantity * (price ?? estimated_price) * 1.001 (for fees)
            var estimatedPrice = price ?? 50000m; // Fallback price
            return Task.FromResult(quantity * estimatedPrice * 1.001m);
        }

        public async Task<OrderBookDto> GetOrderBookAsync(string symbol, int depth = 20, CancellationToken cancellationToken = default)
        {
            return await _tradingService.GetOrderBookAsync(symbol, depth);
        }
    }
}

