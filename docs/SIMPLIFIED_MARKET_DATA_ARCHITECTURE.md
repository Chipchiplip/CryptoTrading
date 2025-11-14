# Simplified Market Data Architecture

## 🎯 Tổng quan

Hệ thống Market Data đã được refactor theo kiến trúc **đơn giản, chuẩn SOLID**, chỉ sử dụng **CoinGecko** làm nguồn dữ liệu duy nhất. Không cần Binance API hay API key.

### Kiến trúc mới:

```
┌─────────────────────────────────────────────────────────────┐
│                SimplifiedMarketDataProvider                 │
│         (Public API cho Bot, Risk, TradingService)          │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│              SimplifiedPriceValidator                       │
│                 (Validate MarketQuote)                      │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│       SimplifiedCoinGeckoExchangeDataProvider              │
│        (Generate Bid/Ask from Spot Price)                   │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│              CoinGeckoPriceSource                           │
│          (Adapter for ICoinGeckoService)                    │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│               ICoinGeckoService (Existing)                  │
│              (Fetches spot price from API)                  │
└─────────────────────────────────────────────────────────────┘
```

---

## 📦 Component Overview

### 1. **MarketQuote** (Model)
**File**: `Models/Market/MarketQuote.cs`

```csharp
public class MarketQuote
{
    public string Symbol { get; set; }
    public decimal Bid { get; set; }      // Giá mua (người mua sẵn sàng trả)
    public decimal Ask { get; set; }      // Giá bán (người bán yêu cầu)
    public decimal Mid => (Bid + Ask) / 2m;  // Giá giữa (dùng cho bot, PnL)
    public decimal Last { get; set; }     // Giá spot gốc từ CoinGecko
    public DateTime Timestamp { get; set; }
    public string Source { get; set; }
}
```

**Công thức sinh Bid/Ask từ Last:**
- `Bid = Last * (1 - 0.0005)` → -0.05%
- `Ask = Last * (1 + 0.0005)` → +0.05%
- `Spread = 0.1%` (realistic cho demo trading)

---

### 2. **IUpstreamPriceSource** + **CoinGeckoPriceSource**
**Files**:
- `Services/Market/IUpstreamPriceSource.cs`
- `Services/Market/CoinGeckoPriceSource.cs`

**Nhiệm vụ**: Adapter layer để lấy giá spot từ `ICoinGeckoService` (không thay đổi service cũ)

```csharp
public interface IUpstreamPriceSource
{
    Task<decimal?> GetSpotPriceAsync(string symbol, CancellationToken ct = default);
    string SourceName { get; }
}
```

**Logic**:
1. Normalize symbol: "BTCUSDT" → "BTC"
2. Tìm coin trong cache trước (nhanh)
3. Fallback sang API nếu cache miss
4. Trả về `CurrentPrice` từ CoinGecko

---

### 3. **SimplifiedCoinGeckoExchangeDataProvider**
**File**: `Services/Market/SimplifiedCoinGeckoExchangeDataProvider.cs`

**Nhiệm vụ**: Generate Bid/Ask/Mid từ 1 giá spot

```csharp
public async Task<MarketQuote?> GetQuoteAsync(string symbol)
{
    var spotPrice = await _upstreamSource.GetSpotPriceAsync(symbol);
    if (spotPrice is null or <= 0) return null;

    var bid = spotPrice.Value * (1 - 0.0005m); // -0.05%
    var ask = spotPrice.Value * (1 + 0.0005m); // +0.05%

    return new MarketQuote
    {
        Symbol = symbol,
        Bid = bid,
        Ask = ask,
        Last = spotPrice.Value,
        Mid = (bid + ask) / 2m,
        Timestamp = DateTime.UtcNow,
        Source = "CoinGecko"
    };
}
```

---

### 4. **SimplifiedPriceValidator**
**Files**:
- `Services/Market/ISimplifiedPriceValidator.cs`
- `Services/Market/SimplifiedPriceValidator.cs`

