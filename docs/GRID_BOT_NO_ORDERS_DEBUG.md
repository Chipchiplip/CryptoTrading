# Grid Trading Bot - Không Tạo Orders - Debug Guide

## Vấn đề:

Bot Grid Trading đang chạy nhưng `totalOrders = 0`, không tạo orders.

## Nguyên nhân có thể:

### 1. Bot không lấy được giá thị trường
**Triệu chứng:**
- Logs có "Unable to retrieve market price"
- `lastStatusReason` = "Unable to retrieve market price"

**Giải pháp:**
- Kiểm tra market data service có hoạt động không
- Kiểm tra kết nối database
- Xem `docs/BOT_PRICE_FETCH_DEBUG.md`

### 2. Grid Range không hợp lý
**Triệu chứng:**
- `upperBound <= lowerBound`
- Grid lines không được tạo

**Giải pháp:**
- Kiểm tra `parameters.lowerBound` và `upperBound`
- Đảm bảo `upperBound > lowerBound`
- Ví dụ: `lowerBound: 3000, upperBound: 5000`

### 3. Bot chưa có vốn
**Triệu chứng:**
- `capitalAllocation` = 0 hoặc quá nhỏ
- User không có đủ USD

**Giải pháp:**
- Kiểm tra `parameters.capitalAllocation`
- Kiểm tra user có đủ USD trong wallet không
- Set `capitalAllocation` >= 1000 (tối thiểu)

### 4. Bot mới tạo, chưa chạy đủ lần
**Triệu chứng:**
- Bot vừa mới tạo
- `lastExecutionAt` gần đây nhưng chưa có orders

**Giải pháp:**
- Nudge bot để chạy ngay
- Chờ bot chạy thêm vài lần
- Bot cần thời gian để khởi tạo grid lines

### 5. Logic tạo orders có vấn đề
**Triệu chứng:**
- Logs không có "OrderPlaced"
- Logs có "OrderFailed" hoặc "OrderSkipped"

**Giải pháp:**
- Xem logs chi tiết
- Kiểm tra `ShouldPlaceBuy()` và `ShouldPlaceSell()` conditions
- Kiểm tra inventory (cho SELL orders)

## Cách kiểm tra:

### 1. Xem logs:
```http
GET /api/bots/{botId}/logs?category=Execution
```

Tìm:
- "Current price: $X" → Bot lấy được giá
- "OrderPlaced" → Bot đã tạo orders
- "OrderFailed" → Bot gặp lỗi khi tạo orders
- "OrderSkipped" → Bot bỏ qua orders (thiếu inventory/vốn)

### 2. Xem parameters:
```http
GET /api/bots/{botId}
```

Kiểm tra:
- `parameters.lowerBound` và `upperBound`
- `parameters.capitalAllocation`
- `parameters.orderSize`
- `parameters.gridLevels`

### 3. Xem runtime state:
- `openPositions` = 0 → Bot chưa có positions
- `totalOrders` = 0 → Bot chưa tạo orders
- `lastExecutionAt` → Bot có đang chạy không?

## Checklist:

- [ ] Bot status = "Running"?
- [ ] `lastExecutionAt` gần đây?
- [ ] Có logs "Current price"?
- [ ] `upperBound > lowerBound`?
- [ ] `capitalAllocation` > 0?
- [ ] User có đủ USD?
- [ ] Có logs "OrderPlaced"?
- [ ] Có logs "OrderFailed" hoặc "OrderSkipped"?

## Giải pháp nhanh:

### 1. Nudge bot:
```http
POST /api/bots/{botId}/nudge
```

### 2. Kiểm tra và update parameters:
```json
{
  "lowerBound": 3000,
  "upperBound": 5000,
  "gridLevels": 20,
  "orderSize": 0.1,
  "capitalAllocation": 10000
}
```

### 3. Kiểm tra user balance:
- User cần có đủ USD để bot mua ETH
- User cần có đủ ETH để bot bán (nếu muốn tạo SELL orders ngay)

## Expected Behavior:

### Khi bot chạy lần đầu:
1. Bot khởi tạo grid lines (20 lines từ $3000 đến $5000)
2. Bot lấy giá thị trường (ví dụ: $3000)
3. Bot tạo BUY orders ở các grid lines < $3000
4. Bot tạo SELL orders ở các grid lines > $3000 (nếu có inventory)

### Nếu không thấy orders:
- Bot có thể đang chờ giá thị trường
- Bot có thể chưa có vốn
- Bot có thể gặp lỗi khi tạo orders

## Debug Steps:

1. **Xem logs** → Tìm lỗi hoặc warning
2. **Kiểm tra parameters** → Đảm bảo hợp lý
3. **Nudge bot** → Force chạy ngay
4. **Kiểm tra user balance** → Đảm bảo có đủ vốn
5. **Chờ bot chạy thêm** → Bot cần thời gian để khởi tạo

