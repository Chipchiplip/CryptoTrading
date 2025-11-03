# 📋 DANH SÁCH TASK - CORE TRADING SIMULATION
**Thành viên:** Vũ Hoàng  
**Branch:** `feature/trading-system`  
**Priority:** ⭐ P2 (Depends on Auth + Market Data)

---

## 🎯 MỤC TIÊU TỔNG QUAN

Xây dựng hệ thống mô phỏng giao dịch nội bộ hoàn chỉnh, cho phép:
- Đặt lệnh mua/bán (Market/Limit) theo giá CoinGecko real-time
- Matching orderbook nội bộ (users có thể khớp lệnh với nhau)
- Quản lý số dư (balance) tự động từ các lệnh giả lập
- Lưu trữ và hiển thị lịch sử lệnh, trades
- Kiểm soát trạng thái lệnh (NEW, PARTIAL, FILLED, CANCELED, REJECTED)
- **QUAN TRỌNG:** Tất cả logic chạy trên hệ thống, không gửi lệnh ra ngoài

---

## 📦 PHASE 1: BACKEND - TRADING SERVICE CORE

### ✅ Task 1.1: Tạo TradingService Interface & Implementation
**File:** `Services/Trading/ITradingService.cs`, `Services/Trading/TradingService.cs`

**Yêu cầu:**
- [ ] Tạo folder `Services/Trading/`
- [ ] Tạo interface `ITradingService` với các methods:
  ```csharp
  Task<OrderDto> PlaceOrderAsync(int userId, PlaceOrderRequest request);
  Task<bool> CancelOrderAsync(int userId, ulong orderId);
  Task<OrderDetailDto?> GetOrderAsync(int userId, ulong orderId);
  Task<List<OrderDto>> GetOrdersAsync(int userId, OrdersQuery? query = null);
  Task<List<TradeDto>> GetTradesAsync(int userId, TradesQuery? query = null);
  Task<OrderBookDto> GetOrderBookAsync(string symbol);
  Task<OrderDto> ExecuteMarketOrderAsync(Order order);
  Task MatchOrdersAsync();
  ```

**Dependencies:**
- `ICoinGeckoService` - Lấy giá real-time từ cache
- `ApplicationDbContext` - Database access
- `ICurrentUser` - User context (hoặc pass userId trực tiếp)

**Notes:**
- Service sẽ được inject vào `TradingController`
- Tất cả operations cần transaction để đảm bảo consistency

---

### ✅ Task 1.2: Order Placement Logic
**File:** `Services/Trading/TradingService.cs` - Method `PlaceOrderAsync()`

**Yêu cầu:**
- [ ] Validate input:
  - Symbol hợp lệ (phải có trong Cryptocurrencies table)
  - Side: BUY hoặc SELL (case-insensitive)
  - Type: MARKET hoặc LIMIT (case-insensitive)
  - Quantity > 0
  - Price > 0 (nếu LIMIT), Price = null (nếu MARKET)

- [ ] Lấy giá hiện tại cho MARKET orders:
  - Gọi `ICoinGeckoService.GetMarketDataAsync()` 
  - Tìm crypto theo Symbol
  - Lấy `CurrentPrice` để estimate lock amount

- [ ] Kiểm tra số dư đủ:
  - **BUY order:**
    - MARKET: Lock `Quantity * CurrentPrice * 1.05` (buffer 5%)
    - LIMIT: Lock `Quantity * Price` (chính xác)
    - Kiểm tra USD wallet có đủ tiền
  - **SELL order:**
    - Lock `Quantity` coin
    - Kiểm tra Crypto wallet có đủ coin

- [ ] Tạo Order record:
  ```csharp
  new Order {
      UserId = userId,
      CryptocurrencyId = crypto.Id,
      Side = side,
      Type = type,
      PriceUsd = price,
      QuantityCoin = quantity,
      FilledQty = 0,
      Status = "NEW"
  }
  ```

- [ ] Lock balance (Task 1.3):
  - Gọi `LockBalanceAsync()` để tạo OrderHold
  - Lưu WalletMovement với RefType = "ORDER_HOLD"

- [ ] Return OrderDto

**Business Rules:**
- Lock toàn bộ số tiền cần thiết ngay khi đặt lệnh
- MARKET order: Lock với buffer 5% để tránh slippage
- LIMIT order: Lock chính xác theo Price * Quantity
- Sử dụng database transaction

---

### ✅ Task 1.3: Balance Management (Wallet Lock/Unlock)
**File:** `Services/Trading/TradingService.cs` - Helper methods