**Validation rules**:
- ✅ Bid > 0
- ✅ Ask > 0
- ✅ Bid < Ask
- ✅ Last > 0
- ⚠️ Quote age < 5 minutes (warning only)

---

### 5. **SimplifiedMarketDataProvider**
**File**: `Services/Market/SimplifiedMarketDataProvider.cs`

**Public API cho toàn bộ hệ thống:**

```csharp
// Lấy quote đầy đủ
MarketQuote? quote = await _marketData.GetQuoteAsync("BTCUSDT");
if (quote != null)
{
    // Sử dụng quote.Bid, quote.Ask, quote.Mid
}

// Lấy Mid Price (dùng cho bot, risk, PnL)
decimal? midPrice = await _marketData.GetMidPriceAsync("BTCUSDT");
if (midPrice.HasValue)
{
    // Sử dụng midPrice.Value
}

// Lấy Bid (dùng cho SELL orders)
decimal? bidPrice = await _marketData.GetBidPriceAsync("BTCUSDT");

// Lấy Ask (dùng cho BUY orders)
decimal? askPrice = await _marketData.GetAskPriceAsync("BTCUSDT");
```

---

## 🔧 Dependency Injection Setup

### Trong `Program.cs` hoặc `Startup.cs`:

```csharp
using CryptoTrading.Services;
using CryptoTradingApp.Services.Market;

// ===== Simplified Market Data Architecture =====

// 1. Register existing CoinGecko service (already configured in Program.cs)
// Note: ICoinGeckoService is already registered as HttpClient in Program.cs
// builder.Services.AddHttpClient<ICoinGeckoService, CoinGeckoService>(...);

// 2. Register upstream price source (adapter)
services.AddScoped<IUpstreamPriceSource, CoinGeckoPriceSource>();

// 3. Register exchange data provider (generates Bid/Ask)
services.AddScoped<IExchangeDataProvider, SimplifiedCoinGeckoExchangeDataProvider>();

// 4. Register price validator
services.AddSingleton<ISimplifiedPriceValidator, SimplifiedPriceValidator>();

// 5. Register market data provider (public API)
services.AddScoped<SimplifiedMarketDataProvider>();
```

**Lưu ý**: 
- `ICoinGeckoService` đã được register trong `Program.cs` (dòng 154-160), không cần register lại
- Namespace: `ICoinGeckoService` nằm trong `CryptoTrading.Services`
- Namespace: Các simplified market data services nằm trong `CryptoTradingApp.Services.Market`

---

## 📖 Usage Examples

### **1. Trong Bot Strategy**

```csharp
public class GridTradingStrategy : ITradingStrategy
{
    private readonly SimplifiedMarketDataProvider _marketData;

    public async Task ExecuteAsync(BotContext context)
    {
        // Lấy Mid Price để tính toán grid levels
        var midPrice = await _marketData.GetMidPriceAsync(symbol);
        if (!midPrice.HasValue)
        {
            _logger.LogWarning("Cannot get mid price for {Symbol}", symbol);
            return;
        }

        // Tính grid levels dựa trên mid price
        var gridLevels = CalculateGridLevels(midPrice.Value, gridCount);

        // Place orders...
    }
}
```

### **2. Trong TradingService - Market Order**

