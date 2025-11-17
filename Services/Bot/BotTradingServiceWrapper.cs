using System;
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

        public BotTradingServiceWrapper(
            ITradingService tradingService,
            int userId,
            Guid botId)
        {
            _tradingService = tradingService;
            _userId = userId;
            _botId = botId;
        }

        public async Task<ulong> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default)
        {
            var order = await _tradingService.PlaceOrderAsync(_userId, request);
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
            // DEMO FIX: Remove hard-coded $50k fallback - require price to be provided
            // Hard-coded fallback causes wildly wrong estimates (e.g., 100,000x error for $0.50 coins)
            if (price == null || price <= 0)
            {
                throw new ArgumentException("Price is required for capital estimation and must be greater than 0");
            }
            
            // Estimate: quantity * price * 1.001 (for fees)
            return Task.FromResult(quantity * price.Value * 1.001m);
        }
    }
}