**Yêu cầu:**
- [ ] Method `LockBalanceAsync(int userId, string assetType, string currencyCode, decimal amount, ulong orderId)`:
  ```csharp
  // 1. Get or create wallet
  var wallet = await GetOrCreateWalletAsync(userId, assetType, currencyCode, cryptoId);
  
  // 2. Check available balance
  var available = await CalculateAvailableBalance(wallet.Id);
  if (available < amount) throw new InsufficientBalanceException();
  
  // 3. Create OrderHold
  var hold = new OrderHold {
      OrderId = orderId,
      WalletId = wallet.Id,
      Amount = amount
  };
  
  // 4. Create WalletMovement (negative = lock)
  var movement = new WalletMovement {
      WalletId = wallet.Id,
      RefType = "ORDER_HOLD",
      RefId = orderId,
      Amount = -amount  // Negative = debit/lock
  };
  ```

- [ ] Method `ReleaseBalanceAsync(ulong orderId, decimal? amount = null)`:
  ```csharp
  // 1. Find OrderHold
  var hold = await _db.OrderHolds.FirstOrDefaultAsync(h => h.OrderId == orderId);
  if (hold == null || hold.ReleasedAt != null) return;
  
  // 2. Calculate release amount (partial or full)
  var releaseAmount = amount ?? hold.Amount;
  
  // 3. Mark OrderHold as released
  hold.ReleasedAt = DateTime.UtcNow;
  
  // 4. Create WalletMovement (positive = unlock)
  var movement = new WalletMovement {
      WalletId = hold.WalletId,
      RefType = "ORDER_RELEASE",
      RefId = orderId,
      Amount = releaseAmount  // Positive = credit/unlock
  };
  ```

- [ ] Method `GetOrCreateWalletAsync(int userId, string assetType, string currencyCode, int? cryptoId = null)`:
  ```csharp
  // Check existing wallet
  var wallet = await _db.Wallets.FirstOrDefaultAsync(w => 
      w.UserId == userId && 
      w.AssetType == assetType && 
      (assetType == "FIAT" ? w.CurrencyCode == currencyCode : w.CryptocurrencyId == cryptoId)
  );
  
  // Create if not exists
  if (wallet == null) {
      wallet = new Wallet {
          UserId = userId,
          AssetType = assetType,
          CurrencyCode = assetType == "FIAT" ? currencyCode : null,
          CryptocurrencyId = assetType == "COIN" ? cryptoId : null
      };
      _db.Wallets.Add(wallet);
      await _db.SaveChangesAsync();
  }
  
  return wallet;
  ```

- [ ] Method `CalculateAvailableBalance(ulong walletId)`:
  ```csharp
  // Sum all WalletMovements
  var totalBalance = await _db.WalletMovements
      .Where(m => m.WalletId == walletId)
      .SumAsync(m => m.Amount);
  
  // Subtract locked amount (OrderHolds not released)
  var locked = await _db.OrderHolds
      .Where(h => h.WalletId == walletId && h.ReleasedAt == null)
      .SumAsync(h => h.Amount);
  
  return totalBalance - locked;  // Available = Total - Locked
  ```

**Notes:**
- Tất cả operations trong transaction
- Handle concurrency với database locks nếu cần
- `Amount` trong WalletMovement: Positive = credit, Negative = debit

---

### ✅ Task 1.4: Market Order Execution
**File:** `Services/Trading/TradingService.cs` - Method `ExecuteMarketOrderAsync()`

**Yêu cầu:**
- [ ] Lấy giá hiện tại từ CoinGecko:
  ```csharp
  var marketData = await _coinGeckoService.GetMarketDataAsync();
  var crypto = marketData.FirstOrDefault(c => c.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
  var currentPrice = crypto?.CurrentPrice ?? throw new InvalidOperationException("Price not available");
  ```

- [ ] **BUY Market Order:**
  - Tìm SELL orders trong orderbook nội bộ:
    ```csharp
    var sellOrders = await _db.Orders
        .Where(o => o.CryptocurrencyId == order.CryptocurrencyId &&
                   o.Side == "SELL" &&
                   (o.Status == "NEW" || o.Status == "PARTIAL"))
        .OrderBy(o => o.PriceUsd ?? currentPrice)  // Best price first
        .ThenBy(o => o.CreatedAt)  // FIFO
        .ToListAsync();
    ```
  
  - Match theo giá tốt nhất:
    - Loop qua sellOrders
    - Match từng phần cho đến hết quantity hoặc hết orders
    - Tạo Trade record cho mỗi fill
    - Update Order.FilledQty và Status
  
  - Nếu không đủ match nội bộ:
    - Remaining quantity → Match với virtual counterparty
    - Execution price = currentPrice từ CoinGecko
    - Tạo Trade record với virtual counterparty (có thể set UserId = 0 hoặc null)
  
  - Tính phí: `fee = (quantity * price) * 0.001` (0.1%)

