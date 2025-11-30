# Bot Mua Nhiều Orders Hơn - Đã Sửa

## Những thay đổi đã thực hiện:

### 1. Giảm Threshold cho Pyramid (Dễ Pyramid Hơn)
- **Trước:** Cần lời >= 3% mới pyramid
- **Sau:** Cần lời >= 1% là pyramid được
- **Kết quả:** Bot sẽ pyramid sớm hơn, tạo nhiều orders hơn

### 2. Giảm Threshold cho Martingale (Dễ Martingale Hơn)
- **Trước:** Cần lỗ >= 4% mới martingale
- **Sau:** Cần lỗ >= 2% là martingale được
- **Kết quả:** Bot sẽ martingale sớm hơn, tạo nhiều orders hơn

### 3. Tăng Số Lượng Orders Tối Đa
- **Trước:** maxPyramidOrders = 10, maxMartingaleOrders = 10
- **Sau:** maxPyramidOrders = 20, maxMartingaleOrders = 20
- **Kết quả:** Bot có thể tạo nhiều orders hơn

### 4. Thêm Logic Mua Thêm Khi Trend Mạnh
- Bot sẽ mua thêm khi:
  - Trend mạnh (Up/Down, không phải Sideways)
  - Chưa có nhiều positions (< 5)
  - Không lỗ quá nhiều (< 1%) hoặc có lời
  - Chưa đạt max pyramid orders
- **Kết quả:** Bot sẽ mua nhiều hơn khi thị trường tốt

### 5. Cho Phép Cả Pyramid VÀ Martingale
- **Trước:** Chỉ kiểm tra pyramid HOẶC martingale (else if)
- **Sau:** Kiểm tra cả hai (if riêng biệt)
- **Kết quả:** Bot có thể tạo nhiều orders hơn trong một lần chạy

## Kết quả mong đợi:

### Trước khi sửa:
- Bot chỉ tạo 1-2 orders
- Cần lời >= 3% mới pyramid
- Cần lỗ >= 4% mới martingale

### Sau khi sửa:
- Bot sẽ tạo nhiều orders hơn (5-10+ orders)
- Chỉ cần lời >= 1% là pyramid
- Chỉ cần lỗ >= 2% là martingale
- Mua thêm khi trend mạnh

## Lưu ý:

1. **Risk cao hơn:** Bot sẽ mua nhiều hơn → rủi ro cao hơn
2. **Cần đủ vốn:** Đảm bảo có đủ vốn để bot mua nhiều orders
3. **Monitor thường xuyên:** Kiểm tra bot thường xuyên để đảm bảo hoạt động tốt

## Cách kiểm tra:

1. **Xem orders:**
```http
GET /api/bots/{botId}/orders
```

2. **Xem logs:**
```http
GET /api/bots/{botId}/logs?category=Trading
```

3. **Xem runtime metrics:**
```http
GET /api/bots/{botId}
```

Tìm:
- `totalOrders` tăng lên
- Logs có "Added pyramid position"
- Logs có "Added martingale position"
- Logs có "adding additional position (aggressive mode)"

## Nếu vẫn chỉ có 2 orders:

1. **Kiểm tra bot có đang chạy không:**
   - `status` = "Running"?
   - `lastExecutionAt` gần đây?

2. **Kiểm tra vốn:**
   - Có đủ vốn để mua nhiều orders không?
   - `capitalAllocation` đủ lớn?

3. **Kiểm tra market conditions:**
   - Trend có mạnh không?
   - Giá có biến động không?

4. **Kiểm tra logs:**
   - Có lỗi gì không?
   - Bot có đang chờ điều kiện không?

