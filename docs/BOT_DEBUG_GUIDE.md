# Hướng dẫn Debug Bot Không Trading

## Vấn đề: Bot đang chạy nhưng không có orders

### Bước 1: Kiểm tra Logs của Bot

```http
GET http://localhost:5299/api/bots/{botId}/logs?page=1&pageSize=50&level=Error
```

Tìm các log có:
- `"Unable to get current price"` → Vấn đề với market data
- `"Failed to place order"` → Vấn đề khi tạo order
- `"Insufficient balance"` → Không đủ vốn
- `"Trend is Sideways"` → Không có signal để trade

### Bước 2: Kiểm tra Orders của Bot

```http
GET http://localhost:5299/api/bots/{botId}/orders?page=1&pageSize=20
```

Nếu `totalItems: 0` → Bot chưa từng place order nào.

### Bước 3: Kiểm tra Bot Status và NextRunAt

```http
GET http://localhost:5299/api/bots/{botId}
```

Kiểm tra:
- `status`: Phải là "Running"
- `nextRunAt`: Phải là thời gian trong tương lai (hoặc đã qua nếu bot đang chờ chạy)
- `lastExecutionAt`: Xem bot có chạy gần đây không

### Bước 4: Nudge Bot để chạy ngay

```http
POST http://localhost:5299/api/bots/{botId}/nudge
```

Sau đó kiểm tra logs lại để xem bot có chạy không.

### Bước 5: Kiểm tra Market Data

Bot cần market data để:
1. Lấy giá hiện tại
2. Tính EMA (cần 200 periods mặc định)
3. Tính RSI (cần 5 periods)

Nếu không có data → Bot sẽ không trade.

### Bước 6: Kiểm tra Balance

Bot cần có USD balance để mua ETH. Kiểm tra:
- User có đủ USD không?
- Wallet có bị lock không?

## Các nguyên nhân thường gặp:

### 1. **Không có Market Data** ⚠️ PHỔ BIẾN NHẤT
- **Triệu chứng**: Logs có "Unable to get current price", bot status = "Error"
- **Nguyên nhân**: 
  - CoinGecko service không hoạt động
  - Symbol không match ("ETH" vs "ethereum")
  - Cache service không có data
- **Giải pháp**: 
  - Xem file `BOT_PRICE_FETCH_DEBUG.md` để debug chi tiết
  - Kiểm tra logs để xem available symbols
  - Đảm bảo CoinGecko service đang chạy

### 2. **Trend luôn là Sideways**
- **Triệu chứng**: Logs có "Trend is Sideways"
- **Nguyên nhân**: 
  - Chưa đủ data để tính EMA (cần 200 periods)
  - RSI ở mức neutral (50)
- **Giải pháp**: 
  - Đợi bot tích lũy đủ price history
  - Hoặc giảm `emaPeriod` trong parameters

### 3. **Không đủ Balance**
- **Triệu chứng**: Logs có "Insufficient balance"
- **Giải pháp**: Nạp thêm USD vào wallet

### 4. **Bot không chạy**
- **Triệu chứng**: `nextRunAt` đã qua nhưng bot không chạy
- **Giải pháp**: 
  - Kiểm tra `BotExecutionHostedService` có đang chạy không
  - Restart application
  - Nudge bot

### 5. **Symbol không match**
- **Triệu chứng**: Bot dùng "ETH/USD" nhưng market data không có
- **Giải pháp**: Đảm bảo bot dùng đúng symbol (USD, không phải USDT)

## Debug Commands:

### Xem tất cả logs của bot (bao gồm Info, Warning, Error):
```http
GET http://localhost:5299/api/bots/{botId}/logs?page=1&pageSize=100
```

### Xem chỉ Error logs:
```http
GET http://localhost:5299/api/bots/{botId}/logs?page=1&pageSize=50&level=Error
```

### Xem logs theo category:
```http
GET http://localhost:5299/api/bots/{botId}/logs?page=1&pageSize=50&category=Execution
GET http://localhost:5299/api/bots/{botId}/logs?page=1&pageSize=50&category=Trading
```

## Ví dụ Logs tốt (Bot đang hoạt động):

```json
{
  "level": "Info",
  "category": "Execution",
  "message": "Current price: $3155.23"
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

## Ví dụ Logs xấu (Bot không hoạt động):

```json
{
  "level": "Error",
  "category": "Execution",
  "message": "Unable to get current price"
},
{
  "level": "Warning",
  "category": "Execution",
  "message": "Trend is Sideways, but attempting entry with small position"
}
```

