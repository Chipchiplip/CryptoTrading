# Quick Start - Simplified Market Data System

## 🚀 Cài đặt nhanh trong 3 bước

### **Bước 1: Register DI Services**

Trong `Program.cs`:

```csharp
// ===== Market Data Services (Simplified) =====
services.AddScoped<IUpstreamPriceSource, CoinGeckoPriceSource>();
services.AddScoped<IExchangeDataProvider, SimplifiedCoinGeckoExchangeDataProvider>();
services.AddSingleton<ISimplifiedPriceValidator, SimplifiedPriceValidator>();
services.AddScoped<SimplifiedMarketDataProvider>();
```

---

### **Bước 2: Inject vào Service/Strategy**

```csharp
public class MyTradingStrategy
{
    private readonly SimplifiedMarketDataProvider _marketData;

    public MyTradingStrategy(SimplifiedMarketDataProvider marketData)
    {
        _marketData = marketData;
    }

    public async Task ExecuteAsync()
    {
        // Lấy quote đầy đủ
        var quote = await _marketData.GetQuoteAsync("BTCUSDT");

        Console.WriteLine($"BTC Quote:");
        Console.WriteLine($"  Bid: ${quote.Bid}");
        Console.WriteLine($"  Ask: ${quote.Ask}");
        Console.WriteLine($"  Mid: ${quote.Mid}");
        Console.WriteLine($"  Last: ${quote.Last}");
        Console.WriteLine($"  Spread: {quote.SpreadPercent:F4}%");
    }
}
```

---

### **Bước 3: Sử dụng trong Order Execution**

#### **Market Order**:
```csharp
// BUY market order
var quote = await _marketData.GetQuoteAsync(symbol);
decimal buyPrice = quote.Ask;  // Mua tại ASK

// SELL market order
decimal sellPrice = quote.Bid;  // Bán tại BID
```

#### **Limit Order Check**:
```csharp
var quote = await _marketData.GetQuoteAsync(symbol);

// BUY limit: fill khi Ask <= Limit
if (orderSide == Buy && quote.Ask <= limitPrice)
{
    // Fill order at Ask
}

// SELL limit: fill khi Bid >= Limit
if (orderSide == Sell && quote.Bid >= limitPrice)
{
    // Fill order at Bid
}
```

#### **PnL Calculation**:
```csharp
// Dùng MID PRICE cho PnL
var midPrice = await _marketData.GetMidPriceAsync(symbol);
decimal pnl = (midPrice - entryPrice) * quantity;
```

---

## 📋 Cheat Sheet

| Use Case | Method | Returns |
|----------|--------|---------|
| Full quote | `GetQuoteAsync(symbol)` | `MarketQuote` with Bid/Ask/Mid |
| Bot calculations | `GetMidPriceAsync(symbol)` | `decimal?` mid price |
| BUY market order | `GetAskPriceAsync(symbol)` | `decimal?` ask price |
| SELL market order | `GetBidPriceAsync(symbol)` | `decimal?` bid price |
| Backtesting | `GetCandlesAsync(symbol, interval, limit)` | `List<OHLCVCandle>` |

---

## 🎯 Key Concepts

### **1. Giá được sinh từ CoinGecko Spot:**
```
Spot Price = $50,000 (from CoinGecko)
    ↓
Bid = $49,975  (Spot * 0.9995)
Ask = $50,025  (Spot * 1.0005)
Mid = $50,000  ((Bid + Ask) / 2)
Spread = 0.1%
```

### **2. Khi nào dùng giá nào:**
- **Market BUY** → Ask ($50,025)
- **Market SELL** → Bid ($49,975)
- **Limit Order** → User-defined price (e.g., $49,900)
- **Bot Logic** → Mid ($50,000)
- **PnL Calc** → Mid ($50,000)

### **3. Không phụ thuộc Binance:**
- ✅ Chỉ cần `ICoinGeckoService` (đã có sẵn)
- ✅ Không cần API key
- ✅ Demo trading hoàn toàn realistic

---

## 📖 Đọc thêm

Chi tiết đầy đủ: [SIMPLIFIED_MARKET_DATA_ARCHITECTURE.md](SIMPLIFIED_MARKET_DATA_ARCHITECTURE.md)

---

**Ready to use! 🎉**
