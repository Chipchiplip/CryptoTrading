using CryptoTrading.Data;
using CryptoTrading.Models;
using CryptoTrading.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Services.Trading
{
    /// <summary>
    /// Implementation of trading service with order management, execution, and matching
    /// </summary>
    public class TradingService : ITradingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICoinGeckoService _coinGeckoService;
        private readonly ILogger<TradingService> _logger;
        private const decimal FEE_RATE = 0.001m; // 0.1% fee
        private const decimal MARKET_PRICE_BUFFER = 0.05m; // 5% buffer for market orders

        public TradingService(
            ApplicationDbContext context,
            ICoinGeckoService coinGeckoService,
            ILogger<TradingService> logger)
        {
            _context = context;
            _coinGeckoService = coinGeckoService;
            _logger = logger;
        }

        #region PlaceOrderAsync

        /// <summary>
        /// Places a new order with comprehensive validation, balance checks, and locking
        /// </summary>
        public async Task<OrderDto> PlaceOrderAsync(int userId, PlaceOrderRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate and parse symbol
                var (coinSymbol, quoteSymbol) = ParseSymbol(request.Symbol);

                // Find cryptocurrency
                var crypto = await _context.Cryptocurrencies
                    .FirstOrDefaultAsync(c => c.Symbol.ToUpper() == coinSymbol.ToUpper());

                if (crypto == null)
                {
                    throw new InvalidOperationException($"Cryptocurrency '{coinSymbol}' not found");
                }

                // Validate order type specific requirements
                if (request.Type.ToUpper() == "LIMIT" && request.Price == null)
                {
                    throw new InvalidOperationException("Price is required for LIMIT orders");
                }

                // Get current market price for validation and market orders
                var marketData = await _coinGeckoService.GetMarketDataAsync();
                var currentPrice = marketData
                    .FirstOrDefault(c => c.Symbol.Equals(coinSymbol, StringComparison.OrdinalIgnoreCase))
                    ?.CurrentPrice ?? 0m;

                if (currentPrice <= 0)
                {
                    throw new InvalidOperationException($"Unable to retrieve market price for {coinSymbol}");
                }

                // Determine order price with buffer for market orders
                decimal orderPrice;
                if (request.Type.ToUpper() == "MARKET")
                {
                    // Apply 5% buffer for market orders
                    orderPrice = request.Side.ToUpper() == "BUY"
                        ? currentPrice * (1 + MARKET_PRICE_BUFFER)
                        : currentPrice * (1 - MARKET_PRICE_BUFFER);
                }
                else
                {
                    orderPrice = request.Price!.Value;

                    // Validate limit price is reasonable (within 50% of market price)
                    if (Math.Abs(orderPrice - currentPrice) / currentPrice > 0.50m)
                    {
                        throw new InvalidOperationException(
                            $"Limit price ${orderPrice} is too far from market price ${currentPrice}");
                    }
                }

                // Calculate required balance
                decimal requiredAmount;
                Wallet wallet;

                if (request.Side.ToUpper() == "BUY")
                {
                    // For BUY orders, lock USD (quantity * price + estimated fee)
                    requiredAmount = request.Quantity * orderPrice * (1 + FEE_RATE);
                    wallet = await GetOrCreateWalletAsync(userId, "FIAT", "USD", null);
                }
                else // SELL
                {
                    // For SELL orders, lock cryptocurrency quantity
                    requiredAmount = request.Quantity;
                    wallet = await GetOrCreateWalletAsync(userId, "COIN", null, crypto.Id);
                }

                // Check available balance
                var availableBalance = await CalculateAvailableBalanceAsync(wallet.Id);
                if (availableBalance < requiredAmount)
                {
                    throw new InvalidOperationException(
                        $"Insufficient balance. Available: {availableBalance}, Required: {requiredAmount}");
                }

                // Create order
                var order = new Order
                {
                    UserId = userId,
                    CryptocurrencyId = crypto.Id,
                    Side = request.Side.ToUpper(),
                    Type = request.Type.ToUpper(),
                    PriceUsd = request.Type.ToUpper() == "LIMIT" ? request.Price : null,
                    QuantityCoin = request.Quantity,
                    FilledQty = 0,
                    Status = "NEW",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Lock balance
                await LockBalanceAsync(order.Id, wallet.Id, requiredAmount);

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Order {OrderId} placed: {Side} {Quantity} {Symbol} @ {Price}",
                    order.Id, order.Side, order.QuantityCoin, coinSymbol, orderPrice);

                // If market order, execute immediately
                if (request.Type.ToUpper() == "MARKET")
                {
                    await ExecuteMarketOrderAsync(order);
                }

                // Return DTO
                return MapToOrderDto(order, crypto.Symbol);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error placing order for user {UserId}", userId);
                throw;
            }
        }

        #endregion

        #region Balance Management

        /// <summary>
        /// Locks balance for an order
        /// </summary>
        private async Task LockBalanceAsync(ulong orderId, ulong walletId, decimal amount)
        {
            var orderHold = new OrderHold
            {
                OrderId = orderId,
                WalletId = walletId,
                Amount = amount,
                CreatedAt = DateTime.UtcNow,
                ReleasedAt = null
            };

            _context.OrderHolds.Add(orderHold);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Locked {Amount} for order {OrderId} in wallet {WalletId}",
                amount, orderId, walletId);
        }

        /// <summary>
        /// Releases locked balance for an order
        /// </summary>
        private async Task ReleaseBalanceAsync(ulong orderId, decimal? amountToRelease = null)
        {
            var holds = await _context.OrderHolds
                .Where(h => h.OrderId == orderId && h.ReleasedAt == null)
                .ToListAsync();

            foreach (var hold in holds)
            {
                if (amountToRelease.HasValue)
                {
                    // Partial release
                    if (hold.Amount > amountToRelease.Value)
                    {
                        // Create a new hold with reduced amount
                        var reducedHold = new OrderHold
                        {
                            OrderId = hold.OrderId,
                            WalletId = hold.WalletId,
                            Amount = hold.Amount - amountToRelease.Value,
                            CreatedAt = DateTime.UtcNow,
                            ReleasedAt = null
                        };
                        _context.OrderHolds.Add(reducedHold);
                    }
                }

                hold.ReleasedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogDebug("Released balance for order {OrderId}", orderId);
        }

        /// <summary>
        /// Gets or creates a wallet for a user
        /// </summary>
        private async Task<Wallet> GetOrCreateWalletAsync(
            int userId, string assetType, string? currencyCode, int? cryptoId)
        {
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w =>
                    w.UserId == userId &&
                    w.AssetType == assetType &&
                    w.CurrencyCode == currencyCode &&
                    w.CryptocurrencyId == cryptoId);

            if (wallet == null)
            {
                wallet = new Wallet
                {
                    UserId = userId,
                    AssetType = assetType,
                    CurrencyCode = currencyCode,
                    CryptocurrencyId = cryptoId
                };

                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created wallet {WalletId} for user {UserId}", wallet.Id, userId);
            }

            return wallet;
        }

        /// <summary>
        /// Calculates available balance considering movements and holds
        /// </summary>
        private async Task<decimal> CalculateAvailableBalanceAsync(ulong walletId)
        {
            // Get total balance from wallet movements
            var totalBalance = await _context.WalletMovements
                .Where(m => m.WalletId == walletId)
                .SumAsync(m => m.Amount);

            // Get locked balance from active order holds
            var lockedBalance = await _context.OrderHolds
                .Where(h => h.WalletId == walletId && h.ReleasedAt == null)
                .SumAsync(h => h.Amount);

            return totalBalance - lockedBalance;
        }

        #endregion

        #region ExecuteMarketOrderAsync

        /// <summary>
        /// Executes a market order immediately with internal matching and virtual counterparty
        /// </summary>
        public async Task ExecuteMarketOrderAsync(Order order)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Reload order with navigation properties
                order = await _context.Orders
                    .Include(o => o.Cryptocurrency)
                    .FirstOrDefaultAsync(o => o.Id == order.Id)
                    ?? throw new InvalidOperationException($"Order {order.Id} not found");

                // Get current market price
                var marketData = await _coinGeckoService.GetMarketDataAsync();
                var executionPrice = marketData
                    .FirstOrDefault(c => c.Symbol.Equals(order.Cryptocurrency.Symbol, StringComparison.OrdinalIgnoreCase))
                    ?.CurrentPrice ?? 0m;

                if (executionPrice <= 0)
                {
                    order.Status = "REJECTED";
                    order.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _logger.LogWarning("Market order {OrderId} rejected: no market price", order.Id);
                    return;
                }

                // Try internal matching first (match with opposite side limit orders)
                var remainingQty = order.QuantityCoin;
                var opposingSide = order.Side == "BUY" ? "SELL" : "BUY";

                var opposingOrders = await _context.Orders
                    .Where(o =>
                        o.CryptocurrencyId == order.CryptocurrencyId &&
                        o.Side == opposingSide &&
                        o.Type == "LIMIT" &&
                        (o.Status == "NEW" || o.Status == "PARTIAL"))
                    .OrderBy(o => order.Side == "BUY" ? o.PriceUsd : -o.PriceUsd) // Best price first
                    .ThenBy(o => o.CreatedAt) // FIFO
                    .ToListAsync();

                foreach (var opposingOrder in opposingOrders)
                {
                    if (remainingQty <= 0) break;

                    // Check if price is acceptable for market order
                    var opposingPrice = opposingOrder.PriceUsd ?? 0m;
                    if (order.Side == "BUY" && opposingPrice > executionPrice * 1.10m) continue;
                    if (order.Side == "SELL" && opposingPrice < executionPrice * 0.90m) continue;

                    var matchQty = Math.Min(remainingQty, opposingOrder.QuantityCoin - opposingOrder.FilledQty);
                    if (matchQty <= 0) continue;

                    // Execute the match at the limit order's price
                    await ExecuteTradeAsync(order, opposingOrder, matchQty, opposingPrice);
                    remainingQty -= matchQty;
                }

                // If still remaining quantity, match with virtual counterparty at market price
                if (remainingQty > 0)
                {
                    await ExecuteTradeWithVirtualCounterpartyAsync(order, remainingQty, executionPrice);
                }

                // Update order status
                order.UpdatedAt = DateTime.UtcNow;
                if (order.FilledQty >= order.QuantityCoin)
                {
                    order.Status = "FILLED";
                }
                else if (order.FilledQty > 0)
                {
                    order.Status = "PARTIAL";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Market order {OrderId} executed: {Filled}/{Total} @ avg ${AvgPrice}",
                    order.Id, order.FilledQty, order.QuantityCoin, executionPrice);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error executing market order {OrderId}", order.Id);
                throw;
            }
        }

        /// <summary>
        /// Executes a trade between two orders
        /// </summary>
        private async Task ExecuteTradeAsync(Order takerOrder, Order makerOrder, decimal quantity, decimal price)
        {
            // Update filled quantities
            takerOrder.FilledQty += quantity;
            makerOrder.FilledQty += quantity;

            // Update maker order status
            if (makerOrder.FilledQty >= makerOrder.QuantityCoin)
            {
                makerOrder.Status = "FILLED";
            }
            else if (makerOrder.FilledQty > 0)
            {
                makerOrder.Status = "PARTIAL";
            }
            makerOrder.UpdatedAt = DateTime.UtcNow;

            // Calculate fees
            var feeUsd = quantity * price * FEE_RATE;

            // Create trade records
            var takerTrade = new Trade
            {
                OrderId = takerOrder.Id,
                CryptocurrencyId = takerOrder.CryptocurrencyId,
                PriceUsd = price,
                QuantityCoin = quantity,
                FeeUsd = feeUsd,
                CreatedAt = DateTime.UtcNow
            };

            var makerTrade = new Trade
            {
                OrderId = makerOrder.Id,
                CryptocurrencyId = makerOrder.CryptocurrencyId,
                PriceUsd = price,
                QuantityCoin = quantity,
                FeeUsd = feeUsd,
                CreatedAt = DateTime.UtcNow
            };

            _context.Trades.Add(takerTrade);
            _context.Trades.Add(makerTrade);

            // Settle wallets
            await SettleTradeAsync(takerOrder.UserId, takerOrder.Side, quantity, price, feeUsd, takerOrder.CryptocurrencyId);
            await SettleTradeAsync(makerOrder.UserId, makerOrder.Side, quantity, price, feeUsd, makerOrder.CryptocurrencyId);

            // Release appropriate amounts from order holds
            if (takerOrder.Side == "BUY")
            {
                var usdUsed = quantity * price * (1 + FEE_RATE);
                await ReleaseBalanceAsync(takerOrder.Id, usdUsed);
            }
            else
            {
                await ReleaseBalanceAsync(takerOrder.Id, quantity);
            }

            if (makerOrder.Side == "BUY")
            {
                var usdUsed = quantity * price * (1 + FEE_RATE);
                await ReleaseBalanceAsync(makerOrder.Id, usdUsed);
            }
            else
            {
                await ReleaseBalanceAsync(makerOrder.Id, quantity);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Executes a trade with virtual counterparty (for market orders)
        /// </summary>
        private async Task ExecuteTradeWithVirtualCounterpartyAsync(Order order, decimal quantity, decimal price)
        {
            // Update filled quantity
            order.FilledQty += quantity;

            // Calculate fee
            var feeUsd = quantity * price * FEE_RATE;

            // Create trade record
            var trade = new Trade
            {
                OrderId = order.Id,
                CryptocurrencyId = order.CryptocurrencyId,
                PriceUsd = price,
                QuantityCoin = quantity,
                FeeUsd = feeUsd,
                CreatedAt = DateTime.UtcNow
            };

            _context.Trades.Add(trade);

            // Settle wallet
            await SettleTradeAsync(order.UserId, order.Side, quantity, price, feeUsd, order.CryptocurrencyId);

            // Release from order hold
            if (order.Side == "BUY")
            {
                var usdUsed = quantity * price * (1 + FEE_RATE);
                await ReleaseBalanceAsync(order.Id, usdUsed);
            }
            else
            {
                await ReleaseBalanceAsync(order.Id, quantity);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Settles wallet balances after a trade
        /// </summary>
        private async Task SettleTradeAsync(int userId, string side, decimal quantity, decimal price, decimal feeUsd, int cryptoId)
        {
            if (side == "BUY")
            {
                // Debit USD (cost + fee)
                var usdWallet = await GetOrCreateWalletAsync(userId, "FIAT", "USD", null);
                var costWithFee = quantity * price + feeUsd;

                _context.WalletMovements.Add(new WalletMovement
                {
                    WalletId = usdWallet.Id,
                    RefType = "TRADE",
                    RefId = null,
                    Amount = -costWithFee,
                    Note = $"BUY {quantity} crypto @ ${price}",
                    CreatedAt = DateTime.UtcNow
                });

                // Credit cryptocurrency
                var cryptoWallet = await GetOrCreateWalletAsync(userId, "COIN", null, cryptoId);

                _context.WalletMovements.Add(new WalletMovement
                {
                    WalletId = cryptoWallet.Id,
                    RefType = "TRADE",
                    RefId = null,
                    Amount = quantity,
                    Note = $"Received from BUY order",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else // SELL
            {
                // Debit cryptocurrency
                var cryptoWallet = await GetOrCreateWalletAsync(userId, "COIN", null, cryptoId);

                _context.WalletMovements.Add(new WalletMovement
                {
                    WalletId = cryptoWallet.Id,
                    RefType = "TRADE",
                    RefId = null,
                    Amount = -quantity,
                    Note = $"SELL {quantity} crypto @ ${price}",
                    CreatedAt = DateTime.UtcNow
                });

                // Credit USD (proceeds - fee)
                var usdWallet = await GetOrCreateWalletAsync(userId, "FIAT", "USD", null);
                var proceedsMinusFee = quantity * price - feeUsd;

                _context.WalletMovements.Add(new WalletMovement
                {
                    WalletId = usdWallet.Id,
                    RefType = "TRADE",
                    RefId = null,
                    Amount = proceedsMinusFee,
                    Note = $"Received from SELL order",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }

        #endregion

        #region MatchOrdersAsync

        /// <summary>
        /// Matches pending limit orders based on price-time priority
        /// </summary>
        public async Task<int> MatchOrdersAsync()
        {
            var matchCount = 0;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get all cryptocurrencies with open orders
                var cryptoIds = await _context.Orders
                    .Where(o => o.Type == "LIMIT" && (o.Status == "NEW" || o.Status == "PARTIAL"))
                    .Select(o => o.CryptocurrencyId)
                    .Distinct()
                    .ToListAsync();

                foreach (var cryptoId in cryptoIds)
                {
                    matchCount += await MatchOrdersForCryptocurrencyAsync(cryptoId);
                }

                await transaction.CommitAsync();

                if (matchCount > 0)
                {
                    _logger.LogInformation("Matched {Count} order pairs", matchCount);
                }

                return matchCount;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error matching orders");
                throw;
            }
        }

        /// <summary>
        /// Matches orders for a specific cryptocurrency
        /// </summary>
        private async Task<int> MatchOrdersForCryptocurrencyAsync(int cryptoId)
        {
            var matchCount = 0;

            // Get open buy orders (highest price first, then FIFO)
            var buyOrders = await _context.Orders
                .Where(o =>
                    o.CryptocurrencyId == cryptoId &&
                    o.Side == "BUY" &&
                    o.Type == "LIMIT" &&
                    (o.Status == "NEW" || o.Status == "PARTIAL"))
                .OrderByDescending(o => o.PriceUsd)
                .ThenBy(o => o.CreatedAt)
                .ToListAsync();

            // Get open sell orders (lowest price first, then FIFO)
            var sellOrders = await _context.Orders
                .Where(o =>
                    o.CryptocurrencyId == cryptoId &&
                    o.Side == "SELL" &&
                    o.Type == "LIMIT" &&
                    (o.Status == "NEW" || o.Status == "PARTIAL"))
                .OrderBy(o => o.PriceUsd)
                .ThenBy(o => o.CreatedAt)
                .ToListAsync();

            // Match orders where buy price >= sell price
            foreach (var buyOrder in buyOrders)
            {
                foreach (var sellOrder in sellOrders)
                {
                    if (buyOrder.PriceUsd >= sellOrder.PriceUsd)
                    {
                        var buyRemaining = buyOrder.QuantityCoin - buyOrder.FilledQty;
                        var sellRemaining = sellOrder.QuantityCoin - sellOrder.FilledQty;

                        if (buyRemaining > 0 && sellRemaining > 0)
                        {
                            var matchQty = Math.Min(buyRemaining, sellRemaining);

                            // Execute at the maker's price (earlier order)
                            var executionPrice = buyOrder.CreatedAt < sellOrder.CreatedAt
                                ? buyOrder.PriceUsd!.Value
                                : sellOrder.PriceUsd!.Value;

                            await ExecuteTradeAsync(buyOrder, sellOrder, matchQty, executionPrice);
                            matchCount++;
                        }
                    }
                }
            }

            return matchCount;
        }

        #endregion

        #region CancelOrderAsync

        /// <summary>
        /// Cancels an order with validation and balance release
        /// </summary>
        public async Task<OrderDto> CancelOrderAsync(int userId, ulong orderId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Cryptocurrency)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    throw new InvalidOperationException($"Order {orderId} not found");
                }

                // Verify ownership
                if (order.UserId != userId)
                {
                    throw new UnauthorizedAccessException("Not authorized to cancel this order");
                }

                // Validate status
                if (order.Status == "FILLED")
                {
                    throw new InvalidOperationException("Cannot cancel a filled order");
                }

                if (order.Status == "CANCELED")
                {
                    throw new InvalidOperationException("Order is already canceled");
                }

                // Release all locked balances
                await ReleaseBalanceAsync(orderId);

                // Update order status
                order.Status = "CANCELED";
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Order {OrderId} canceled by user {UserId}", orderId, userId);

                return MapToOrderDto(order, order.Cryptocurrency.Symbol);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error canceling order {OrderId}", orderId);
                throw;
            }
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets a single order with details
        /// </summary>
        public async Task<OrderDetailDto> GetOrderAsync(int userId, ulong orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Cryptocurrency)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                throw new InvalidOperationException($"Order {orderId} not found");
            }

            // Get trades for this order
            var trades = await _context.Trades
                .Where(t => t.OrderId == orderId)
                .Include(t => t.Cryptocurrency)
                .ToListAsync();

            var orderDto = MapToOrderDetailDto(order, order.Cryptocurrency.Symbol, trades);
            return orderDto;
        }

        /// <summary>
        /// Gets orders with filtering and pagination
        /// </summary>
        public async Task<PaginatedResponse<OrderDto>> GetOrdersAsync(int userId, OrdersQuery query)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.Cryptocurrency)
                .Where(o => o.UserId == userId);

            // Apply filters
            if (!string.IsNullOrEmpty(query.Symbol))
            {
                var coinSymbol = ParseSymbol(query.Symbol).CoinSymbol;
                ordersQuery = ordersQuery.Where(o => o.Cryptocurrency.Symbol.ToUpper() == coinSymbol.ToUpper());
            }

            if (!string.IsNullOrEmpty(query.Side))
            {
                ordersQuery = ordersQuery.Where(o => o.Side == query.Side.ToUpper());
            }

            if (!string.IsNullOrEmpty(query.Type))
            {
                ordersQuery = ordersQuery.Where(o => o.Type == query.Type.ToUpper());
            }

            if (!string.IsNullOrEmpty(query.Status))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == query.Status.ToUpper());
            }

            if (query.FromDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.CreatedAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.CreatedAt <= query.ToDate.Value);
            }

            // Get total count
            var totalItems = await ordersQuery.CountAsync();

            // Apply pagination
            var orders = await ordersQuery
                .OrderByDescending(o => o.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var orderDtos = orders.Select(o => MapToOrderDto(o, o.Cryptocurrency.Symbol)).ToList();

            return new PaginatedResponse<OrderDto>
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize),
                Data = orderDtos
            };
        }

        /// <summary>
        /// Gets trades with filtering and pagination
        /// </summary>
        public async Task<PaginatedResponse<TradeDto>> GetTradesAsync(int userId, TradesQuery query)
        {
            var tradesQuery = _context.Trades
                .Include(t => t.Order)
                .Include(t => t.Cryptocurrency)
                .Where(t => t.Order.UserId == userId);

            // Apply filters
            if (!string.IsNullOrEmpty(query.Symbol))
            {
                var coinSymbol = ParseSymbol(query.Symbol).CoinSymbol;
                tradesQuery = tradesQuery.Where(t => t.Cryptocurrency.Symbol.ToUpper() == coinSymbol.ToUpper());
            }

            if (!string.IsNullOrEmpty(query.OrderId) && ulong.TryParse(query.OrderId, out var orderId))
            {
                tradesQuery = tradesQuery.Where(t => t.OrderId == orderId);
            }

            if (query.FromDate.HasValue)
            {
                tradesQuery = tradesQuery.Where(t => t.CreatedAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                tradesQuery = tradesQuery.Where(t => t.CreatedAt <= query.ToDate.Value);
            }

            // Get total count
            var totalItems = await tradesQuery.CountAsync();

            // Apply pagination
            var trades = await tradesQuery
                .OrderByDescending(t => t.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var tradeDtos = trades.Select(t => MapToTradeDto(t, t.Cryptocurrency.Symbol)).ToList();

            return new PaginatedResponse<TradeDto>
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize),
                Data = tradeDtos
            };
        }

        /// <summary>
        /// Gets order book with aggregated bid/ask levels
        /// </summary>
        public async Task<OrderBookDto> GetOrderBookAsync(string symbol, int depth = 20)
        {
            var (coinSymbol, quoteSymbol) = ParseSymbol(symbol);

            // Find cryptocurrency
            var crypto = await _context.Cryptocurrencies
                .FirstOrDefaultAsync(c => c.Symbol.ToUpper() == coinSymbol.ToUpper());

            if (crypto == null)
            {
                throw new InvalidOperationException($"Cryptocurrency '{coinSymbol}' not found");
            }

            // Get current market price from CoinGecko
            var marketData = await _coinGeckoService.GetMarketDataAsync();
            var cryptoData = marketData.FirstOrDefault(c =>
                c.Symbol.Equals(coinSymbol, StringComparison.OrdinalIgnoreCase));

            var currentPrice = cryptoData?.CurrentPrice ?? 0m;
            var priceChange24h = cryptoData?.PriceChange24h ?? 0m;
            var priceChangePercentage24h = cryptoData?.PriceChangePercentage24h ?? 0m;

            // Get open sell orders (asks)
            var sellOrders = await _context.Orders
                .Where(o =>
                    o.CryptocurrencyId == crypto.Id &&
                    o.Side == "SELL" &&
                    o.Type == "LIMIT" &&
                    (o.Status == "NEW" || o.Status == "PARTIAL"))
                .GroupBy(o => o.PriceUsd)
                .Select(g => new OrderBookLevel
                {
                    Price = g.Key!.Value,
                    Quantity = g.Sum(o => o.QuantityCoin - o.FilledQty),
                    Total = g.Key!.Value * g.Sum(o => o.QuantityCoin - o.FilledQty),
                    OrderCount = g.Count()
                })
                .OrderBy(l => l.Price)
                .Take(depth)
                .ToListAsync();

            // Get open buy orders (bids)
            var buyOrders = await _context.Orders
                .Where(o =>
                    o.CryptocurrencyId == crypto.Id &&
                    o.Side == "BUY" &&
                    o.Type == "LIMIT" &&
                    (o.Status == "NEW" || o.Status == "PARTIAL"))
                .GroupBy(o => o.PriceUsd)
                .Select(g => new OrderBookLevel
                {
                    Price = g.Key!.Value,
                    Quantity = g.Sum(o => o.QuantityCoin - o.FilledQty),
                    Total = g.Key!.Value * g.Sum(o => o.QuantityCoin - o.FilledQty),
                    OrderCount = g.Count()
                })
                .OrderByDescending(l => l.Price)
                .Take(depth)
                .ToListAsync();

            return new OrderBookDto
            {
                Symbol = symbol,
                CurrentPrice = currentPrice,
                PriceChange24h = priceChange24h,
                PriceChangePercentage24h = priceChangePercentage24h,
                Asks = sellOrders,
                Bids = buyOrders,
                LastUpdated = DateTime.UtcNow
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Parses trading pair symbol (e.g., "BTC/USDT" -> ("BTC", "USDT"))
        /// </summary>
        private (string CoinSymbol, string QuoteSymbol) ParseSymbol(string symbol)
        {
            var parts = symbol.Split('/');
            if (parts.Length != 2)
            {
                throw new ArgumentException($"Invalid symbol format: {symbol}. Expected format: COIN/QUOTE");
            }

            return (parts[0].ToUpper(), parts[1].ToUpper());
        }

        /// <summary>
        /// Maps Order entity to OrderDto
        /// </summary>
        private OrderDto MapToOrderDto(Order order, string coinSymbol)
        {
            return new OrderDto
            {
                Id = order.Id.ToString(),
                Symbol = $"{coinSymbol.ToUpper()}/USDT",
                Side = order.Side,
                Type = order.Type,
                Quantity = order.QuantityCoin,
                Price = order.PriceUsd,
                Filled = order.FilledQty,
                Remaining = order.QuantityCoin - order.FilledQty,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt ?? order.CreatedAt
            };
        }

        /// <summary>
        /// Maps Order entity to OrderDetailDto with trades
        /// </summary>
        private OrderDetailDto MapToOrderDetailDto(Order order, string coinSymbol, List<Trade> trades)
        {
            var totalFees = trades.Sum(t => t.FeeUsd);
            var avgPrice = trades.Any()
                ? trades.Sum(t => t.PriceUsd * t.QuantityCoin) / trades.Sum(t => t.QuantityCoin)
                : (decimal?)null;

            return new OrderDetailDto
            {
                Id = order.Id.ToString(),
                Symbol = $"{coinSymbol.ToUpper()}/USDT",
                Side = order.Side,
                Type = order.Type,
                Quantity = order.QuantityCoin,
                Price = order.PriceUsd,
                Filled = order.FilledQty,
                Remaining = order.QuantityCoin - order.FilledQty,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt ?? order.CreatedAt,
                AvgPrice = avgPrice,
                TotalFees = totalFees,
                Trades = trades.Select(t => MapToTradeDto(t, coinSymbol)).ToList()
            };
        }

        /// <summary>
        /// Maps Trade entity to TradeDto
        /// </summary>
        private TradeDto MapToTradeDto(Trade trade, string coinSymbol)
        {
            return new TradeDto
            {
                Id = trade.Id.ToString(),
                OrderId = trade.OrderId.ToString(),
                Symbol = $"{coinSymbol.ToUpper()}/USDT",
                Price = trade.PriceUsd,
                Quantity = trade.QuantityCoin,
                Fee = trade.FeeUsd,
                CreatedAt = trade.CreatedAt
            };
        }

        #endregion
    }
}

