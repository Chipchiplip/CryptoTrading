# TÓM TẮT CÁCH BOT TRADING HOẠT ĐỘNG

## 📋 TỔNG QUAN

Hệ thống bot trading tự động giao dịch crypto dựa trên các chiến lược (strategies) được định nghĩa sẵn. Bot có thể được tạo thủ công hoặc tự động từ AI chat.

---

## 🔄 FLOW HOẠT ĐỘNG TỔNG QUAN

```
1. TẠO BOT
   ├── Thủ công: User tạo bot qua UI/API
   └── Tự động: AI chat → /taobot → Tạo bot từ conversation

2. BOT EXECUTION
   ├── BotExecutionHostedService chạy background
   ├── Mỗi bot có execution interval (mặc định 60s)
   ├── Strategy được execute → Tạo orders
   └── Orders được gửi vào order book

3. ORDER MATCHING
   ├── OrderMatchingBackgroundService chạy background
   ├── Match BUY và SELL orders
   ├── Tạo trades khi match thành công
   └── Update wallet balances

4. P&L CALCULATION
   ├── Tính từ trades đã filled
   ├── Realized P&L: Từ trades đã đóng
   ├── Unrealized P&L: Từ positions đang mở
   └── Update vào BotRuntimeInfo

5. MONITORING
   ├── Frontend hiển thị bot status, P&L
   ├── Logs tracking bot execution
   └── Orders history
```

---

## 🎯 CÁC THÀNH PHẦN CHÍNH

### 1. **Bot Creation**

#### A. Tạo từ AI Chat
```
User chat với AI → Cung cấp thông tin (vốn, risk, symbols, time horizon)
→ Gửi "/taobot"
→ Gemini extract bot config từ conversation
→ Tạo AiGeneratedBotProfile
→ User apply suggestion → Tạo TradingBot
```

**Thông tin cần thiết:**
- Vốn (capital): Số USD
- Symbols: BTCUSD, ETHUSD, SOLUSD, etc.
- Risk mode: aggressive, balanced, safe
- Time horizon: scalping, intraday, swing

**API Endpoints:**
- `POST /api/ai/chat` - Chat với AI
- `POST /api/ai/chat/bot-suggestions/{id}/apply` - Apply bot suggestion

#### B. Tạo thủ công
```
User tạo bot qua UI/API
→ Chọn strategy, parameters
→ Tạo TradingBot với status = "DRAFT"
→ Start bot → status = "RUNNING"
```

**API Endpoints:**
- `POST /api/bots` - Tạo bot
- `POST /api/bots/{id}/start` - Start bot
- `POST /api/bots/{id}/stop` - Stop bot

---

### 2. **Bot Execution**

#### BotExecutionHostedService
- Background service chạy liên tục
- Poll các bots có status = "RUNNING"
- Execute bot theo execution interval
- Mỗi bot có worker riêng để xử lý

**Flow:**
```
1. Load bot từ database
2. Load strategy definition
3. Parse parameters
4. Build bot context (market data, portfolio, etc.)
5. Execute strategy.ExecuteAsync()
6. Strategy trả về BotExecutionResult với orders
7. Tạo orders qua TradingService
8. Link orders với bot qua TradingBotOrders
9. Update bot runtime snapshot
10. Schedule next execution
```

**Strategies hiện có:**
- `grid-basic`: Grid Trading
- `momentum_scalping`: Momentum Scalping
- `aggressive_forex`: Aggressive Forex

---

### 3. **Order Management**

#### Order Types
- **MARKET**: Execute ngay với giá thị trường
- **LIMIT**: Chờ giá chạm đến limit price

#### Order Lifecycle
```
NEW → OPEN → FILLED/CANCELLED/REJECTED
```

#### Order Matching
- `OrderMatchingBackgroundService` chạy background
- Match BUY orders với SELL orders
- Tạo `Trade` records khi match thành công
- Update wallet balances

---

### 4. **P&L Calculation**

#### Realized P&L
- Tính từ trades đã filled và đóng position
- Formula: (Sell Price - Buy Price) * Quantity - Fees

#### Unrealized P&L
- Tính từ positions đang mở
- Formula: (Current Price - Entry Price) * Quantity

#### Calculation Flow
```
1. Get filled orders từ TradingBotOrders
2. Get trades từ filled orders
3. Calculate realized P&L từ closed trades
4. Calculate unrealized P&L từ open positions
5. Update BotRuntimeInfo
6. Frontend hiển thị P&L
```

**API Endpoints:**
- `GET /api/bots/{id}` - Get bot detail với runtime info
- `GET /api/bots` - List bots với P&L summary

---

## 🛠️ CÁC ENDPOINTS QUAN TRỌNG

### Bot Management
```http
# List bots
GET /api/bots?status=RUNNING&page=1&pageSize=20

# Get bot detail
GET /api/bots/{id}

# Start bot
POST /api/bots/{id}/start

# Stop bot
POST /api/bots/{id}/stop

# Nudge bot (trigger execution ngay)
POST /api/bots/{id}/nudge
```

### Bot Orders
```http
# Get bot orders
GET /api/bots/{id}/orders?page=1&pageSize=20&status=FILLED

# Get bot logs
GET /api/bots/{id}/logs?page=1&pageSize=50
```