```csharp
public async Task<Order> PlaceMarketOrderAsync(PlaceOrderRequest request)
{
    // Lấy quote để execute market order
    var quote = await _marketData.GetQuoteAsync(request.Symbol);
    if (quote == null)
        throw new InvalidOperationException("Cannot get market quote");

    // Market Order logic:
    // - BUY order: execute tại ASK price (bạn mua từ người bán)
    // - SELL order: execute tại BID price (bạn bán cho người mua)
    decimal executionPrice = request.Side == "BUY"
        ? quote.Ask  // BUY = lấy ASK
        : quote.Bid; // SELL = lấy BID

    _logger.LogInformation(
        "Market {Side} order for {Symbol}: Execution Price={Price}",
        request.Side, request.Symbol, executionPrice);

    // Create order with execution price
    // Note: Cần lấy CryptocurrencyId từ Symbol trước
    var cryptoId = await GetCryptocurrencyIdAsync(request.Symbol);
    var order = new Order
    {
        UserId = GetCurrentUserId(),  // Từ authentication context
        CryptocurrencyId = cryptoId,
        Side = request.Side,  // "BUY" or "SELL"
        Type = "MARKET",
        QuantityCoin = request.Quantity,
        PriceUsd = executionPrice,  // Filled at Bid/Ask
        Status = "FILLED",
        FilledQty = request.Quantity,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    _context.Orders.Add(order);
    await _context.SaveChangesAsync();
    
    // ... update balance, wallet, etc.
    return order;
}
```

### **3. Trong TradingService - Limit Order**

```csharp
public async Task CheckAndFillLimitOrdersAsync()
{
    var pendingOrders = await _context.Orders
        .Where(o => o.Status == "NEW" && o.Type == "LIMIT")
        .ToListAsync();

    foreach (var order in pendingOrders)
    {
        // Lấy symbol từ Cryptocurrency navigation property
        // Hoặc từ một mapping service: Symbol = GetSymbolFromCryptoId(order.CryptocurrencyId)
        var symbol = $"{order.Cryptocurrency.Symbol}USDT";  // Ví dụ: "BTCUSDT"
        var quote = await _marketData.GetQuoteAsync(symbol);
        if (quote == null) continue;

        bool shouldFill = false;

        // Limit BUY: fill khi Ask <= Limit Price
        // Note: Order.PriceUsd chứa limit price cho LIMIT orders
        if (order.Side == "BUY" && order.PriceUsd.HasValue && quote.Ask <= order.PriceUsd.Value)
        {
            order.PriceUsd = quote.Ask;  // Fill at Ask
            order.FilledQty = order.QuantityCoin;  // Fill toàn bộ
            shouldFill = true;
        }

        // Limit SELL: fill khi Bid >= Limit Price
        if (order.Side == "SELL" && order.PriceUsd.HasValue && quote.Bid >= order.PriceUsd.Value)
        {
            order.PriceUsd = quote.Bid;  // Fill at Bid
            order.FilledQty = order.QuantityCoin;  // Fill toàn bộ
            shouldFill = true;
        }

        if (shouldFill)
        {
            order.Status = "FILLED";
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Limit {Side} order filled: {Symbol} @ {Price}",
                order.Side, order.Symbol, order.PriceUsd);
        }
    }
}
```

### **4. Trong RiskManager - PnL Calculation**

```csharp
public async Task<decimal> CalculateUnrealizedPnLAsync(int botId)
{
    var openPositions = await GetOpenPositionsAsync(botId);
    decimal totalPnL = 0;

    foreach (var position in openPositions)
    {
        // Dùng MID PRICE để tính PnL (fair value)
        var currentMidPrice = await _marketData.GetMidPriceAsync(position.Symbol);
        if (!currentMidPrice.HasValue) continue;

        // PnL = (Current Mid - Entry Price) * Quantity
        var pnl = (currentMidPrice.Value - position.EntryPrice) * position.Quantity;
        totalPnL += pnl;

        _logger.LogDebug(
            "Position {Symbol}: Entry={Entry}, Mid={Mid}, PnL={PnL}",
            position.Symbol, position.EntryPrice, currentMidPrice.Value, pnl);
    }

    return totalPnL;
}
```

### **5. Trong BacktestEngine**

