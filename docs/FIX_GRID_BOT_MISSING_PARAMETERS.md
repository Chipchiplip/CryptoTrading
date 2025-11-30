# Fix Grid Bot - Missing Parameters

## Vấn đề:

Bot Grid Trading gặp lỗi "Attempted to divide by zero" vì thiếu parameters:
- `lowerBound`
- `upperBound`
- `gridLevels`
- `orderSize`
- `capitalAllocation`

## Giải pháp:

### Option 1: Update Bot (Khuyến nghị)

Sử dụng file `update_grid_bot_parameters.http` để update bot:

```http
PUT /api/bots/{botId}
```

Với body:
```json
{
  "parameters": {
    "symbols": ["ETHUSD"],
    "ai_source": "chat",
    "lowerBound": 2100,
    "upperBound": 4500,
    "gridLevels": 20,
    "orderSize": 0.1,
    "capitalAllocation": 10000,
    "refreshIntervalSeconds": 60
  }
}
```

### Option 2: Tạo Bot Mới

Bot mới được tạo bằng AI sẽ tự động có đầy đủ parameters.

## Parameters cần thiết:

### 1. lowerBound
- **Mô tả:** Giá thấp nhất của grid
- **Ví dụ:** 2100 (70% giá hiện tại nếu giá ~$3000)
- **Yêu cầu:** > 0

### 2. upperBound
- **Mô tả:** Giá cao nhất của grid
- **Ví dụ:** 4500 (150% giá hiện tại, hoặc >= giá limit order của bạn)
- **Yêu cầu:** > lowerBound
- **Lưu ý:** Phải >= giá limit order bạn muốn match (ví dụ: $4,000)

### 3. gridLevels
- **Mô tả:** Số mức giá trong grid
- **Ví dụ:** 20
- **Yêu cầu:** >= 2

### 4. orderSize
- **Mô tả:** Kích thước mỗi order (số lượng ETH)
- **Ví dụ:** 0.1
- **Yêu cầu:** > 0

### 5. capitalAllocation
- **Mô tả:** Vốn phân bổ cho bot
- **Ví dụ:** 10000
- **Yêu cầu:** > 0, <= vốn user có

### 6. refreshIntervalSeconds
- **Mô tả:** Tần suất bot chạy (giây)
- **Ví dụ:** 60
- **Yêu cầu:** >= 10

## Cách tính parameters:

### Dựa trên giá thị trường hiện tại:
```
Giá hiện tại: $3000
lowerBound = $3000 * 0.7 = $2100
upperBound = $3000 * 1.5 = $4500 (hoặc cao hơn nếu cần match limit order $4000)
gridLevels = 20
orderSize = 0.1
capitalAllocation = 10000
```

### Nếu muốn match limit order ở $4,000:
```
lowerBound = 2000
upperBound = 5000 (phải >= 4000)
gridLevels = 20
orderSize = 0.1
capitalAllocation = 10000
```

## Sau khi update:

1. **Restart bot:**
```http
POST /api/bots/{botId}/stop
POST /api/bots/{botId}/start
```

2. **Nudge bot:**
```http
POST /api/bots/{botId}/nudge
```

3. **Kiểm tra logs:**
```http
GET /api/bots/{botId}/logs?category=Execution
```

Tìm:
- "Auto-set lowerBound" → Bot đã tự động set parameters
- "Current price: $X" → Bot lấy được giá
- "OrderPlaced" → Bot đã tạo orders

## Lưu ý:

- Bot mới tạo bằng AI sẽ tự động có parameters đầy đủ
- Bot cũ cần update thủ công
- Sau khi update, restart bot để áp dụng thay đổi