- [ ] **SELL Market Order:**
  - Tương tự BUY nhưng:
    - Tìm BUY orders
    - Sort by Price DESC (highest first)
    - Match với virtual counterparty nếu không đủ

- [ ] Update WalletMovements sau khi match:
  ```csharp
  // BUY: Debit USD, Credit Coin
  // SELL: Debit Coin, Credit USD
  
  // Release lock từ OrderHold
  await ReleaseBalanceAsync(order.Id, filledAmount);
  
  // Credit/Debit wallets
  // BUY example:
  await DebitWalletAsync(userId, "USD", totalCost);
  await CreditWalletAsync(userId, "COIN", cryptoId, quantity);
  await DebitWalletAsync(userId, "USD", fee);  // Fee
  ```

- [ ] Update Order:
  ```csharp
  order.FilledQty = totalFilled;
  order.Status = totalFilled >= order.QuantityCoin ? "FILLED" : "PARTIAL";
  order.UpdatedAt = DateTime.UtcNow;
  ```

**Matching Priority:**
```
1. Internal orderbook (user-to-user matching) - BEST PRICE FIRST
2. CoinGecko price (virtual counterparty) - nếu không đủ match nội bộ
```

---

### ✅ Task 1.5: Limit Order Execution & Matching Engine
**File:** `Services/Trading/TradingService.cs` - Methods `ExecuteLimitOrderAsync()`, `MatchOrdersAsync()`

**Yêu cầu:**
- [ ] **Limit Order Placement:**
  - Khi PlaceOrder với Type = "LIMIT":
    - Lưu vào database với Status = "NEW"
    - Không execute ngay
    - Return OrderDto

- [ ] **Matching Engine (Background Service - Task 2.2):**
  - Tạo `OrderMatchingBackgroundService` (IHostedService)
  - Polling mỗi 5 giây (configurable)
  - Gọi `MatchOrdersAsync()` từ TradingService

- [ ] Method `MatchOrdersAsync()`:
  ```csharp
  // 1. Get all NEW/PARTIAL orders grouped by CryptocurrencyId
  var pendingOrders = await _db.Orders
      .Where(o => o.Status == "NEW" || o.Status == "PARTIAL")
      .GroupBy(o => o.CryptocurrencyId)
      .ToListAsync();
  
  foreach (var group in pendingOrders) {
      var cryptoId = group.Key;
      
      // 2. Get BUY orders (sorted by Price DESC, CreatedAt ASC)
      var buyOrders = group.Where(o => o.Side == "BUY")
          .OrderByDescending(o => o.PriceUsd ?? 0)
          .ThenBy(o => o.CreatedAt)
          .ToList();
      
      // 3. Get SELL orders (sorted by Price ASC, CreatedAt ASC)
      var sellOrders = group.Where(o => o.Side == "SELL")
          .OrderBy(o => o.PriceUsd ?? decimal.MaxValue)
          .ThenBy(o => o.CreatedAt)
          .ToList();
      
      // 4. Match orders
      foreach (var buyOrder in buyOrders) {
          var buyPrice = buyOrder.PriceUsd ?? 0;
          var remainingQty = buyOrder.QuantityCoin - buyOrder.FilledQty;
          
          foreach (var sellOrder in sellOrders) {
              if (remainingQty <= 0) break;
              
              var sellPrice = sellOrder.PriceUsd ?? 0;
              
              // Check if prices match
              if (sellPrice <= buyPrice && sellOrder.Status != "FILLED") {
                  var availableQty = sellOrder.QuantityCoin - sellOrder.FilledQty;
                  var matchQty = Math.Min(remainingQty, availableQty);
                  
                  // Execution price = sell price (better for buyer)
                  var execPrice = sellPrice;
                  
                  // Create Trade for both orders
                  await CreateTradeAsync(buyOrder, matchQty, execPrice);
                  await CreateTradeAsync(sellOrder, matchQty, execPrice);
                  
                  // Update orders
                  UpdateOrderFill(buyOrder, matchQty);
                  UpdateOrderFill(sellOrder, matchQty);
                  
                  // Update wallets
                  await SettleTradeAsync(buyOrder.UserId, sellOrder.UserId, cryptoId, matchQty, execPrice);
                  
                  remainingQty -= matchQty;
              }
          }
      }
  }
  ```

- [ ] Helper method `CreateTradeAsync(Order order, decimal quantity, decimal price)`:
  ```csharp
  var trade = new Trade {
      OrderId = order.Id,
      CryptocurrencyId = order.CryptocurrencyId,
      PriceUsd = price,
      QuantityCoin = quantity,
      FeeUsd = (quantity * price) * 0.001m,
      CreatedAt = DateTime.UtcNow
  };
  _db.Trades.Add(trade);
  ```