```csharp
public async Task<BacktestResult> RunBacktestAsync(Strategy strategy)
{
    var candles = await _marketData.GetCandlesAsync(symbol, "1h", 1000);

    foreach (var candle in candles)
    {
        // Simulate Bid/Ask from candle close
        var simulatedQuote = new MarketQuote
        {
            Symbol = symbol,
            Bid = candle.Close * 0.9995m,  // -0.05%
            Ask = candle.Close * 1.0005m,  // +0.05%
            Last = candle.Close,
            Timestamp = candle.Timestamp
        };

        // Strategy logic với simulated quote
        await strategy.OnCandleAsync(simulatedQuote);
    }

    return backtest.GetResults();
}
```

---

## 🎯 Market Order vs Limit Order Logic

### **Market Order**

**Định nghĩa**: Order thực thi ngay lập tức tại giá thị trường hiện tại

**Logic**:
```csharp
var quote = await GetQuoteAsync(symbol);
if (quote == null) throw new InvalidOperationException("Cannot get market quote");

if (side == "BUY")
{
    // BUY market order: execute tại ASK
    // Lý do: Bạn mua từ người đang bán (họ offer ở Ask)
    executionPrice = quote.Ask;
}
else if (side == "SELL")
{
    // SELL market order: execute tại BID
    // Lý do: Bạn bán cho người đang mua (họ bid ở Bid)
    executionPrice = quote.Bid;
}
```

**Ví dụ thực tế**:
- Quote: BTC Bid=50,000, Ask=50,005
- User place BUY market → filled at **50,005** (Ask)
- User place SELL market → filled at **50,000** (Bid)

---

### **Limit Order**

**Định nghĩa**: Order chỉ thực thi khi giá thị trường đạt hoặc tốt hơn limit price

**Logic**:
```csharp
var quote = await GetQuoteAsync(symbol);
if (quote == null) return; // Không có quote, không thể fill

if (side == "BUY" && limitPrice != null)
{
    // BUY limit: chỉ fill khi Ask <= Limit Price
    if (quote.Ask <= limitPrice)
    {
        executionPrice = quote.Ask;
        // Fill order
    }
}
else if (side == "SELL" && limitPrice != null)
{
    // SELL limit: chỉ fill khi Bid >= Limit Price
    if (quote.Bid >= limitPrice)
    {
        executionPrice = quote.Bid;
        // Fill order
    }
}
```

**Ví dụ thực tế**:
- User đặt BUY limit @ 49,995
- Quote: BTC Bid=49,990, Ask=49,995 → **FILLED** (Ask = Limit)
- Quote: BTC Bid=50,000, Ask=50,005 → **PENDING** (Ask > Limit)

---

### **Mid Price Usage**

**Định nghĩa**: `Mid = (Bid + Ask) / 2` - Giá tham chiếu công bằng

**Khi nào dùng Mid Price:**
- ✅ Bot tính toán grid levels
- ✅ Risk Manager tính PnL
- ✅ Backtesting simulation
- ✅ Portfolio valuation
- ✅ UI hiển thị "current price"

**Khi nào KHÔNG dùng Mid Price:**
- ❌ Order execution (phải dùng Bid/Ask)
- ❌ Slippage calculation (cần order book depth)

---

## 🔥 Migration Guide từ code cũ

### **Trước đây (sử dụng MarketDataProvider cũ)**:

```csharp
// Old way - trả về Mid price trực tiếp
var price = await _marketData.GetMidPriceAsync(baseAsset, quoteAsset);
```

### **Bây giờ (sử dụng SimplifiedMarketDataProvider)**:

```csharp
// New way - lấy MarketQuote đầy đủ
var quote = await _marketData.GetQuoteAsync($"{baseAsset}{quoteAsset}");
var midPrice = quote?.Mid;

// Hoặc lấy Mid trực tiếp
var midPrice = await _marketData.GetMidPriceAsync($"{baseAsset}{quoteAsset}");
```

### **Refactor các service phụ thuộc**:

