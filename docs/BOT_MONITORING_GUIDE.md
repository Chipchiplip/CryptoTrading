# Bot Monitoring Guide

## Bot đang hoạt động tốt! 🎉

### Bot ID: `fed10806-0ca5-480c-be31-3f45943eed36`

**Current Status:**
- ✅ Status: Running
- ✅ totalOrders: 2
- ✅ filledOrders: 2
- ✅ Đã có trading activity

## Cách Monitor Bot

### 1. Kiểm tra Orders

```http
GET http://localhost:5299/api/bots/fed10806-0ca5-480c-be31-3f45943eed36/orders?page=1&pageSize=20
```

Xem:
- Bot đã tạo orders nào
- Orders đã filled chưa
- Price và quantity
- P&L của từng order

### 2. Kiểm tra Logs

```http
GET http://localhost:5299/api/bots/fed10806-0ca5-480c-be31-3f45943eed36/logs?page=1&pageSize=50
```

Tìm:
- "Placed BUY/SELL order" → Bot đã tạo order
- "Initial LONG/SHORT signal" → Bot có signal
- "Got price from GetMidPriceAsync" → Bot lấy được giá
- "OrderFilled" → Order đã được match

### 3. Kiểm tra Runtime Metrics

```http
GET http://localhost:5299/api/bots/fed10806-0ca5-480c-be31-3f45943eed36
```

Xem `runtime` object:
- `totalOrders`: Tăng lên khi bot tạo order mới
- `filledOrders`: Tăng lên khi order được fill
- `realizedPnl`: Lợi nhuận từ orders đã closed
- `unrealizedPnl`: Lợi nhuận từ open positions
- `lastSignal`: Signal gần nhất

## Expected Behavior

### Bot đang hoạt động tốt nếu:

1. ✅ `totalOrders` tăng dần
2. ✅ `filledOrders` tăng dần
3. ✅ Logs có "Placed order" messages
4. ✅ Logs có "Signal" messages
5. ✅ `lastExecutionAt` được update thường xuyên
6. ✅ Status = "Running"

### Bot có vấn đề nếu:

1. ❌ `totalOrders` không tăng sau nhiều lần chạy
2. ❌ Logs có nhiều Error
3. ❌ `lastExecutionAt` không update
4. ❌ Status = "Error" hoặc "Degraded"

## Monitoring Schedule

### Hàng ngày:
- Kiểm tra `realizedPnl` và `unrealizedPnl`
- Xem `totalOrders` và `filledOrders`
- Kiểm tra `lastSignal`

### Hàng giờ:
- Kiểm tra status = "Running"
- Xem logs có Error không
- Kiểm tra `lastExecutionAt` có update không

### Real-time:
- Xem logs khi bot chạy (sau nudge)
- Monitor orders mới được tạo
- Track signals

## Performance Metrics

### Good Performance:
- `filledOrders` / `totalOrders` > 80% (high fill rate)
- `realizedPnl` > 0 (profitable)
- `totalOrders` tăng đều (active trading)

### Poor Performance:
- `filledOrders` / `totalOrders` < 50% (low fill rate)
- `realizedPnl` < 0 (losing money)
- `totalOrders` không tăng (not trading)

## Tips

1. **Monitor regularly**: Check bot ít nhất 1 lần/ngày
2. **Watch for errors**: Nếu có nhiều Error logs, cần fix ngay
3. **Track P&L**: Monitor `realizedPnl` và `unrealizedPnl`
4. **Check signals**: Xem `lastSignal` để biết bot đang nghĩ gì
5. **Review orders**: Xem orders để hiểu bot đang trade như thế nào

## Quick Commands

Sử dụng file `monitor_bot.http` để monitor bot nhanh chóng.

