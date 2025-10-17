using CryptoTrading.Controllers;

namespace CryptoTrading.Services.Trading;

public interface ITradingService
{
    Task<IEnumerable<BalanceDto>> GetBalancesAsync();
    Task<OrderDto> PlaceOrderAsync(PlaceOrderDto dto);
    Task<OrderDto?> GetOrderAsync(Guid id);
    Task<IEnumerable<OrderDto>> GetOrdersAsync();
    Task<bool> CancelOrderAsync(Guid id);
    Task<IEnumerable<TradeDto>> GetTradesAsync();
    Task<TradingStatsDto> GetTradingStatsAsync();
}

// DTOs
public record TradeDto(Guid Id, string Symbol, string Side, decimal Quantity, decimal Price, decimal Fee, DateTime ExecutedAtUtc);
public record TradingStatsDto(decimal TotalVolume, decimal TotalFees, int TotalTrades, decimal WinRate);