- [ ] Helper method `UpdateOrderFill(Order order, decimal fillQty)`:
  ```csharp
  order.FilledQty += fillQty;
  if (order.FilledQty >= order.QuantityCoin) {
      order.Status = "FILLED";
  } else {
      order.Status = "PARTIAL";
  }
  order.UpdatedAt = DateTime.UtcNow;
  ```

- [ ] Helper method `SettleTradeAsync(int buyerId, int sellerId, int cryptoId, decimal qty, decimal price)`:
  ```csharp
  // Buyer: Debit USD, Credit Coin
  await DebitWalletAsync(buyerId, "USD", qty * price);
  await CreditWalletAsync(buyerId, "COIN", cryptoId, qty);
  
  // Seller: Debit Coin, Credit USD
  await DebitWalletAsync(sellerId, "COIN", cryptoId, qty);
  await CreditWalletAsync(sellerId, "USD", qty * price);
  
  // Release locks
  // Fee deducted from buyer's USD wallet
  ```

**Example Matching:**
```
BUY Order: BTC @ $50,000, Quantity: 1.0, Status: NEW
SELL Order: BTC @ $49,900, Quantity: 0.5, Status: NEW

Match: 0.5 BTC @ $49,900 (SELL order price - better for buyer)

Result: 
- SELL order: FILLED (FilledQty = 0.5)
- BUY order: PARTIAL (FilledQty = 0.5, Remaining = 0.5)
- 2 Trade records created
- Wallets updated for both users
```

---

### ✅ Task 1.6: Order Cancellation
**File:** `Services/Trading/TradingService.cs` - Method `CancelOrderAsync()`

**Yêu cầu:**
- [ ] Validate:
  ```csharp
  var order = await _db.Orders
      .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
  
  if (order == null) throw new NotFoundException();
  if (order.Status == "FILLED") throw new InvalidOperationException("Cannot cancel filled order");
  if (order.Status == "CANCELED") throw new InvalidOperationException("Order already canceled");
  ```

- [ ] Release locked balance:
  ```csharp
  var remainingQty = order.QuantityCoin - order.FilledQty;
  
  if (order.Side == "BUY") {
      // Release USD lock
      var lockAmount = remainingQty * (order.PriceUsd ?? order.QuantityCoin * GetCurrentPrice(order.CryptocurrencyId));
      await ReleaseBalanceAsync(order.Id, lockAmount);
  } else {
      // Release Coin lock
      await ReleaseBalanceAsync(order.Id, remainingQty);
  }
  ```

- [ ] Update Order:
  ```csharp
  order.Status = "CANCELED";
  order.UpdatedAt = DateTime.UtcNow;
  await _db.SaveChangesAsync();
  ```

- [ ] Return success

---

### ✅ Task 1.7: Trade History & Query
**File:** `Services/Trading/TradingService.cs` - Methods `GetTradesAsync()`, `GetOrderAsync()`, `GetOrdersAsync()`

**Yêu cầu:**
- [ ] `GetTradesAsync(int userId, TradesQuery? query = null)`:
  ```csharp
  var trades = _db.Trades
      .Where(t => _db.Orders.Any(o => o.Id == t.OrderId && o.UserId == userId));
  
  // Apply filters
  if (query?.Symbol != null) {
      // Filter by symbol
  }
  if (query?.From != null) {
      trades = trades.Where(t => t.CreatedAt >= query.From);
  }
  if (query?.To != null) {
      trades = trades.Where(t => t.CreatedAt <= query.To);
  }
  
  return trades
      .Include(t => t.Order)
      .Include(t => t.Cryptocurrency)
      .OrderByDescending(t => t.CreatedAt)
      .Select(t => new TradeDto { ... })
      .ToListAsync();
  ```

- [ ] `GetOrderAsync(int userId, ulong orderId)`:
  ```csharp
  var order = await _db.Orders
      .Include(o => o.Cryptocurrency)
      .Include(o => o.Trades)  // Need to add navigation property
      .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
  
  return new OrderDetailDto {
      // Order info
      // List of Trades (fills)
      // Timeline
  };
  ```

- [ ] `GetOrdersAsync(int userId, OrdersQuery? query = null)`:
  ```csharp
  var orders = _db.Orders.Where(o => o.UserId == userId);
  
  // Apply filters: Status, Side, Type, Symbol, Date range
  // Pagination
  // Sort by CreatedAt DESC
  
  return orders.Select(o => new OrderDto { ... }).ToListAsync();
  ```