### Testing (DEV ONLY)
```http
# Force fill orders để test P&L
POST /api/bots/{id}/test-fill-orders?count=5

# Test P&L info
POST /api/bots/{id}/test-pnl
```

---

## 📊 BOT RUNTIME INFO

Bot runtime được tính toán và lưu trong `BotRuntimeInfoDto`:

```json
{
  "nextRunAt": "2025-11-30T19:33:52",
  "openPositions": 0,
  "unrealizedPnl": 0,
  "realizedPnl": -1459.236,
  "totalFees": 0,
  "totalOrders": 97,
  "filledOrders": 32,
  "lastSignal": null,
  "lastExecutionAt": "2025-11-30T19:32:51",
  "heartbeatAt": null
}
```

**Các metrics:**
- `realizedPnl`: Lợi nhuận đã thực hiện (từ closed trades)
- `unrealizedPnl`: Lợi nhuận chưa thực hiện (từ open positions)
- `totalFees`: Tổng phí đã trả
- `totalOrders`: Tổng số orders bot đã tạo
- `filledOrders`: Số orders đã được filled
- `lastExecutionAt`: Thời gian execution cuối cùng

---

## 🎨 FRONTEND UI

### Bots List Page
- Hiển thị danh sách bots
- Summary cards: Total P&L, Active Bots, Total Fees
- Filter theo status (RUNNING, STOPPED, ALL)
- Table với P&L nổi bật (Total P&L với icon TrendingUp/Down)

### Bot Detail Page
- Bot information (name, status, strategy)
- Performance Metrics:
  - **Total P&L** (nổi bật, lớn nhất)
  - Realized P&L
  - Unrealized P&L
  - Total Fees
- Bot Statistics (orders, positions)
- Bot Orders table
- Bot Parameters

### AI Chat Page
- Chat với AI để tạo bot
- Bot suggestions hiển thị dưới dạng cards
- Button "Tạo bot từ gợi ý này" để apply suggestion

---

## 🔍 CÁCH TEST BOT

### 1. Test Bot Execution
```http
# Nudge bot để trigger execution ngay
POST /api/bots/{id}/nudge
```

### 2. Test P&L (DEV ONLY)
```http
# Force fill 5 orders
POST /api/bots/{id}/test-fill-orders?count=5

# Check bot detail để xem P&L
GET /api/bots/{id}
```

### 3. Monitor Bot
```http
# Check bot status và P&L
GET /api/bots/{id}

# Check orders
GET /api/bots/{id}/orders?status=FILLED

# Check logs
GET /api/bots/{id}/logs
```

---

## 📝 LƯU Ý QUAN TRỌNG

### 1. **P&L Calculation**
- P&L chỉ được tính khi có **trades** được tạo
- Orders chưa filled → P&L = 0
- LIMIT orders chỉ được filled khi giá chạm limit price
- MARKET orders được filled ngay

### 2. **Bot Execution**
- Bot chạy theo interval (mặc định 60s)
- Có thể nudge để trigger execution ngay
- Bot execution tạo orders, không tạo trades trực tiếp
- Trades được tạo bởi Order Matching Service

### 3. **Order Matching**
- Order Matching Service chạy background
- Match BUY và SELL orders tự động
- Tạo trades khi match thành công
- Update wallet balances

### 4. **Grid Trading Strategy**
- Tạo grid lines trong price range
- Mua ở giá thấp, bán ở giá cao
- Cần có inventory để bán
- Cần có cash để mua

---

## 🚀 QUICK START

### Tạo bot từ AI Chat:
1. Chat với AI: "Tôi có vốn 10000 USD, muốn trade BTC với risk aggressive"
2. Gửi: "/taobot"
3. AI tạo bot suggestion
4. Click "Tạo bot từ gợi ý này"
5. Bot được tạo với status = "DRAFT"
6. Vào Bots page → Click "Bật" để start bot

### Monitor bot:
1. Vào Bots page → Xem summary cards (Total P&L)
2. Click vào bot → Xem detail với Performance Metrics
3. Check orders và logs

### Test bot:
1. Nudge bot để trigger execution
2. Check orders được tạo
3. Force fill orders (DEV) để test P&L
4. Check P&L được update

---

## 📚 TÀI LIỆU LIÊN QUAN

- `docs/BOT_API_REFERENCE.md` - API reference chi tiết
- `docs/BOT_DEBUG_GUIDE.md` - Hướng dẫn debug bot
- `docs/BOT_MONITORING_GUIDE.md` - Hướng dẫn monitor bot
- `docs/AI_BOT_CREATION_REQUIREMENTS.md` - Yêu cầu tạo bot từ AI

---

## ✅ CHECKLIST TEST BOT

- [ ] Bot được tạo thành công
- [ ] Bot status = "RUNNING"
- [ ] Bot tạo orders khi execute
- [ ] Orders được filled (hoặc force fill trong DEV)
- [ ] Trades được tạo từ filled orders
- [ ] P&L được tính và hiển thị
- [ ] Frontend hiển thị P&L đúng (màu xanh/đỏ)
- [ ] Summary cards hiển thị Total P&L
- [ ] Bot detail page hiển thị đầy đủ metrics

---

**Last Updated:** 2025-11-30

