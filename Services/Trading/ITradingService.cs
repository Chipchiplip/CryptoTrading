using CryptoTrading.Models.DTOs;

namespace CryptoTrading.Services.Trading
{
    /// <summary>
    /// Service interface for trading operations including order management, execution, and matching
    /// </summary>
    public interface ITradingService
    {
        /// <summary>
        /// Places a new order with validation, balance checks, and locking
        /// </summary>
        /// <param name="userId">User ID placing the order</param>
        /// <param name="request">Order request details</param>
        /// <returns>Created order DTO</returns>
        Task<OrderDto> PlaceOrderAsync(int userId, PlaceOrderRequest request);

        /// <summary>
        /// Cancels an existing order and releases locked balances
        /// </summary>
        /// <param name="userId">User ID requesting cancellation</param>
        /// <param name="orderId">Order ID to cancel</param>
        /// <returns>Cancelled order DTO</returns>
        Task<OrderDto> CancelOrderAsync(int userId, ulong orderId);

        /// <summary>
        /// Retrieves a single order by ID
        /// </summary>
        /// <param name="userId">User ID requesting the order</param>
        /// <param name="orderId">Order ID to retrieve</param>
        /// <returns>Order details including trades</returns>
        Task<OrderDetailDto> GetOrderAsync(int userId, ulong orderId);

        /// <summary>
        /// Retrieves orders with filtering and pagination
        /// </summary>
        /// <param name="userId">User ID requesting orders</param>
        /// <param name="query">Filter and pagination parameters</param>
        /// <returns>Paginated list of orders</returns>
        Task<PaginatedResponse<OrderDto>> GetOrdersAsync(int userId, OrdersQuery query);

        /// <summary>
        /// Retrieves trade history with filtering and pagination
        /// </summary>
        /// <param name="userId">User ID requesting trades</param>
        /// <param name="query">Filter and pagination parameters</param>
        /// <returns>Paginated list of trades</returns>
        Task<PaginatedResponse<TradeDto>> GetTradesAsync(int userId, TradesQuery query);

        /// <summary>
        /// Retrieves order book with aggregated bid/ask levels
        /// </summary>
        /// <param name="symbol">Trading pair symbol (e.g., "BTC/USDT")</param>
        /// <param name="depth">Number of price levels to return (default 20)</param>
        /// <returns>Order book snapshot</returns>
        Task<OrderBookDto> GetOrderBookAsync(string symbol, int depth = 20);

        /// <summary>
        /// Executes a market order immediately using internal matching and virtual counterparty
        /// </summary>
        /// <param name="order">Market order to execute</param>
        /// <returns>Task representing the execution operation</returns>
        Task ExecuteMarketOrderAsync(Models.Order order);

        /// <summary>
        /// Matches pending limit orders based on price-time priority
        /// </summary>
        /// <returns>Number of matches made</returns>
        Task<int> MatchOrdersAsync();
    }
}