**DTOs cần tạo:**
- `PlaceOrderRequest` - Input cho PlaceOrder
- `OrderDetailDto` - Chi tiết order + list trades
- `TradeDto` - Thông tin trade
- `OrdersQuery`, `TradesQuery` - Query parameters

---

### ✅ Task 1.8: Internal Orderbook Generation
**File:** `Services/Trading/TradingService.cs` - Method `GetOrderBookAsync()`

**Yêu cầu:**
- [ ] Aggregate orders từ database:
  ```csharp
  var orders = await _db.Orders
      .Where(o => o.CryptocurrencyId == cryptoId && 
                 (o.Status == "NEW" || o.Status == "PARTIAL"))
      .ToListAsync();
  
  // Group by Price, sum quantities
  var bids = orders.Where(o => o.Side == "BUY")
      .GroupBy(o => o.PriceUsd)
      .Select(g => new {
          Price = g.Key,
          Quantity = g.Sum(o => o.QuantityCoin - o.FilledQty)
      })
      .OrderByDescending(x => x.Price)
      .Take(20)
      .ToList();
  
  var asks = orders.Where(o => o.Side == "SELL")
      .GroupBy(o => o.PriceUsd)
      .Select(g => new {
          Price = g.Key,
          Quantity = g.Sum(o => o.QuantityCoin - o.FilledQty)
      })
      .OrderBy(x => x.Price)
      .Take(20)
      .ToList();
  ```

- [ ] Get CoinGecko current price:
  ```csharp
  var marketData = await _coinGeckoService.GetMarketDataAsync();
  var currentPrice = marketData.FirstOrDefault(...)?.CurrentPrice;
  ```

- [ ] Build OrderBookDto:
  ```csharp
  return new OrderBookDto {
      Symbol = symbol,
      CurrentPrice = currentPrice,
      Asks = asks.Select(a => new OrderBookLevelDto { ... }),
      Bids = bids.Select(b => new OrderBookLevelDto { ... }),
      LastUpdated = DateTime.UtcNow
  };
  ```

---

## 📦 PHASE 2: BACKEND - API ENDPOINTS

### ✅ Task 2.1: Enhance TradingController
**File:** `Controllers/TradingController.cs`

**Yêu cầu:**
- [ ] Inject `ITradingService` vào constructor:
  ```csharp
  private readonly ITradingService _tradingService;
  
  public TradingController(ITradingService tradingService, ...)
  {
      _tradingService = tradingService;
  }
  ```

- [ ] Refactor `PlaceOrder()`:
  ```csharp
  [HttpPost("orders")]
  public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
  {
      try {
          var userId = GetUserId();
          var order = await _tradingService.PlaceOrderAsync(userId, request);
          return Ok(order);
      }
      catch (InsufficientBalanceException ex) {
          return BadRequest(new { error = "INSUFFICIENT_BALANCE", message = ex.Message });
      }
      catch (InvalidOperationException ex) {
          return BadRequest(new { error = "INVALID_ORDER", message = ex.Message });
      }
  }
  ```

- [ ] Implement `CancelOrder()`:
  ```csharp
  [HttpDelete("orders/{id}")]
  public async Task<IActionResult> CancelOrder(ulong id)
  {
      try {
          var userId = GetUserId();
          var success = await _tradingService.CancelOrderAsync(userId, id);
          if (success) {
              return Ok(new { message = "Order canceled" });
          }
          return BadRequest(new { message = "Cannot cancel order" });
      }
      catch (Exception ex) {
          return BadRequest(new { message = ex.Message });
      }
  }
  ```

- [ ] Enhance `GetOrder()`:
  ```csharp
  [HttpGet("orders/{id}")]
  public async Task<IActionResult> GetOrder(ulong id)
  {
      var userId = GetUserId();
      var order = await _tradingService.GetOrderAsync(userId, id);
      if (order == null) {
          return NotFound();
      }
      return Ok(order);
  }
  ```

- [ ] Enhance `GetOrders()`:
  ```csharp
  [HttpGet("orders")]
  public async Task<IActionResult> GetOrders(
      [FromQuery] string? status,
      [FromQuery] string? side,
      [FromQuery] string? type,
      [FromQuery] string? symbol,
      [FromQuery] string? from,
      [FromQuery] string? to)
  {
      var userId = GetUserId();
      var query = new OrdersQuery {
          Status = status,
          Side = side,
          Type = type,
          Symbol = symbol,
          From = from != null ? DateTime.Parse(from) : null,
          To = to != null ? DateTime.Parse(to) : null
      };
      var orders = await _tradingService.GetOrdersAsync(userId, query);
      return Ok(orders);
  }
  ```

