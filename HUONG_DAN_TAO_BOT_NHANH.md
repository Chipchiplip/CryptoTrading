# Hướng dẫn tạo Bot nhanh - Sẵn sàng chạy ngay

## 🚀 Tạo bot trong 3 bước

### Bước 1: Lấy Strategy ID

```http
GET http://localhost:5299/api/bot-strategies
Authorization: Bearer YOUR_TOKEN
```

Tìm strategy có `strategyKey = "aggressive-forex"` và copy `id`.

### Bước 2: Tạo Bot

Sử dụng file `create_bot_ready_to_run.http` hoặc gửi request:

```json
POST http://localhost:5299/api/bots
{
  "name": "ETH Aggressive Trading Bot",
  "strategyDefinitionId": "YOUR_STRATEGY_ID",
  "baseAsset": "ETH",
  "quoteAsset": "USD",
  "riskProfile": "AGGRESSIVE",
  "parameters": {
    "emaPeriod": 50,
    "initialLot": 0.1,
    "capitalAllocation": 10000,
    "cutLossUSD": 1000,
    ...
  },
  "executionIntervalSeconds": 60
}
```

### Bước 3: Start Bot

```http
POST http://localhost:5299/api/bots/{botId}/start
{
  "mode": "live"
}
```

## ⚙️ Cấu hình Bot

### Parameters quan trọng:

- **capitalAllocation**: 10000 USD - Tổng vốn bot được dùng
- **initialLot**: 0.1 ETH - Số lượng mỗi lệnh
- **cutLossUSD**: 1000 USD - Cắt lỗ khi thua 1000 USD
- **emaPeriod**: 50 - EMA ngắn (bot trade sớm hơn, không cần đợi 200 periods)
- **executionIntervalSeconds**: 60 - Chạy mỗi 60 giây

### Position Sizing:

- **maxCapitalPerTrade**: 1000 USD - Tối đa mỗi lệnh
- **maxDailyExposure**: 5000 USD - Tối đa mỗi ngày

## ✅ Kiểm tra Bot hoạt động

### 1. Kiểm tra status:
```http
GET http://localhost:5299/api/bots/{botId}
```

Status phải là "Running".

### 2. Nudge bot để chạy ngay:
```http
POST http://localhost:5299/api/bots/{botId}/nudge
```

### 3. Xem logs:
```http
GET http://localhost:5299/api/bots/{botId}/logs?page=1&pageSize=50
```

Tìm logs:
- ✅ "Got price from GetMidPriceAsync" → Bot lấy được giá
- ✅ "Placed BUY order" → Bot đã tạo order
- ❌ "Unable to get current price" → Vấn đề market data

### 4. Xem orders:
```http
GET http://localhost:5299/api/bots/{botId}/orders?page=1&pageSize=20
```

Nếu `totalItems > 0` → Bot đang trading!

## 🔧 Troubleshooting

### Bot không trading?

1. **Kiểm tra logs** - Xem có lỗi gì không
2. **Kiểm tra balance** - User có đủ USD không?
3. **Kiểm tra market data** - CoinGecko service có chạy không?
4. **Nudge bot** - Force bot chạy ngay

### Bot báo "Unable to get current price"?

Xem file `docs/BOT_PRICE_FETCH_DEBUG.md` để debug.

### Bot status = "Error"?

Check `lastStatusReason` trong response để biết lý do.

## 📊 Monitor Bot

### Runtime Metrics:

- **totalOrders**: Số orders bot đã tạo
- **filledOrders**: Số orders đã filled
- **realizedPnl**: Lợi nhuận đã thực hiện
- **lastSignal**: Signal gần nhất

### Logs Categories:

- **Execution**: Bot execution logs
- **Trading**: Order placement logs
- **Signal**: Trading signals
- **Error**: Error logs

## 🎯 Tips

1. **EMA Period**: Dùng 50 thay vì 200 để bot trade sớm hơn
2. **Initial Lot**: Bắt đầu nhỏ (0.1 ETH) để test
3. **Cut Loss**: Đặt cắt lỗ hợp lý (10% vốn)
4. **Execution Interval**: 60 giây là hợp lý cho aggressive strategy

## 📝 Lưu ý

- ✅ Luôn dùng **USD**, không dùng USDT
- ✅ Đảm bảo user có đủ **USD balance**
- ✅ Bot cần thời gian để tích lũy price history
- ✅ Monitor logs để đảm bảo bot hoạt động đúng