1. **GridTradingStrategy.cs** → thay `GetMidPriceAsync` bằng `GetQuoteAsync`
2. **MomentumScalpingStrategy.cs** → dùng `GetAskPriceAsync` cho entry
3. **TradingService.cs** → dùng Bid/Ask cho market orders
4. **RiskManager.cs** → dùng Mid cho PnL calculation
5. **PortfolioService.cs** → dùng Mid cho portfolio valuation

---

## 📊 Flow Diagram

### **Complete Data Flow**:

```
User Request
    │
    ▼
SimplifiedMarketDataProvider.GetQuoteAsync("BTCUSDT")
    │
    ▼
SimplifiedCoinGeckoExchangeDataProvider.GetQuoteAsync()
    │
    ├─► IUpstreamPriceSource.GetSpotPriceAsync("BTCUSDT")
    │       │
    │       ▼
    │   CoinGeckoPriceSource
    │       │
    │       ├─► Try Cache (ICryptoCacheService)
    │       │       └─► Found? Return CurrentPrice
    │       │
    │       └─► API Call (ICoinGeckoService.GetMarketDataAsync)
    │               └─► Find BTC coin → Return CurrentPrice
    │
    └─► Generate Bid/Ask from Spot:
            Bid = Spot * (1 - 0.0005)
            Ask = Spot * (1 + 0.0005)
            Mid = (Bid + Ask) / 2
    │
    ▼
SimplifiedPriceValidator.ValidateQuote()
    │
    ├─► Check: Bid > 0 ✓
    ├─► Check: Ask > 0 ✓
    ├─► Check: Bid < Ask ✓
    ├─► Check: Last > 0 ✓
    └─► Check: Age < 5min ⚠️
    │
    ▼
Return MarketQuote to caller
```

---

## 🧪 Testing

### **Unit Test Example**:

```csharp
[Fact]
public async Task GetQuoteAsync_ShouldReturnValidQuote()
{
    // Arrange
    var mockUpstream = new Mock<IUpstreamPriceSource>();
    mockUpstream.Setup(x => x.GetSpotPriceAsync("BTCUSDT", default))
        .ReturnsAsync(50000m);

    var provider = new SimplifiedCoinGeckoExchangeDataProvider(
        mockUpstream.Object,
        Mock.Of<ILogger<SimplifiedCoinGeckoExchangeDataProvider>>());

    // Act
    var quote = await provider.GetQuoteAsync("BTCUSDT");

    // Assert
    Assert.NotNull(quote);
    Assert.Equal("BTCUSDT", quote.Symbol);
    Assert.Equal(50000m, quote.Last);
    Assert.Equal(49975m, quote.Bid);  // 50000 * 0.9995
    Assert.Equal(50025m, quote.Ask);  // 50000 * 1.0005
    Assert.Equal(50000m, quote.Mid);  // (49975 + 50025) / 2
}
```

---

## ✅ Checklist Migration

- [ ] Register DI services in `Program.cs`
- [ ] Replace `IMarketDataProvider` với `SimplifiedMarketDataProvider` ở các Bot Strategies
- [ ] Update `TradingService` để dùng Bid/Ask cho Market Orders
- [ ] Update `RiskManager` để dùng Mid Price cho PnL
- [ ] Test Market Order execution (BUY = Ask, SELL = Bid)
- [ ] Test Limit Order matching logic
- [ ] Verify backtest simulation với realistic Bid/Ask
- [ ] Remove unused BinanceDataProvider references (nếu có)

---

## 🎉 Kết quả

Sau khi migration:
- ✅ Chỉ cần CoinGecko (không cần Binance API)
- ✅ Kiến trúc đơn giản, dễ maintain
- ✅ Market Order = Bid/Ask (realistic)
- ✅ Limit Order = user-defined price
- ✅ Bot/Risk/PnL = Mid Price (fair value)
- ✅ SOLID principles applied
- ✅ Testable với DI

---

**Author**: Claude Code
**Date**: 2025-11-14
**Version**: 1.0