- [ ] Implement `GetTrades()`:
  ```csharp
  [HttpGet("trades")]
  public async Task<IActionResult> GetTrades(
      [FromQuery] string? symbol,
      [FromQuery] string? from,
      [FromQuery] string? to)
  {
      var userId = GetUserId();
      var query = new TradesQuery {
          Symbol = symbol,
          From = from != null ? DateTime.Parse(from) : null,
          To = to != null ? DateTime.Parse(to) : null
      };
      var trades = await _tradingService.GetTradesAsync(userId, query);
      return Ok(trades);
  }
  ```

- [ ] Enhance `GetOrderBook()` - Delegate to TradingService

---

### ✅ Task 2.2: Background Service - Order Matching
**File:** `Services/OrderMatchingBackgroundService.cs`

**Yêu cầu:**
- [ ] Implement `IHostedService`:
  ```csharp
  public class OrderMatchingBackgroundService : BackgroundService
  {
      private readonly IServiceScopeFactory _scopeFactory;
      private readonly ILogger<OrderMatchingBackgroundService> _logger;
      private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);
      
      protected override async Task ExecuteAsync(CancellationToken ct)
      {
          while (!ct.IsCancellationRequested) {
              try {
                  using var scope = _scopeFactory.CreateScope();
                  var tradingService = scope.ServiceProvider.GetRequiredService<ITradingService>();
                  
                  await tradingService.MatchOrdersAsync();
              }
              catch (Exception ex) {
                  _logger.LogError(ex, "Error in order matching");
              }
              
              await Task.Delay(_interval, ct);
          }
      }
  }
  ```

- [ ] Register trong `Program.cs`:
  ```csharp
  builder.Services.AddHostedService<OrderMatchingBackgroundService>();
  ```

- [ ] Error handling:
  - Log errors nhưng không crash service
  - Retry logic cho transient errors

---

### ✅ Task 2.3: DTOs & Response Models
**File:** Tạo file `Models/DTOs/TradingDtos.cs`

**Yêu cầu:**
- [ ] `PlaceOrderRequest`:
  ```csharp
  public record PlaceOrderRequest(
      string Symbol,      // "BTC/USDT"
      string Side,        // "BUY" or "SELL"
      string Type,        // "MARKET" or "LIMIT"
      decimal Quantity,
      decimal? Price = null  // Required for LIMIT
  );
  ```

- [ ] `OrderDto`:
  ```csharp
  public class OrderDto {
      public string Id { get; set; }
      public string Symbol { get; set; }
      public string Side { get; set; }
      public string Type { get; set; }
      public decimal Quantity { get; set; }
      public decimal? Price { get; set; }
      public decimal Filled { get; set; }
      public decimal Remaining { get; set; }
      public string Status { get; set; }
      public DateTime CreatedAt { get; set; }
      public DateTime UpdatedAt { get; set; }
  }
  ```

- [ ] `OrderDetailDto` (extend OrderDto):
  ```csharp
  public class OrderDetailDto : OrderDto {
      public List<TradeDto> Trades { get; set; }
  }
  ```

- [ ] `TradeDto`:
  ```csharp
  public class TradeDto {
      public string Id { get; set; }
      public string OrderId { get; set; }
      public string Symbol { get; set; }
      public decimal PriceUsd { get; set; }
      public decimal QuantityCoin { get; set; }
      public decimal FeeUsd { get; set; }
      public DateTime CreatedAt { get; set; }
  }
  ```

- [ ] `OrdersQuery`, `TradesQuery`:
  ```csharp
  public class OrdersQuery {
      public string? Status { get; set; }
      public string? Side { get; set; }
      public string? Type { get; set; }
      public string? Symbol { get; set; }
      public DateTime? From { get; set; }
      public DateTime? To { get; set; }
      public int Page { get; set; } = 1;
      public int Limit { get; set; } = 50;
  }
  ```

---

## 📦 PHASE 3: DATABASE & MIGRATIONS

### ✅ Task 3.1: Verify Database Schema
**File:** Check `Models/Order.cs`, `Models/Trade.cs`, `Models/OrderHold.cs`

**Yêu cầu:**
- [ ] Verify indexes exist trong `ApplicationDbContext`:
  - Orders: `(UserId, CreatedAt)`, `(CryptocurrencyId, Status)`
  - Trades: `(OrderId, CreatedAt)`
  - OrderHolds: `(OrderId)`, `(WalletId)`

- [ ] Add navigation properties nếu cần:
  - `Order.Trades` collection
  - `Trade.Order` navigation

---

### ✅ Task 3.2: Migration (nếu cần)
**Yêu cầu:**
- [ ] Tạo migration nếu có thay đổi schema:
  ```bash
  dotnet ef migrations add TradingSystemEnhancements
  ```
