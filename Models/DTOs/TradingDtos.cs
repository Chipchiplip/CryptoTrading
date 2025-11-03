using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.Models.DTOs
{
    /// <summary>
    /// Request DTO for placing a new order
    /// </summary>
    public class PlaceOrderRequest
    {
        /// <summary>
        /// Trading pair symbol (e.g., "BTC/USDT")
        /// </summary>
        [Required]
        public string Symbol { get; set; } = null!;

        /// <summary>
        /// Order side: "BUY" or "SELL"
        /// </summary>
        [Required]
        [RegularExpression("^(BUY|SELL)$", ErrorMessage = "Side must be 'BUY' or 'SELL'")]
        public string Side { get; set; } = null!;

        /// <summary>
        /// Order type: "MARKET" or "LIMIT"
        /// </summary>
        [Required]
        [RegularExpression("^(MARKET|LIMIT)$", ErrorMessage = "Type must be 'MARKET' or 'LIMIT'")]
        public string Type { get; set; } = null!;

        /// <summary>
        /// Quantity of cryptocurrency to trade
        /// </summary>
        [Required]
        [Range(0.00000001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Limit price in USD (required for LIMIT orders, ignored for MARKET orders)
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal? Price { get; set; }
    }

    /// <summary>
    /// Response DTO for order information
    /// </summary>
    public class OrderDto
    {
        /// <summary>
        /// Unique order identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Trading pair symbol
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Order side: BUY or SELL
        /// </summary>
        public string Side { get; set; } = string.Empty;

        /// <summary>
        /// Order type: MARKET or LIMIT
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Original order quantity
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Order price (null for MARKET orders)
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Filled quantity
        /// </summary>
        public decimal Filled { get; set; }

        /// <summary>
        /// Remaining quantity
        /// </summary>
        public decimal Remaining { get; set; }

        /// <summary>
        /// Order status: NEW, PARTIAL, FILLED, CANCELED, REJECTED
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Order creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Detailed order information including trades
    /// </summary>
    public class OrderDetailDto : OrderDto
    {
        /// <summary>
        /// Average execution price
        /// </summary>
        public decimal? AvgPrice { get; set; }

        /// <summary>
        /// Total fees paid
        /// </summary>
        public decimal TotalFees { get; set; }

        /// <summary>
        /// List of trades for this order
        /// </summary>
        public List<TradeDto> Trades { get; set; } = new();
    }

    /// <summary>
    /// Trade execution information
    /// </summary>
    public class TradeDto
    {
        /// <summary>
        /// Unique trade identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Associated order ID
        /// </summary>
        public string OrderId { get; set; } = string.Empty;

        /// <summary>
        /// Trading pair symbol
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Execution price in USD
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Executed quantity
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Trading fee in USD
        /// </summary>
        public decimal Fee { get; set; }

        /// <summary>
        /// Trade execution timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Query parameters for filtering orders
    /// </summary>
    public class OrdersQuery
    {
        /// <summary>
        /// Filter by trading pair symbol (e.g., "BTC/USDT")
        /// </summary>
        public string? Symbol { get; set; }

        /// <summary>
        /// Filter by order side: BUY or SELL
        /// </summary>
        public string? Side { get; set; }

        /// <summary>
        /// Filter by order type: MARKET or LIMIT
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Filter by order status: NEW, PARTIAL, FILLED, CANCELED, REJECTED. Supports multiple values.
        /// </summary>
        public List<string>? Status { get; set; }

        /// <summary>
        /// Filter orders created after this date
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Filter orders created before this date
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Page number (1-based)
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        /// <summary>
        /// Number of items per page
        /// </summary>
        [Range(1, 100)]
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Query parameters for filtering trades
    /// </summary>
    public class TradesQuery
    {
        /// <summary>
        /// Filter by trading pair symbol
        /// </summary>
        public string? Symbol { get; set; }

        /// <summary>
        /// Filter by order ID
        /// </summary>
        public string? OrderId { get; set; }

        /// <summary>
        /// Filter trades created after this date
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Filter trades created before this date
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Page number (1-based)
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        /// <summary>
        /// Number of items per page
        /// </summary>
        [Range(1, 100)]
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Paginated response wrapper
    /// </summary>
    public class PaginatedResponse<T>
    {
        /// <summary>
        /// Current page number
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Data items for current page
        /// </summary>
        public List<T> Data { get; set; } = new();
    }

    /// <summary>
    /// Order book snapshot
    /// </summary>
    public class OrderBookDto
    {
        /// <summary>
        /// Trading pair symbol
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Current market price from CoinGecko
        /// </summary>
        public decimal CurrentPrice { get; set; }

        /// <summary>
        /// 24-hour price change
        /// </summary>
        public decimal PriceChange24h { get; set; }

        /// <summary>
        /// 24-hour price change percentage
        /// </summary>
        public decimal PriceChangePercentage24h { get; set; }

        /// <summary>
        /// Aggregated ask levels (sell orders)
        /// </summary>
        public List<OrderBookLevel> Asks { get; set; } = new();

        /// <summary>
        /// Aggregated bid levels (buy orders)
        /// </summary>
        public List<OrderBookLevel> Bids { get; set; } = new();

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Order book price level
    /// </summary>
    public class OrderBookLevel
    {
        /// <summary>
        /// Price level
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Total quantity at this price level
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Total value (Price * Quantity)
        /// </summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Number of orders at this level
        /// </summary>
        public int OrderCount { get; set; }
    }
}

