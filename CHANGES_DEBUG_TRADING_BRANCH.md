# Tổng Hợp Thay Đổi Trên Nhánh `debug-trading`

## 📋 Mục Lục
1. [Tổng Quan Thay Đổi](#tổng-quan-thay-đổi)
2. [Chi Tiết Các Thay Đổi](#chi-tiết-các-thay-đổi)
3. [Các Bug Đã Sửa](#các-bug-đã-sửa)
4. [Migration Database](#migration-database)
5. [Hướng Dẫn Test Bot Trading](#hướng-dẫn-test-bot-trading)

---

## Tổng Quan Thay Đổi

### Thống Kê
- **Số file đã thay đổi**: 30+ files
- **Số dòng code thêm**: ~19,000+ dòng
- **Số dòng code xóa**: ~1,700+ dòng
- **Các file mới**: 7 files

### Các Lĩnh Vực Thay Đổi Chính

1. **Risk Management & Kill Switch**
   - Enhanced Risk Manager
   - Kill Switch Service
   - Position Tracker
   - Bot Risk State tracking

2. **Market Data Providers**
   - Simplified Market Data Provider
   - CoinGecko Integration
   - Binance Data Provider
   - Price Validation

3. **Trading Strategies**
   - Grid Trading Strategy (cải thiện)
   - Momentum Scalping Strategy (cải thiện)
   - Aggressive Forex Strategy

4. **Trading Service**
   - Order placement improvements
   - Price validation
   - Market data integration

5. **Database Models**
   - BotRiskState
   - KillSwitchEvent
   - UserCapitalLimits
   - MarketQuote
   - OHLCVCandle

---

## Chi Tiết Các Thay Đổi

### 1. Risk Management System

#### Files Modified:
- `Services/Bot/RiskManager.cs` - Enhanced risk checks
- `Services/Bot/EnhancedRiskManager.cs` - New enhanced risk manager
- `Services/Risk/KillSwitchService.cs` - Kill switch implementation
- `Services/Risk/IKillSwitchService.cs` - Kill switch interface
- `Services/Risk/PositionTracker.cs` - Position tracking
- `Services/Risk/IPositionTracker.cs` - Position tracker interface
- `Models/Bot/BotRiskState.cs` - Risk state model
- `Models/Bot/KillSwitchEvent.cs` - Kill switch event model
- `Models/Bot/UserCapitalLimits.cs` - Capital limits model

#### Changes:
- ✅ Thêm kill switch để dừng bot khi lỗ quá nhiều
- ✅ Track position và capital limits
- ✅ Risk checks trước khi đặt order
- ✅ Cooldown và rate limiting

### 2. Market Data Providers

#### Files Modified:
- `Services/Market/SimplifiedMarketDataProvider.cs` - Simplified provider
- `Services/Market/CoinGeckoDataProvider.cs` - CoinGecko integration
- `Services/Market/BinanceDataProvider.cs` - Binance integration
- `Services/Market/SimplifiedCoinGeckoExchangeDataProvider.cs` - CoinGecko wrapper
- `Services/Market/PriceValidator.cs` - Price validation
- `Services/Market/SimplifiedPriceValidator.cs` - Simplified validator
- `Services/Bot/MarketDataProvider.cs` - Bot market data provider
- `Models/Market/MarketQuote.cs` - Market quote model
- `Models/Market/OHLCVCandle.cs` - OHLCV candle model (NEW)

#### Changes:
- ✅ Simplified market data layer
- ✅ Multiple price sources (CoinGecko, Binance, Mock)
- ✅ Price validation và staleness checks
- ✅ OHLCV data support

### 3. Trading Strategies

#### Files Modified:
- `Services/Bot/Strategies/GridTradingStrategy.cs` - Grid strategy fixes
- `Services/Bot/Strategies/MomentumScalpingStrategy.cs` - Momentum fixes
- `Services/Bot/BotTradingServiceWrapper.cs` - Trading wrapper

#### Changes:
- ✅ Sửa bug unrealized PnL calculation
- ✅ Sửa bug FilledPrice property
- ✅ Sửa symbol format
- ✅ Cải thiện error handling

### 4. Trading Service

#### Files Modified:
- `Services/Trading/TradingService.cs` - Trading service improvements
- `Services/Market/IExchangeDataProvider.cs` - Exchange interface
- `Services/Market/IPriceValidator.cs` - Price validator interface

#### Changes:
- ✅ Price validation improvements
- ✅ Better error messages
- ✅ Market data integration

### 5. Database & Models

#### Files Modified:
- `Data/ApplicationDbContext.cs` - DbContext updates
- `Models/Bot/BotRiskState.cs` - Risk state model
- `Models/Bot/KillSwitchEvent.cs` - Kill switch model
- `Models/Bot/UserCapitalLimits.cs` - Capital limits model
- `Models/Market/MarketQuote.cs` - Market quote model

#### New Files:
- `Models/Market/OHLCVCandle.cs` - OHLCV candle model

### 6. Program.cs & Configuration

#### Files Modified:
- `Program.cs` - Service registration updates

#### Changes:
- ✅ Register new services (KillSwitch, PositionTracker, etc.)
- ✅ Configure market data providers
- ✅ Setup risk management services

---

## Các Bug Đã Sửa

### 🔴 Bug #1: Momentum Strategy Crash - Missing `FilledPrice` Property

**File:** `Services/Bot/Strategies/MomentumScalpingStrategy.cs`

**Vấn đề:** Code truy cập `order.FilledPrice` nhưng `OrderDto` không có property này, gây crash.

**Đã sửa:**
- Thay đổi để dùng `GetOrderAsync()` để lấy `OrderDetailDto` có `AvgPrice`
- Dùng `AvgPrice ?? Price ?? fallbackPrice` để lấy fill price
- Áp dụng cho cả entry và exit orders

### 🔴 Bug #2: Grid Strategy PnL Luôn Bằng 0

**File:** `Services/Bot/Strategies/GridTradingStrategy.cs`

**Vấn đề:** `UnrealizedPnl = Inventory * (currentPrice - LastPrice)` trong khi `LastPrice` được set bằng `currentPrice` trên cùng dòng, làm PnL luôn bằng 0.

**Đã sửa:**
- Thêm property `AverageEntryPrice` vào `GridRuntimeState`
- Thay đổi calculation thành `UnrealizedPnl = Inventory * (currentPrice - AverageEntryPrice)`
- Thêm null/zero checks

### 🔴 Bug #3: Kill Switch Track Sai Loại PnL

**File:** `Services/Bot/Strategies/GridTradingStrategy.cs`

**Vấn đề:** Grid strategy gửi unrealized PnL changes đến kill switch, nhưng unrealized PnL có thể đảo ngược (paper losses), gây behavior sai.

**Đã sửa:**
- Xóa unrealized PnL tracking cho kill switch
- Thêm comment giải thích kill switch chỉ nên track realized PnL
- Skip tracking này trong demo để tránh false triggers

### 🔴 Bug #4: Price = 0 Không Được Validate

**File:** `Services/Trading/TradingService.cs`

**Vấn đề:** Nếu market data provider trả về 0, strategies có thể đặt orders ở giá $0, gây lỗi.

**Đã sửa:**
- Enhanced error message để rõ ràng price phải > 0
- Thêm validation check trước khi dùng price

### 🔴 Bug #5: Symbol Format Mismatch

**File:** `Services/Bot/Strategies/MomentumScalpingStrategy.cs`

**Vấn đề:** Momentum strategy dùng chỉ base asset (ví dụ: "BTC") thay vì full format "BTC/USDT".

**Đã sửa:**
- Thay đổi `Symbol = opportunity.Symbol` thành `Symbol = $"{opportunity.Symbol}/{context.QuoteAsset}"`
- Áp dụng cho cả BUY và SELL orders

### 🔴 Bug #6: Hard-Coded Price Fallback

**File:** `Services/Bot/BotTradingServiceWrapper.cs`

**Vấn đề:** Dùng hard-coded $50,000 fallback khi price không được cung cấp, gây tính toán sai (ví dụ: 100,000x error cho coin $0.50).

**Đã sửa:**
- Xóa hard-coded fallback
- Throw `ArgumentException` nếu price không được cung cấp hoặc <= 0
- Thêm error message rõ ràng

---

## Migration Database

### Migration 004: Fix BotRiskState BotId to Guid

**File:** `database/mysql/004_FixBotRiskStateBotIdToGuid_Simple.sql`

**Mô tả:**
- Thay đổi kiểu dữ liệu của cột `BotId` từ `INT` sang `CHAR(36)` (GUID) trong:
  - `BotRiskStates`
  - `KillSwitchEvents`

**Cách chạy:**
```bash
mysql -u your_username -p crypto_trading < database/mysql/004_FixBotRiskStateBotIdToGuid_Simple.sql
```

**Lưu ý:**
- ⚠️ Backup database trước khi chạy migration
- Nếu có dữ liệu cũ với BotId là INT, cần migrate thủ công hoặc xóa dữ liệu cũ

**Kiểm tra sau migration:**
```sql
DESCRIBE BotRiskStates;
DESCRIBE KillSwitchEvents;
```

Cột `BotId` phải có kiểu `char(36)`.

---

## Hướng Dẫn Test Bot Trading

### 📋 Prerequisites

1. Backend API đang chạy tại `http://localhost:5186` hoặc `https://localhost:7269`
2. Frontend đang chạy tại `http://localhost:5173`
3. Database đã được seed với test data
4. User đã đăng nhập và có JWT token

### 🔄 Quy Trình Test Hoàn Chỉnh

#### Bước 1: Tạo Bot

```http
POST /api/bots
Content-Type: application/json
Authorization: Bearer {your_jwt_token}

{
  "name": "Test Bot 2024",
  "strategyDefinitionId": "a1a11bc3-415d-4a5b-9da4-2e3afac22be5",
  "parameters": {
    "gridLevels": 20,
    "lowerBound": 100000,
    "upperBound": 110000,
    "orderSize": 0.05,
    "capitalAllocation": 15000,
    "rebalanceMode": "balanced",
    "refreshIntervalSeconds": 60
  }
}
```

**Expected Response:**
```json
{
  "id": "f2b606a6-5f80-4615-be7e-b3ccfa8ba720",
  "status": "Draft",
  "name": "Test Bot 2024",
  ...
}
```

#### Bước 2: Start Bot

⚠️ **QUAN TRỌNG:** Phân biệt Strategy ID và Bot ID
- **Strategy ID**: `a1a11bc3-415d-4a5b-9da4-2e3afac22be5` (dùng để TẠO bot)
- **Bot ID**: `f2b606a6-5f80-4615-be7e-b3ccfa8ba720` (dùng để START/STOP/UPDATE bot)

Nếu không nhớ Bot ID:
1. `GET /api/bots` → Lấy danh sách tất cả bots
2. Tìm bot có name = "Test Bot 2024"
3. Copy field "id" từ response → Đó là Bot ID

```http
POST /api/bots/{botId}/start
Content-Type: application/json
Authorization: Bearer {your_jwt_token}

{
  "mode": "live"
}
```

**Expected Response (202 Accepted):**
```json
{
  "operationId": "f2b606a6-5f80-4615-be7e-b3ccfa8ba720",
  "message": "Bot starting"
}
```

**Sau khi start:**
- Status sẽ chuyển từ "Draft" → "Starting" → "Running"
- `nextRunAt` sẽ được set (thường là ngay lập tức hoặc sau vài giây)
- Bot sẽ bắt đầu chạy theo `executionIntervalSeconds` (60 giây)

#### Bước 3: Kiểm Tra Trạng Thái Bot

```http
GET /api/bots/{botId}
Authorization: Bearer {your_jwt_token}
```

**Kiểm tra các field quan trọng:**
- ✅ `status`: "Running" (hoặc "Starting") → Bot đang hoạt động
- ✅ `nextRunAt`: có giá trị (không null) → Bot có lịch chạy
- ✅ `runtime`: có dữ liệu → Bot đã chạy ít nhất 1 lần
- ✅ `updatedAt`: có giá trị → Bot đã được cập nhật

**Ví dụ response khi bot ĐANG CHẠY:**
```json
{
  "id": "f2b606a6-5f80-4615-be7e-b3ccfa8ba720",
  "status": "Running",
  "nextRunAt": "2025-11-10T12:15:00Z",
  "runtime": {
    "nextRunAt": "2025-11-10T12:15:00Z",
    "openPositions": 0,
    "totalOrders": 5,
    "filledOrders": 3,
    "lastExecutionAt": "2025-11-10T12:14:00Z"
  },
  ...
}
```

#### Bước 4: Xem Logs Của Bot

```http
GET /api/bots/{botId}/logs?page=1&pageSize=20
Authorization: Bearer {your_jwt_token}
```

**Nếu bot ĐANG HOẠT ĐỘNG, bạn sẽ thấy:**
- Logs với level "Info", "Debug", "Warning"
- Messages về việc bot đang chạy strategy
- Messages về việc đặt orders
- Timestamps gần đây (trong vài phút)

**Ví dụ logs khi bot HOẠT ĐỘNG:**
```json
{
  "data": [
    {
      "level": "Info",
      "message": "Bot execution started",
      "createdAt": "2025-11-10T12:14:00Z"
    },
    {
      "level": "Info",
      "message": "Strategy executed successfully",
      "createdAt": "2025-11-10T12:14:01Z"
    },
    {
      "level": "Info",
      "category": "order",
      "message": "Placed order: BUY 0.01 BTC/USDT @ 50000",
      "createdAt": "2025-11-10T12:14:02Z"
    }
  ]
}
```

#### Bước 5: Xem Orders Của Bot

```http
GET /api/bots/{botId}/orders?page=1&pageSize=20
Authorization: Bearer {your_jwt_token}
```

**Nếu bot ĐANG HOẠT ĐỘNG và đặt orders:**
- Sẽ có danh sách orders do bot tạo
- Orders có intent (ví dụ: "grid_buy", "grid_sell")
- Orders có `botId` = bot ID của bạn

**Nếu KHÔNG có orders:**
- Bot chưa đặt order nào (có thể đang chờ điều kiện thị trường)

#### Bước 6: Nudge Bot (Buộc Chạy Ngay)

```http
POST /api/bots/{botId}/nudge
Authorization: Bearer {your_jwt_token}
```

**API này buộc bot chạy ngay lập tức** (không cần đợi `nextRunAt`). Dùng để test nhanh xem bot có hoạt động không.

**Response (202 Accepted):**
```json
{
  "message": "Bot execution triggered"
}
```

**Sau đó kiểm tra lại:**
1. `GET /api/bots/{id}` → xem runtime có cập nhật không
2. `GET /api/bots/{id}/logs` → xem có logs mới không
3. `GET /api/bots/{id}/orders` → xem có orders mới không

### 📊 Các Trạng Thái Bot

- **"Draft"**: Bot mới tạo, chưa start → CHƯA HOẠT ĐỘNG
- **"Starting"**: Bot đang khởi động → SẮP HOẠT ĐỘNG
- **"Running"**: Bot đang chạy → ĐANG HOẠT ĐỘNG ✅
- **"Stopped"**: Bot đã dừng → KHÔNG HOẠT ĐỘNG
- **"Error"**: Bot gặp lỗi → KHÔNG HOẠT ĐỘNG ❌

### ✅ Checklist Kiểm Tra Bot Hoạt Động

- ✅ Bot status = "Running" hoặc "Starting"
- ✅ `nextRunAt` có giá trị (không null)
- ✅ `runtime` có dữ liệu (không null)
- ✅ Có logs mới trong vài phút gần đây
- ✅ `updatedAt` được cập nhật gần đây
- ✅ (Tùy chọn) Có orders do bot tạo ra

**Nếu TẤT CẢ các điều kiện trên đều đúng → Bot ĐANG HOẠT ĐỘNG ✅**

**Nếu THIẾU bất kỳ điều kiện nào → Bot CHƯA HOẠT ĐỘNG hoặc GẶP LỖI**

### 🔍 Kiểm Tra Logs Trong Console/Terminal

Nếu bạn đang chạy backend (`dotnet run`), kiểm tra console output:

**Logs khi bot START:**
```
Bot f2b606a6-5f80-4615-be7e-b3ccfa8ba720 starting
```

**Logs khi bot EXECUTE:**
```
Queued bot f2b606a6-5f80-4615-be7e-b3ccfa8ba720 for execution
Executing bot f2b606a6-5f80-4615-be7e-b3ccfa8ba720 with strategy grid-basic
Bot f2b606a6-5f80-4615-be7e-b3ccfa8ba720 executed successfully, next run at ...
```

**Nếu có lỗi:**
```
Error executing bot f2b606a6-5f80-4615-be7e-b3ccfa8ba720: ...
```

### 🛠️ Troubleshooting

#### 1. Bot status vẫn là "Draft" sau khi start:
- → Kiểm tra xem `BotExecutionHostedService` có đang chạy không
- → Kiểm tra logs trong console có lỗi gì không

#### 2. Bot status là "Error":
- → Kiểm tra `lastStatusReason` trong response `GET /api/bots/{id}`
- → Kiểm tra logs để xem lỗi chi tiết

#### 3. Bot không tạo orders:
- → Có thể strategy đang chờ điều kiện thị trường
- → Kiểm tra parameters có hợp lệ không
- → Kiểm tra balance trong wallet có đủ không

#### 4. Bot chạy nhưng không có logs:
- → Đợi vài giây rồi kiểm tra lại (logs có thể delay)
- → Kiểm tra xem `BotLogger` có hoạt động không

#### 5. `nextRunAt` đã qua nhưng bot không chạy:
- → Kiểm tra `BotExecutionHostedService` có đang chạy không
- → Kiểm tra database connection
- → Thử nudge bot để chạy ngay

### 📝 Test Simulation (Backtesting)

Để test strategy với dữ liệu lịch sử:

```http
POST /api/bots/{botId}/simulate
Content-Type: application/json
Authorization: Bearer {your_jwt_token}

{
  "strategyDefinitionId": "a1a11bc3-415d-4a5b-9da4-2e3afac22be5",
  "parameters": {
    "gridLevels": 20,
    "lowerBound": 100000,
    "upperBound": 110000,
    "orderSize": 0.05,
    "capitalAllocation": 15000,
    "rebalanceMode": "balanced",
    "refreshIntervalSeconds": 60
  },
  "startDate": "2024-11-10T00:00:00Z",
  "endDate": "2025-11-10T00:00:00Z",
  "initialCapital": 15000
}
```

### 🎯 Quy Trình Test Hoàn Chỉnh

1. ✅ Tạo bot (`POST /api/bots`) → Status: "Draft"
2. ✅ Start bot (`POST /api/bots/{id}/start`) → Status: "Starting"
3. ⏳ Đợi 5-10 giây
4. ✅ Kiểm tra status (`GET /api/bots/{id}`) → Status: "Running"
5. ✅ Kiểm tra logs (`GET /api/bots/{id}/logs`) → Có logs mới
6. ✅ Kiểm tra orders (`GET /api/bots/{id}/orders`) → (Có thể có orders)
7. ✅ Đợi `executionIntervalSeconds` (60 giây) → Bot sẽ chạy lại tự động
8. ✅ Kiểm tra lại status và logs → Bot vẫn đang chạy

**Nếu tất cả các bước trên đều OK → Bot HOẠT ĐỘNG TỐT ✅**

---

## 📚 Tài Liệu Tham Khảo

- `API_JSON_REQUESTS.txt` - Chi tiết các API requests
- `TRADING_SYSTEM_ANALYSIS.md` - Phân tích hệ thống trading
- `DEMO_FIXES_SUMMARY.md` - Tóm tắt các bug đã sửa
- `database/mysql/README_MIGRATION_004.md` - Hướng dẫn migration
- `docs/TRADING_SYSTEM_IMPLEMENTATION.md` - Tài liệu implementation

---

## 📝 Notes

- Tất cả các fixes được đánh dấu với comment `// DEMO FIX:` để dễ nhận biết
- Không có production-level features được thêm (slippage, partial fills, etc.)
- Các fixes giữ nguyên demo simplicity nhưng đảm bảo correctness
- Grid strategy's `AverageEntryPrice` sẽ cần được update khi order fill tracking được implement (separate feature)

---

**Last Updated:** 2024-11-13
**Branch:** `debug-trading`