- [ ] Review migration SQL
- [ ] Test migration trên dev database

---

## 📦 PHASE 4: FRONTEND INTEGRATION

### ✅ Task 4.1: Enhance Trade Page (New Order)
**File:** `frontend/src/components/pages/trader/Trade.tsx`

**Yêu cầu:**
- [ ] Connect `PlaceOrder` API:
  - POST `/api/trading/orders`
  - Handle MARKET vs LIMIT logic
  - Preview order trước khi submit
  - Show estimated total (Quantity * Price + Fee)
  - Show available balance check

- [ ] Real-time price updates:
  - Subscribe to SignalR market price updates
  - Update order preview khi giá thay đổi

- [ ] Validation:
  - Minimum order size
  - Maximum order size (nếu có)
  - Balance sufficiency check

- [ ] Order confirmation dialog:
  - Show summary: Symbol, Side, Type, Quantity, Price, Total, Fee
  - Confirm button → Submit order

---

### ✅ Task 4.2: Enhance Orders Page
**File:** `frontend/src/components/pages/trader/Orders.tsx`

**Yêu cầu:**
- [ ] Connect `GetOrders` API với filters:
  - Status filter: All, New, Partial, Filled, Canceled
  - Side filter: All, Buy, Sell
  - Type filter: All, Market, Limit
  - Symbol search
  - Date range picker

- [ ] Cancel order functionality:
  - DELETE `/api/trading/orders/{id}`
  - Confirm dialog trước khi cancel
  - Refresh list sau khi cancel

- [ ] Real-time updates:
  - Polling hoặc SignalR để update order status
  - Highlight orders mới filled

- [ ] Pagination nếu có nhiều orders

---

### ✅ Task 4.3: Enhance Order Detail Page
**File:** `frontend/src/components/pages/trader/OrderDetail.tsx`

**Yêu cầu:**
- [ ] Connect `GetOrder` API:
  - GET `/api/trading/orders/{id}`
  - Show order info: Symbol, Side, Type, Quantity, Price, Status

- [ ] Display Trade timeline:
  - List tất cả fills (TradeDto)
  - Show: Time, Price, Quantity, Fee cho mỗi fill
  - Visual timeline/stepper

- [ ] Cancel button (nếu Status = NEW hoặc PARTIAL)

- [ ] Navigation:
  - Link back to Orders
  - Link to TradesHistory filtered by this order

---

### ✅ Task 4.4: Enhance Trades History Page
**File:** `frontend/src/components/pages/trader/TradesHistory.tsx`

**Yêu cầu:**
- [ ] Connect `GetTrades` API:
  - GET `/api/trading/trades?symbol=...&from=...&to=...`
  - Display table: Time, Symbol, Side, Price, Quantity, Fee, Total

- [ ] Filters:
  - Symbol selector
  - Date range picker
  - Side filter (Buy/Sell)

- [ ] Export to CSV (optional):
  - Download trade history

---

### ✅ Task 4.5: Update API Client
**File:** `frontend/src/api/trading.ts`

**Yêu cầu:**
- [ ] Add methods:
  ```typescript
  placeOrder(data: PlaceOrderRequest): Promise<OrderDto>
  cancelOrder(orderId: string): Promise<void>
  getOrder(orderId: string): Promise<OrderDetailDto>
  getOrders(filters?: OrdersQuery): Promise<OrderDto[]>
  getTrades(filters?: TradesQuery): Promise<TradeDto[]>
  ```

- [ ] Update types:
  - `PlaceOrderRequest`
  - `OrderDetailDto` (extend OrderDto)
  - `TradeDto`
  - `OrdersQuery`, `TradesQuery`

---

## 📦 PHASE 5: TESTING & VALIDATION

### ✅ Task 5.1: Unit Tests (Optional but Recommended)
**File:** `Tests/TradingServiceTests.cs`

**Yêu cầu:**
- [ ] Test `PlaceOrderAsync()`:
  - Valid order
  - Insufficient balance
  - Invalid symbol
  - Invalid quantity/price

- [ ] Test `CancelOrderAsync()`:
  - Cancel NEW order
  - Cancel PARTIAL order
  - Cannot cancel FILLED order

- [ ] Test Matching logic:
  - BUY + SELL match
  - Partial fill
  - Price-Time priority

---

### ✅ Task 5.2: Integration Testing
**Yêu cầu:**
- [ ] Test end-to-end flow:
  1. User A deposit USD
  2. User A place BUY order
  3. User B place SELL order (match)
  4. Verify balances updated
  5. Verify trades created

