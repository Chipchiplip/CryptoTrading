# Grid Trading - Match Limit Orders

## Vấn đề:

Bot không bán khi user treo limit order mua ETH giá $4,000 trong khi thị trường ở $3,000.

## Nguyên nhân:

1. **Grid range không bao gồm $4,000:**
   - Nếu `upperBound` < 4000, bot không có grid line ở $4,000
   - Bot chỉ tạo SELL orders ở các grid lines có sẵn

2. **Bot chưa có inventory:**
   - Bot cần có ETH để bán
   - Nếu `state.Inventory < orderSize`, bot không tạo SELL orders

3. **Logic cũ chỉ tạo SELL khi `currentPrice < gridLinePrice`:**
   - Bot chỉ tạo SELL orders ở các grid lines cao hơn currentPrice
   - Nhưng nếu không có grid line ở $4,000 → Không tạo được

## Giải pháp đã thực hiện:

### 1. Tự động mở rộng Grid Range
- Khi giá hiện tại gần `upperBound`, bot tự động mở rộng grid lên cao hơn
- Thêm grid lines mới để bao gồm các mức giá cao hơn
- Đảm bảo bot có grid lines ở các mức giá user có thể treo limit orders

### 2. Tự động tạo SELL Orders
- Bot tự động tạo SELL orders ở **tất cả** grid lines cao hơn currentPrice
- Không cần chờ điều kiện đặc biệt
- Tạo nhiều SELL orders để match với limit orders của user

### 3. Kiểm tra Inventory
- Bot chỉ tạo SELL orders khi có đủ inventory
- Tạo tối đa số orders có thể với inventory hiện có

## Cách setup đúng:

### 1. Set Grid Range bao gồm giá limit order:
```json
{
  "lowerBound": 3000,  // Giá thấp
  "upperBound": 5000,  // Phải >= giá limit order của user (4000)
  "gridLevels": 20     // Nhiều mức giá hơn
}
```

### 2. Đảm bảo bot có inventory:
- Bot cần mua ETH trước (qua BUY orders)
- Sau khi có inventory, bot sẽ tự động tạo SELL orders

### 3. Monitor bot:
- Kiểm tra `inventory` trong logs
- Kiểm tra số lượng SELL orders đã tạo
- Kiểm tra grid range có bao gồm giá limit order không

## Ví dụ:

### Scenario 1: Grid range đúng
```
lowerBound: 3000
upperBound: 5000 (bao gồm 4000)
gridLevels: 20
→ Bot có grid line ở $4,000
→ Bot sẽ tạo SELL order ở $4,000 khi có inventory
→ Match với limit order của user ✅
```

### Scenario 2: Grid range không đủ
```
lowerBound: 3000
upperBound: 3500 (không bao gồm 4000)
gridLevels: 10
→ Bot KHÔNG có grid line ở $4,000
→ Bot không tạo SELL order ở $4,000
→ KHÔNG match được ❌
```

### Scenario 3: Bot chưa có inventory
```
Grid range: 3000-5000 ✅
Grid line ở $4,000: Có ✅
Inventory: 0 ❌
→ Bot không tạo SELL order vì không có ETH để bán
→ Cần chờ bot mua ETH trước
```

## Checklist:

- [ ] `upperBound` >= giá limit order của user?
- [ ] Bot có inventory > 0?
- [ ] Grid range bao gồm giá limit order?
- [ ] Bot đang chạy và tạo orders?
- [ ] Có logs "SELL order at $X to match limit orders"?

## Lưu ý:

1. **Grid range phải đủ lớn:** `upperBound` phải >= giá limit order cao nhất user có thể treo
2. **Bot cần inventory:** Bot phải mua ETH trước mới bán được
3. **Chờ bot chạy:** Bot cần thời gian để tạo orders, không phải ngay lập tức
4. **Monitor logs:** Xem logs để biết bot đang làm gì

## Debug:

Nếu bot vẫn không bán:

1. **Kiểm tra grid range:**
```http
GET /api/bots/{botId}
```
Xem `parameters.upperBound` có >= 4000 không?

2. **Kiểm tra inventory:**
Xem logs có "inventory: X" không? Nếu = 0, bot chưa có ETH.

3. **Kiểm tra grid lines:**
Xem logs có "SELL order at $X" không? Nếu không có, bot chưa tạo SELL orders.

4. **Kiểm tra orders:**
```http
GET /api/bots/{botId}/orders
```
Xem có SELL orders nào không?

