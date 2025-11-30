# Tại sao bot chỉ có 2 orders?

## Nguyên nhân có thể:

### 1. Bot đã mua và bán (Break Even)
- Bot mở 1 position BUY → 1 order
- Sau đó đóng position → 1 order SELL
- Tổng = 2 orders
- P&L ≈ 0 (break even)

### 2. Bot chưa đủ điều kiện để tạo thêm orders

#### Pyramid (Khi có lời):
- **Điều kiện:** Lời >= `pyramidTriggerPercent` (mặc định 3%)
- **Công thức:** `(totalPnL / totalInvestment) >= pyramidTriggerPercent`
- **Ví dụ:** Nếu mua $1000, cần lời >= $30 (3%) mới pyramid

#### Martingale (Khi lỗ):
- **Điều kiện:** Lỗ >= `martingaleDistancePercent` (mặc định 4%)
- **Công thức:** `(totalPnL / totalInvestment) <= -martingaleDistancePercent`
- **Ví dụ:** Nếu mua $1000, cần lỗ >= $40 (4%) mới martingale

### 3. Bot đã đóng tất cả positions
- Cut loss triggered
- Trailing stop triggered
- Bot đóng tất cả → Không còn positions → Không tạo orders mới

### 4. Bot đang chờ điều kiện
- Trend = Sideways → Không tạo orders mới
- RSI không phù hợp → Không tạo orders mới
- Chưa đủ data (EMA period) → Không tạo orders mới

## Cách kiểm tra:

### 1. Xem orders:
```http
GET /api/bots/{botId}/orders
```

Kiểm tra:
- Orders là BUY hay SELL?
- Orders đã filled chưa?
- Price và quantity của từng order

### 2. Xem logs:
```http
GET /api/bots/{botId}/logs?category=Trading
```

Tìm:
- "Opened initial position" → Bot đã mở position
- "Added pyramid position" → Bot đã pyramid
- "Added martingale position" → Bot đã martingale
- "Closed position" → Bot đã đóng position

### 3. Xem runtime metrics:
```http
GET /api/bots/{botId}
```

Kiểm tra:
- `openPositions` → Số positions đang mở
- `realizedPnl` → Lợi nhuận đã thực hiện
- `unrealizedPnl` → Lợi nhuận chưa thực hiện
- `totalOrders` → Tổng số orders
- `filledOrders` → Số orders đã filled

## Giải pháp để bot tạo nhiều orders hơn:

### 1. Giảm threshold cho Pyramid:
```json
{
  "pyramidTriggerPercent": 1.0  // Thay vì 3.0 (dễ pyramid hơn)
}
```

### 2. Giảm threshold cho Martingale:
```json
{
  "martingaleDistancePercent": 2.0  // Thay vì 4.0 (dễ martingale hơn)
}
```

### 3. Tăng số lượng orders tối đa:
```json
{
  "maxPyramidOrders": 20,  // Thay vì 10
  "maxMartingaleOrders": 20  // Thay vì 10
}
```

### 4. Kiểm tra bot có đang chạy không:
- `status` = "Running"?
- `lastExecutionAt` gần đây?
- Có logs mới không?

## Ví dụ:

### Scenario 1: Bot mua và bán
```
Order 1: BUY 0.1 ETH @ $3000 → Filled
Order 2: SELL 0.1 ETH @ $3000 → Filled
→ totalOrders = 2, realizedPnl = 0
```

### Scenario 2: Bot chờ điều kiện
```
Order 1: BUY 0.1 ETH @ $3000 → Filled
→ Bot chờ lời >= 3% để pyramid
→ Giá hiện tại: $3010 (lời 0.33%, chưa đủ 3%)
→ Bot không tạo order mới
→ totalOrders = 1 (hoặc 2 nếu đã đóng)
```

### Scenario 3: Bot đã đóng position
```
Order 1: BUY 0.1 ETH @ $3000 → Filled
→ Bot đóng position (cut loss hoặc trailing stop)
Order 2: SELL 0.1 ETH @ $2990 → Filled
→ totalOrders = 2, realizedPnl = -$10
```

## Checklist:

- [ ] Bot status = "Running"?
- [ ] `openPositions` > 0? (Nếu = 0, bot đã đóng tất cả)
- [ ] `realizedPnl` = 0? (Nếu = 0, có thể bot đã mua và bán break even)
- [ ] Có logs "Opened initial position"?
- [ ] Có logs "Added pyramid/martingale position"?
- [ ] Có logs "Closed position"?
- [ ] `lastExecutionAt` gần đây? (Bot có đang chạy không?)