- [ ] Test edge cases:
  - Concurrent orders (same price)
  - Large quantity orders
  - Market orders khi không có match nội bộ

---

### ✅ Task 5.3: Manual Testing Checklist
**Yêu cầu:**
- [ ] Place MARKET BUY order → Verify execution
- [ ] Place MARKET SELL order → Verify execution
- [ ] Place LIMIT BUY order → Verify pending
- [ ] Place LIMIT SELL order → Match với BUY → Verify fill
- [ ] Cancel pending order → Verify balance released
- [ ] View order detail → Verify trade timeline
- [ ] View trades history → Verify all trades shown
- [ ] Test với multiple users → Verify matching works

---

## 📦 PHASE 6: DOCUMENTATION & DEPLOYMENT

### ✅ Task 6.1: API Documentation
**Yêu cầu:**
- [ ] Update Swagger comments trong TradingController
- [ ] Document request/response examples
- [ ] Document error codes

---

### ✅ Task 6.2: Code Documentation
**Yêu cầu:**
- [ ] Add XML comments cho public methods
- [ ] Document business rules trong code
- [ ] Update README với trading system architecture

---

## 🔄 DEPENDENCIES & INTEGRATION POINTS

### Dependencies từ module khác:
1. **Auth System (Nhật An):**
   - JWT authentication (đã có)
   - CurrentUser service (đã có)

2. **Market Data (Hữu Triết):**
   - CoinGeckoService (đã có)
   - Real-time price updates (đã có)
   - CryptoPrice cache (đã có)

3. **Portfolio (Trung Hiếu):**
   - Portfolio sẽ sử dụng Trade data từ Trading system
   - No direct dependency

### Integration với Wallet System:
- Sử dụng Wallet + WalletMovement models (đã có)
- Tạo wallets tự động khi cần
- Lock/unlock balance qua OrderHolds

---

## ⚠️ QUAN TRỌNG - BUSINESS RULES

1. **Không gửi lệnh ra ngoài:**
   - Tất cả matching xảy ra trong hệ thống
   - CoinGecko chỉ dùng để lấy giá reference
   - Market orders match với CoinGecko price nhưng chỉ là simulation

2. **Balance Management:**
   - Lock balance ngay khi đặt lệnh
   - Release khi cancel hoặc fill
   - Tính toán available = total - locked

3. **Matching Priority:**
   - Internal orders trước (user-to-user)
   - Nếu không đủ → Match với CoinGecko price (simulation)

4. **Order Status Flow:**
   ```
   NEW → PARTIAL → FILLED
   NEW → CANCELED
   PARTIAL → FILLED
   PARTIAL → CANCELED (remaining qty)
   ```

5. **Fee Calculation:**
   - Suggested: 0.1% of trade value
   - Fee deducted từ USD wallet
   - Track trong Trade.FeeUsd

---

## 📊 DELIVERABLES SUMMARY

### Backend:
- ✅ `ITradingService` + `TradingService`
- ✅ `OrderMatchingBackgroundService`
- ✅ Enhanced `TradingController`
- ✅ DTOs cho Trading APIs
- ✅ Database indexes (nếu cần)

### Frontend:
- ✅ Enhanced Trade page (place order)
- ✅ Enhanced Orders page (list + cancel)
- ✅ Enhanced OrderDetail page (timeline)
- ✅ Enhanced TradesHistory page
- ✅ Updated API client

### Documentation:
- ✅ API documentation (Swagger)
- ✅ Code comments
- ✅ Testing checklist

---

## 🎯 SUCCESS CRITERIA

1. ✅ User có thể đặt lệnh MARKET và LIMIT
2. ✅ Orders được match nội bộ giữa users
3. ✅ Balance được lock/unlock chính xác
4. ✅ Trades được tạo và lưu vào database
5. ✅ Order status được update đúng (NEW → PARTIAL → FILLED)
6. ✅ Frontend hiển thị đầy đủ thông tin orders và trades
7. ✅ Không có lỗi balance calculation
8. ✅ Matching engine hoạt động ổn định (background service)

---

## 📝 NOTES

- **Timeline:** Ước tính 2-3 tuần cho toàn bộ module
- **Priority Order:**
  1. Phase 1 & 2 (Backend Core + API) - Tuần 1
  2. Phase 4 (Frontend) - Tuần 2
  3. Phase 5 & 6 (Testing + Docs) - Tuần 3

- **Cần hỗ trợ từ:**
  - Hữu Triết: Đảm bảo CoinGecko price updates real-time
  - Nhật An: User authentication flow hoạt động tốt

---

**Last Updated:** 2025-01-15  
**Owner:** Vũ Hoàng  
**Status:** 🟡 In Progress
