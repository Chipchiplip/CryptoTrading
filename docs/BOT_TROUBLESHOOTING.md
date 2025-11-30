# Bot Troubleshooting Guide

## Vấn đề: Bot đang chạy nhưng không trading

### Triệu chứng:
- ✅ Status: "Running"
- ❌ P&L: 0.00
- ❌ Realized P&L: 0.00
- ❌ Tín hiệu gần nhất: "—" (không có)
- ❌ totalOrders: 0

## Các bước kiểm tra:

### Bước 1: Kiểm tra Logs

```http
GET http://localhost:5299/api/bots/{botId}/logs?page=1&pageSize=50
```

**Tìm các log quan trọng:**

#### ✅ Logs tốt (Bot đang hoạt động):
```json
{
  "level": "Info",
  "category": "Execution",
  "message": "Got price from GetMidPriceAsync: $3155.23"
},
{
  "level": "Info",
  "category": "Trading",
  "message": "Placed BUY order 0.1 ETH/USD (orderId=12345)"
},
{
  "level": "Info",
  "category": "Signal",
  "message": "Initial LONG signal at $3155.23 qty=0.1"
}
```

#### ❌ Logs xấu (Bot có vấn đề):

**1. Không lấy được giá:**
```json
{
  "level": "Error",
  "category": "Execution",
  "message": "Unable to get current price"
}
```
→ Xem `BOT_PRICE_FETCH_DEBUG.md`

**2. Trend luôn Sideways:**
```json
{
  "level": "Warning",
  "category": "Execution",
  "message": "Trend is Sideways, but attempting entry with small position"
}
```
→ Bot chưa có đủ data hoặc market không có trend rõ ràng

**3. Không đủ balance:**
```json
{
  "level": "Error",
  "category": "Trading",
  "message": "Insufficient balance. Available: 0, Required: 315.52"
}
```
→ User không có đủ USD

**4. Lỗi khi place order:**
```json
{
  "level": "Error",
  "category": "Trading",
  "message": "Failed to place BUY order for ETH: ..."
}
```
→ Có lỗi khi tạo order (check error message)

### Bước 2: Kiểm tra Orders

```http
GET http://localhost:5299/api/bots/{botId}/orders?page=1&pageSize=20
```

- Nếu `totalItems = 0` → Bot chưa từng place order
- Nếu có orders nhưng `filledOrders = 0` → Orders chưa được match

### Bước 3: Nudge Bot

```http
POST http://localhost:5299/api/bots/{botId}/nudge
```

Force bot chạy ngay, sau đó kiểm tra logs lại.

### Bước 4: Kiểm tra Market Data

```http
GET http://localhost:5299/api/market/crypto
```

Xem có data ETH không và format như thế nào.

### Bước 5: Kiểm tra Balance

Đảm bảo user có đủ USD balance để bot có thể trade.

## Các nguyên nhân thường gặp:

### 1. **Không có Market Data** (Phổ biến nhất)
- **Triệu chứng**: Logs có "Unable to get current price"
- **Giải pháp**: 
  - Kiểm tra CoinGecko service
  - Xem `BOT_PRICE_FETCH_DEBUG.md`

### 2. **Trend luôn Sideways**
- **Triệu chứng**: Logs có "Trend is Sideways"
- **Nguyên nhân**: 
  - Chưa đủ price history (EMA cần 50-200 periods)
  - Market không có trend rõ ràng
- **Giải pháp**: 
  - Đợi bot tích lũy đủ data
  - Hoặc giảm `emaPeriod` trong parameters

### 3. **Không đủ Balance**
- **Triệu chứng**: Logs có "Insufficient balance"
- **Giải pháp**: Nạp thêm USD vào wallet

### 4. **Bot không chạy thực sự**
- **Triệu chứng**: `lastExecutionAt` cũ, không có logs mới
- **Giải pháp**: 
  - Nudge bot
  - Kiểm tra `BotExecutionHostedService` có chạy không
  - Restart application

### 5. **Symbol không match**
- **Triệu chứng**: Logs có "Asset ETH not found"
- **Giải pháp**: 
  - Kiểm tra bot dùng "ETH" hay "ethereum"
  - Xem available symbols trong logs

## Quick Fix Commands:

### Force bot chạy ngay:
```http
POST http://localhost:5299/api/bots/{botId}/nudge
```

### Xem logs real-time:
```http
GET http://localhost:5299/api/bots/{botId}/logs?page=1&pageSize=50&category=Execution
```

### Kiểm tra orders:
```http
GET http://localhost:5299/api/bots/{botId}/orders?page=1&pageSize=20
```

## Checklist Debug:

- [ ] Bot status = "Running"?
- [ ] `lastExecutionAt` gần đây?
- [ ] Logs có "Got price"?
- [ ] Logs có "Placed order"?
- [ ] `totalOrders > 0`?
- [ ] User có đủ USD balance?
- [ ] CoinGecko service đang chạy?
- [ ] Không có Error logs?

## Expected Behavior:

Sau khi bot chạy vài lần, bạn nên thấy:

1. **Logs có price**: "Got price from GetMidPriceAsync: $XXXX"
2. **Logs có signal**: "Initial LONG/SHORT signal"
3. **Logs có order**: "Placed BUY/SELL order"
4. **totalOrders > 0**: Bot đã tạo orders
5. **lastSignal không null**: Bot có signal gần nhất

Nếu sau 5-10 lần chạy mà vẫn không có gì, check logs để tìm nguyên nhân.

