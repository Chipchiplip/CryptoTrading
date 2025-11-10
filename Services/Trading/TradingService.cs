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
        private readonly ICryptoCacheService _cacheService; // ✅ THÊM
        private readonly ILogger<TradingService> _logger;
        private const decimal FEE_RATE = 0.001m; // 0.1% fee
        private const decimal MARKET_PRICE_BUFFER = 0.05m; // 5% buffer for market orders
        
        // ✅ In-memory price cache với TTL khác nhau
        private readonly Dictionary<string, (decimal Price, DateTime LastUpdated)> _priceCache = new();
        private readonly TimeSpan _marketOrderCacheTTL = TimeSpan.FromSeconds(30); // MARKET: 30s
        private readonly TimeSpan _limitOrderCacheTTL = TimeSpan.FromMinutes(5);   // LIMIT: 5 phút

        public TradingService(
            ApplicationDbContext context,
            ICoinGeckoService coinGeckoService,
            ICryptoCacheService cacheService, // ✅ THÊM
            ILogger<TradingService> logger)
        {
            _context = context;
            _coinGeckoService = coinGeckoService;
            _cacheService = cacheService; // ✅ THÊM
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

                // ✅ SỬ DỤNG CACHE thay vì gọi API mỗi lần
                decimal currentPrice = 0m;
                
                if (request.Type.ToUpper() == "MARKET")
                {
                    // MARKET Order: Cần price tương đối fresh (< 30s)
                    currentPrice = await GetMarketPriceAsync(coinSymbol, _marketOrderCacheTTL);
                }
                else
                {
                    // LIMIT Order: Chỉ cần validate, có thể dùng cache lâu hơn
                    currentPrice = await GetMarketPriceAsync(coinSymbol, _limitOrderCacheTTL);
                }

                if (currentPrice <= 0)
                {
                    // Fallback: Try fresh fetch
                    _logger.LogWarning("Cache miss for {Symbol}, fetching from API", coinSymbol);
                    currentPrice = await GetMarketPriceAsync(coinSymbol, TimeSpan.Zero, forceRefresh: true);
                    
                    if (currentPrice <= 0)
                    {
                        throw new InvalidOperationException($"Unable to retrieve market price for {coinSymbol}");
                    }
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

                    // ✅ Relax validation cho LIMIT orders (không cần check market price quá chặt)
                    // Chỉ validate reasonable range
                    if (orderPrice < 0.01m || orderPrice > 1_000_000m)
                    {
                        throw new InvalidOperationException(
                            $"Limit price ${orderPrice} is out of reasonable range (0.01 - 1,000,000)");
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
                await _context.SaveChangesAsync(); // Save the lock within transaction

                // ✅ Execute order based on type
                if (request.Type.ToUpper() == "MARKET")
                {
                    // MARKET order: Execute immediately
                    await ExecuteMarketOrderInTransactionAsync(order, currentPrice, transaction);
                }
                else
                {
                    // ✅ LIMIT order: Try immediate matching
                    await TryImmediateMatchAsync(order, transaction);
                }

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Order {OrderId} placed: {Side} {Quantity} {Symbol} @ {Price}",
                    order.Id, order.Side, order.QuantityCoin, coinSymbol, orderPrice);

                // Return DTO with preserved quote symbol
                return MapToOrderDto(order, coinSymbol, quoteSymbol);
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
        /// Note: Does not call SaveChangesAsync - caller must save changes
        /// </summary>
        private Task LockBalanceAsync(ulong orderId, ulong walletId, decimal amount)
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
            // Don't save here - let caller manage SaveChanges within transaction

            _logger.LogDebug("Locked {Amount} for order {OrderId} in wallet {WalletId}",
                amount, orderId, walletId);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Releases locked balance for an order
        /// Note: Does not call SaveChangesAsync - caller must save changes
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

            // Don't save here - let caller manage SaveChanges within transaction

            _logger.LogDebug("Released balance for order {OrderId}", orderId);
        }

        /// <summary>
        /// Gets or creates a wallet for a user
        /// If creating a new wallet, saves it immediately to get the ID (required for foreign keys)
        /// This is safe because it's called within a transaction
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
                // Must save immediately to get the ID for foreign key references
                // This is safe because we're within a transaction - if transaction rolls back, wallet is also rolled back
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
        /// Uses its own transaction - for standalone execution
        /// </summary>
        /// <param name="order">The order to execute</param>
        /// <param name="cachedPrice">Optional cached price to avoid refetching market data</param>
        public async Task ExecuteMarketOrderAsync(Order order, decimal? cachedPrice = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Reload order with navigation properties
                order = await _context.Orders
                    .Include(o => o.Cryptocurrency)
                    .FirstOrDefaultAsync(o => o.Id == order.Id)
                    ?? throw new InvalidOperationException($"Order {order.Id} not found");

                // ✅ Use cached price if available, otherwise fetch from cache/API
                decimal executionPrice;
                if (cachedPrice.HasValue && cachedPrice.Value > 0)
                {
                    executionPrice = cachedPrice.Value;
                    _logger.LogDebug("Using cached market price {Price} for order {OrderId}", executionPrice, order.Id);
                }
                else
                {
                    // Try to get from cache first
                    executionPrice = await GetMarketPriceAsync(
                        order.Cryptocurrency.Symbol, 
                        _marketOrderCacheTTL);
                    
                    if (executionPrice <= 0)
                    {
                        // Fallback to API only if cache miss
                        _logger.LogWarning("Cache miss for market order execution, fetching from API");
                        executionPrice = await GetMarketPriceAsync(
                            order.Cryptocurrency.Symbol, 
                            TimeSpan.Zero, 
                            forceRefresh: true);
                    }
                    
                    _logger.LogDebug("Using market price {Price} for order {OrderId} (from cache/API)", executionPrice, order.Id);
                }

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
                    // ✅ FIX: Release any remaining locked balance when order is fully filled
                    // This handles cases where locked amount differs from actual fill amount (price difference)
                    await ReleaseBalanceAsync(order.Id); // Release all remaining holds
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
        /// Executes a market order within an existing transaction
        /// Used when placing a market order to keep everything in one transaction
        /// </summary>
        /// <param name="order">The order to execute</param>
        /// <param name="cachedPrice">Cached price to use for execution</param>
        /// <param name="transaction">Existing transaction to use</param>
        private async Task ExecuteMarketOrderInTransactionAsync(Order order, decimal? cachedPrice, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
        {
            try
            {
                // Reload order with navigation properties
                order = await _context.Orders
                    .Include(o => o.Cryptocurrency)
                    .FirstOrDefaultAsync(o => o.Id == order.Id)
                    ?? throw new InvalidOperationException($"Order {order.Id} not found");

                // ✅ Use cached price or fetch from cache
                decimal executionPrice = cachedPrice ?? 0m;
                if (executionPrice <= 0)
                {
                    // Try to get from cache first
                    executionPrice = await GetMarketPriceAsync(
                        order.Cryptocurrency.Symbol, 
                        _marketOrderCacheTTL);
                    
                    if (executionPrice <= 0)
                    {
                        // Fallback to API only if cache miss
                        _logger.LogWarning("Cache miss for market order execution, fetching from API");
                        executionPrice = await GetMarketPriceAsync(
                            order.Cryptocurrency.Symbol, 
                            TimeSpan.Zero, 
                            forceRefresh: true);
                    }
                }

                if (executionPrice <= 0)
                {
                    order.Status = "REJECTED";
                    order.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
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
                    // ✅ FIX: Release any remaining locked balance when order is fully filled
                    // This handles cases where locked amount differs from actual fill amount (price difference)
                    await ReleaseBalanceAsync(order.Id); // Release all remaining holds
                }
                else if (order.FilledQty > 0)
                {
                    order.Status = "PARTIAL";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Market order {OrderId} executed: {Filled}/{Total} @ avg ${AvgPrice}",
                    order.Id, order.FilledQty, order.QuantityCoin, executionPrice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing market order {OrderId} in transaction", order.Id);
                throw; // Let the outer transaction handle rollback
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

            // ✅ FIX: Release any remaining locked balance when orders are fully filled
            // This handles cases where locked amount differs from actual fill amount (price difference)
            // Check based on FilledQty, not Status, as Status may not be updated yet for takerOrder
            if (takerOrder.FilledQty >= takerOrder.QuantityCoin)
            {
                await ReleaseBalanceAsync(takerOrder.Id); // Release all remaining holds
            }

            if (makerOrder.FilledQty >= makerOrder.QuantityCoin)
            {
                await ReleaseBalanceAsync(makerOrder.Id); // Release all remaining holds
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

            // ✅ FIX: Release any remaining locked balance when order is fully filled
            // This handles cases where locked amount differs from actual fill amount (price difference)
            if (order.FilledQty >= order.QuantityCoin)
            {
                await ReleaseBalanceAsync(order.Id); // Release all remaining holds
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Settles wallet balances after a trade
        /// Note: Does not call SaveChangesAsync - caller must save changes
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

            // Don't save here - let caller manage SaveChanges within transaction
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

            // ✅ Single optimized query
            var buyOrders = await _context.Orders
                .Where(o =>
                    o.CryptocurrencyId == cryptoId &&
                    o.Side == "BUY" &&
                    o.Type == "LIMIT" &&
                    (o.Status == "NEW" || o.Status == "PARTIAL"))
                .OrderByDescending(o => o.PriceUsd)
                .ThenBy(o => o.CreatedAt)
                .ToListAsync();

            var sellOrders = await _context.Orders
                .Where(o =>
                    o.CryptocurrencyId == cryptoId &&
                    o.Side == "SELL" &&
                    o.Type == "LIMIT" &&
                    (o.Status == "NEW" || o.Status == "PARTIAL"))
                .OrderBy(o => o.PriceUsd)
                .ThenBy(o => o.CreatedAt)
                .ToListAsync();

            // ✅ TỐI ƯU: O(n log n) thay vì O(n²) - Two pointers approach
            int buyIndex = 0;
            int sellIndex = 0;
            
            while (buyIndex < buyOrders.Count && sellIndex < sellOrders.Count)
            {
                var buyOrder = buyOrders[buyIndex];
                var sellOrder = sellOrders[sellIndex];
                
                // Check if prices match
                if (buyOrder.PriceUsd < sellOrder.PriceUsd)
                {
                    // No more matches possible
                    break;
                }
                
                var buyRemaining = buyOrder.QuantityCoin - buyOrder.FilledQty;
                var sellRemaining = sellOrder.QuantityCoin - sellOrder.FilledQty;
                
                if (buyRemaining > 0 && sellRemaining > 0)
                {
                    var matchQty = Math.Min(buyRemaining, sellRemaining);
                    
                    // Execute at maker's price (earlier order)
                    var executionPrice = buyOrder.CreatedAt < sellOrder.CreatedAt
                        ? buyOrder.PriceUsd!.Value
                        : sellOrder.PriceUsd!.Value;
                    
                    await ExecuteTradeAsync(buyOrder, sellOrder, matchQty, executionPrice);
                    matchCount++;
                    
                    // Move pointers based on fill status
                    if (buyOrder.FilledQty >= buyOrder.QuantityCoin)
                    {
                        buyIndex++;
                    }
                    if (sellOrder.FilledQty >= sellOrder.QuantityCoin)
                    {
                        sellIndex++;
                    }
                }
                else
                {
                    // Move to next order
                    if (buyRemaining <= 0) buyIndex++;
                    if (sellRemaining <= 0) sellIndex++;
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
                    throw new KeyNotFoundException($"Order {orderId} not found");
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

                // Default to USD for canceled orders since we don't store quote symbol
                return MapToOrderDto(order, order.Cryptocurrency.Symbol, "USD");
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
                throw new KeyNotFoundException($"Order {orderId} not found");
            }

            // Get trades for this order
            var trades = await _context.Trades
                .Where(t => t.OrderId == orderId)
                .Include(t => t.Cryptocurrency)
                .ToListAsync();

            // Default to USD for order details since we don't store quote symbol
            var orderDto = MapToOrderDetailDto(order, order.Cryptocurrency.Symbol, trades, "USD");
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

            if (query.Status != null && query.Status.Any())
            {
                var upperStatuses = query.Status.Select(s => s.ToUpper()).ToList();
                ordersQuery = ordersQuery.Where(o => upperStatuses.Contains(o.Status));
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

            // Default to USD for order list since we don't store quote symbol
            var orderDtos = orders.Select(o => MapToOrderDto(o, o.Cryptocurrency.Symbol, "USD")).ToList();

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

            // Default to USD for trade list since we don't store quote symbol
            var tradeDtos = trades.Select(t => MapToTradeDto(t, t.Cryptocurrency.Symbol, "USD")).ToList();

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

            // ✅ SỬ DỤNG CACHE thay vì gọi API
            decimal currentPrice = 0m;
            decimal priceChange24h = 0m;
            decimal priceChangePercentage24h = 0m;
            
            // Try cache first
            if (_cacheService.TryGetCryptoData(out var marketData) && marketData != null)
            {
                var coin = marketData.FirstOrDefault(c => 
                    c.Symbol.Equals(coinSymbol, StringComparison.OrdinalIgnoreCase));
                if (coin != null && coin.CurrentPrice > 0)
                {
                    currentPrice = coin.CurrentPrice ?? 0m; // Handle nullable
                    priceChange24h = coin.PriceChange24h ?? 0m;
                    priceChangePercentage24h = coin.PriceChangePercentage24h ?? 0m;
                    _logger.LogDebug("Using cached data for order book: {Symbol}", coinSymbol);
                }
            }
            
            // If cache miss, still proceed (order book can work without current price)
            if (currentPrice <= 0)
            {
                _logger.LogDebug("Cache miss for order book, will use default price");
                // Order book will still show orders even without current market price
            }

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
        /// Smart price fetching with multi-layer cache strategy
        /// </summary>
        private async Task<decimal> GetMarketPriceAsync(
            string symbol, 
            TimeSpan cacheTTL, 
            bool forceRefresh = false)
        {
            // ✅ LAYER 1: Check in-memory cache first
            if (!forceRefresh && _priceCache.TryGetValue(symbol, out var cached))
            {
                var age = DateTime.UtcNow - cached.LastUpdated;
                if (age < cacheTTL)
                {
                    _logger.LogDebug("Using in-memory cache for {Symbol}: {Price} (age: {Age}s)", 
                        symbol, cached.Price, age.TotalSeconds);
                    return cached.Price;
                }
            }
            
            // ✅ LAYER 2: Check service cache (from RealtimeBroadcastService)
            if (!forceRefresh && _cacheService.TryGetCryptoData(out var cachedMarketData) && cachedMarketData != null)
            {
                var coin = cachedMarketData.FirstOrDefault(c => 
                    c.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
                if (coin != null && coin.CurrentPrice.HasValue && coin.CurrentPrice.Value > 0)
                {
                    var price = coin.CurrentPrice.Value;
                    // Update in-memory cache
                    _priceCache[symbol] = (price, DateTime.UtcNow);
                    _logger.LogDebug("Using service cache for {Symbol}: {Price}", symbol, price);
                    return price;
                }
            }
            
            // ✅ LAYER 3: Only fetch from API when needed
            if (forceRefresh || cacheTTL == TimeSpan.Zero)
            {
                _logger.LogWarning("Cache miss for {Symbol}, fetching from API", symbol);
                var freshMarketData = await _coinGeckoService.GetMarketDataAsync();
                var coin = freshMarketData.FirstOrDefault(c => 
                    c.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
                var price = coin?.CurrentPrice ?? 0m;
                
                if (price > 0)
                {
                    _priceCache[symbol] = (price, DateTime.UtcNow);
                }
                
                return price;
            }
            
            // Return cached price even if slightly stale
            if (_priceCache.TryGetValue(symbol, out var stale))
            {
                _logger.LogDebug("Using slightly stale cache for {Symbol}: {Price}", symbol, stale.Price);
                return stale.Price;
            }
            
            return 0m;
        }

        /// <summary>
        /// Immediate matching for LIMIT orders when placed
        /// </summary>
        private async Task TryImmediateMatchAsync(Order newOrder, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
        {
            var opposingSide = newOrder.Side == "BUY" ? "SELL" : "BUY";
            var remainingQty = newOrder.QuantityCoin;
            
            // Get best matching orders (single query, sorted)
            var opposingOrders = await _context.Orders
                .Where(o =>
                    o.CryptocurrencyId == newOrder.CryptocurrencyId &&
                    o.Side == opposingSide &&
                    o.Type == "LIMIT" &&
                    (o.Status == "NEW" || o.Status == "PARTIAL") &&
                    (newOrder.Side == "BUY" 
                        ? o.PriceUsd <= newOrder.PriceUsd  // BUY matches with SELL <= buy price
                        : o.PriceUsd >= newOrder.PriceUsd)) // SELL matches with BUY >= sell price
                .OrderBy(o => newOrder.Side == "BUY" ? o.PriceUsd : -o.PriceUsd) // Best price first
                .ThenBy(o => o.CreatedAt) // FIFO
                .Take(10) // Limit to top 10
                .ToListAsync();
            
            foreach (var opposingOrder in opposingOrders)
            {
                if (remainingQty <= 0) break;
                
                var buyRemaining = newOrder.Side == "BUY" 
                    ? newOrder.QuantityCoin - newOrder.FilledQty
                    : opposingOrder.QuantityCoin - opposingOrder.FilledQty;
                var sellRemaining = newOrder.Side == "SELL"
                    ? newOrder.QuantityCoin - newOrder.FilledQty
                    : opposingOrder.QuantityCoin - opposingOrder.FilledQty;
                
                if (buyRemaining > 0 && sellRemaining > 0)
                {
                    var matchQty = Math.Min(buyRemaining, sellRemaining);
                    var executionPrice = opposingOrder.PriceUsd!.Value; // Maker's price
                    
                    await ExecuteTradeAsync(newOrder, opposingOrder, matchQty, executionPrice);
                    remainingQty -= matchQty;
                }
            }
        }

        /// <summary>
        /// Parses trading pair symbol (e.g., "BTC/USDT" -> ("BTC", "USDT"))
        /// Handles edge cases: whitespace, lowercase, missing delimiter
        /// </summary>
        private (string CoinSymbol, string QuoteSymbol) ParseSymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Symbol cannot be empty");
            }

            // Trim whitespace and convert to uppercase
            symbol = symbol.Trim().ToUpper();

            // Try to split by common delimiters
            string[] parts;
            if (symbol.Contains('/'))
            {
                parts = symbol.Split('/');
            }
            else if (symbol.Contains('-'))
            {
                parts = symbol.Split('-');
            }
            else
            {
                // Try to parse common patterns like BTCUSDT, BTCUSD, ETHUSDT
                // Common quote currencies to check
                var quotes = new[] { "USDT", "USD", "USDC", "EUR", "GBP", "BTC", "ETH" };
                foreach (var quote in quotes)
                {
                    if (symbol.EndsWith(quote) && symbol.Length > quote.Length)
                    {
                        var coin = symbol.Substring(0, symbol.Length - quote.Length);
                        return (coin.Trim().ToUpper(), quote.ToUpper());
                    }
                }
                
                throw new ArgumentException($"Invalid symbol format: {symbol}. Expected format: COIN/QUOTE, COIN-QUOTE, or COINQUOTE");
            }

            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                throw new ArgumentException($"Invalid symbol format: {symbol}. Expected format: COIN/QUOTE or COIN-QUOTE");
            }

            return (parts[0].Trim().ToUpper(), parts[1].Trim().ToUpper());
        }

        /// <summary>
        /// Maps Order entity to OrderDto
        /// </summary>
        private OrderDto MapToOrderDto(Order order, string coinSymbol, string quoteSymbol = "USD")
        {
            return new OrderDto
            {
                Id = order.Id.ToString(),
                Symbol = $"{coinSymbol.ToUpper()}/{quoteSymbol.ToUpper()}",
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
        private OrderDetailDto MapToOrderDetailDto(Order order, string coinSymbol, List<Trade> trades, string quoteSymbol = "USD")
        {
            var totalFees = trades.Sum(t => t.FeeUsd);
            var avgPrice = trades.Any()
                ? trades.Sum(t => t.PriceUsd * t.QuantityCoin) / trades.Sum(t => t.QuantityCoin)
                : (decimal?)null;

            return new OrderDetailDto
            {
                Id = order.Id.ToString(),
                Symbol = $"{coinSymbol.ToUpper()}/{quoteSymbol.ToUpper()}",
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
                Trades = trades.Select(t => MapToTradeDto(t, coinSymbol, quoteSymbol)).ToList()
            };
        }

        /// <summary>
        /// Maps Trade entity to TradeDto
        /// </summary>
        private TradeDto MapToTradeDto(Trade trade, string coinSymbol, string quoteSymbol = "USD")
        {
            return new TradeDto
            {
                Id = trade.Id.ToString(),
                OrderId = trade.OrderId.ToString(),
                Symbol = $"{coinSymbol.ToUpper()}/{quoteSymbol.ToUpper()}",
                Price = trade.PriceUsd,
                Quantity = trade.QuantityCoin,
                Fee = trade.FeeUsd,
                CreatedAt = trade.CreatedAt
            };
        }

        #endregion
    }
}

