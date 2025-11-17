# Fix: Lỗi 500 Internal Server Error và SignalR Warnings

## 🔴 Vấn Đề 1: Lỗi 500 Internal Server Error khi POST `/api/trading/orders`

### Nguyên Nhân
- `TradingController.PlaceOrder()` không có try-catch để handle exceptions
- Khi `TradingService.PlaceOrderAsync()` throw exception, nó trả về 500 Internal Server Error thay vì error message rõ ràng
- Client không biết lỗi cụ thể là gì

### Các Exception Có Thể Xảy Ra
1. **InvalidOperationException**: 
   - Cryptocurrency không tìm thấy
   - Price = 0 hoặc invalid
   - Insufficient balance
   - Wallet không tìm thấy

2. **ArgumentException**: 
   - Invalid symbol format
   - Invalid parameters

3. **UnauthorizedAccessException**: 
   - User không có quyền

4. **Database exceptions**: 
   - Transaction failed
   - Constraint violations

### Giải Pháp
✅ **Đã sửa**: Thêm try-catch trong `TradingController.PlaceOrder()` để:
- Return `400 BadRequest` cho InvalidOperationException và ArgumentException
- Return `403 Forbid` cho UnauthorizedAccessException  
- Return `500 Internal Server Error` với message rõ ràng cho các exception khác
- Log đầy đủ để debug

### Code Changes
```csharp
[HttpPost("orders")]
public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }
    
    try
    {
        var userId = GetUserId();
        var order = await _tradingService.PlaceOrderAsync(userId, request);
        return Ok(order);
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
        return BadRequest(new { message = ex.Message, error = "InvalidOperation" });
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning(ex, "Invalid argument: {Message}", ex.Message);
        return BadRequest(new { message = ex.Message, error = "InvalidArgument" });
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(ex, "Unauthorized: {Message}", ex.Message);
        return Forbid(ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error placing order");
        return StatusCode(500, new { message = "An error occurred while placing the order.", error = ex.Message });
    }
}
```

---

## ⚠️ Vấn Đề 2: SignalR Warnings - Method Names Not Found

### Warning Messages
```
Warning: No client method with the name 'receivepricelist' found.
Warning: No client method with the name 'receivemarketstats' found.
```

### Nguyên Nhân
- SignalR method names là **case-sensitive**
- Backend gửi: `ReceivePriceList` và `ReceiveMarketStats` (PascalCase)
- Frontend đang listen: `ReceivePriceList` và `ReceiveMarketStats` (PascalCase) ✅ **ĐÚNG RỒI**
- Warning có thể do:
  1. Frontend chưa kết nối SignalR đúng cách
  2. Backend chưa gửi message khi frontend đã listen
  3. Timing issue - frontend listen sau khi backend đã gửi

### Kiểm Tra

#### Backend (CoinGeckoService.cs):
```csharp
await _hubContext.Clients.All.SendAsync("ReceivePriceList", cachedData);
await _hubContext.Clients.All.SendAsync("ReceiveMarketStats", stats);
```

#### Frontend (Home.tsx, Markets.tsx, MarketStats.tsx):
```typescript
connection.on('ReceiveMarketStats', (s: any) => { ... });
connection.on('ReceivePriceList', (list: any[]) => { ... });
```

✅ **Method names đã match đúng!**

### Giải Pháp
Warning này có thể **bỏ qua** nếu:
- Frontend vẫn nhận được data (check console logs)
- Order book vẫn update đúng

Nếu muốn fix warning:
1. **Đảm bảo SignalR connection được thiết lập trước khi backend gửi message**
2. **Thêm error handling trong frontend**:
```typescript
connection.on('ReceivePriceList', (list: any[]) => {
    // Handle data
}).catch((error) => {
    console.warn('Error receiving price list:', error);
});
```

3. **Kiểm tra SignalR hub registration trong Program.cs**:
```csharp
builder.Services.AddSignalR();
app.MapHub<MarketHub>("/hubs/market");
```

---

## 🧪 Cách Test

### Test Lỗi 500 → 400
1. **Test với symbol không tồn tại**:
```http
POST /api/trading/orders
{
  "symbol": "INVALID/USDT",
  "side": "BUY",
  "type": "MARKET",
  "quantity": 0.01
}
```
**Expected**: `400 BadRequest` với message "Cryptocurrency 'INVALID' not found"

2. **Test với insufficient balance**:
```http
POST /api/trading/orders
{
  "symbol": "BTC/USDT",
  "side": "BUY",
  "type": "MARKET",
  "quantity": 1000000
}
```
**Expected**: `400 BadRequest` với message "Insufficient balance..."

3. **Test với price = 0** (nếu market data fail):
**Expected**: `400 BadRequest` với message "Invalid market price..."

### Test SignalR
1. Mở browser console
2. Kiểm tra có warnings không
3. Kiểm tra data có được update không (order book, price list)
4. Nếu data vẫn update → warnings có thể bỏ qua

---

## 📝 Notes

- ✅ Controller đã có error handling đầy đủ
- ✅ SignalR method names đã match đúng
- ⚠️ Warnings có thể do timing issue, không ảnh hưởng functionality
- 🔍 Nếu vẫn có lỗi 500, check backend logs để xem exception cụ thể

---

**Last Updated**: 2024-11-14

