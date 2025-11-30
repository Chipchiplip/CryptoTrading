# Debug: "Unable to get current price"

## Vấn đề
Bot báo lỗi `"Unable to get current price"` và status chuyển sang "Error".

## Nguyên nhân có thể

### 1. **CoinGecko Service không hoạt động**
- CoinGecko API bị down hoặc rate limit
- API key không hợp lệ hoặc hết quota
- Network issues

### 2. **Symbol không match**
- Bot tìm "ETH" nhưng CoinGecko dùng "ethereum" hoặc "ETH"
- Cache service có symbol khác format

### 3. **Cache service không có data**
- Cache chưa được populate
- Cache service không chạy

### 4. **Market data chưa được load**
- CoinGecko service chưa fetch data lần đầu
- Background service chưa chạy

## Cách kiểm tra

### Bước 1: Kiểm tra Logs chi tiết

Sau khi sửa code, logs sẽ hiển thị:
- Available symbols trong cache
- Available symbols từ CoinGecko API
- Chi tiết lỗi khi fetch

```http
GET http://localhost:5299/api/bots/{botId}/logs?page=1&pageSize=50&level=Warning
```

Tìm logs có:
- `"Asset ETH not found in cache. Available symbols..."`
- `"Asset ETH not found in API. Available symbols..."`
- `"CoinGecko service returned empty market data"`

### Bước 2: Kiểm tra CoinGecko Service

Kiểm tra xem CoinGecko service có đang chạy và có data không:

```http
GET http://localhost:5299/api/market/crypto
```

Hoặc kiểm tra logs của application để xem CoinGecko service có lỗi không.

### Bước 3: Kiểm tra Cache Service

Cache service cần được populate trước. Kiểm tra:
- Background service có đang chạy không
- Cache có được update định kỳ không

### Bước 4: Kiểm tra Symbol Format

Bot đang tìm symbol "ETH" nhưng có thể cần:
- "ethereum" (CoinGecko ID)
- "ETH" (Symbol)
- "ETHUSD" (Trading pair)

## Giải pháp

### Giải pháp 1: Đảm bảo CoinGecko Service hoạt động

1. Kiểm tra `appsettings.json` có CoinGecko API key không
2. Kiểm tra network connection
3. Kiểm tra rate limits

### Giải pháp 2: Sửa Symbol trong Bot

Nếu logs cho thấy symbol không match, có thể cần:
1. Update bot để dùng đúng symbol format
2. Hoặc sửa code để normalize symbol tốt hơn

### Giải pháp 3: Restart Services

1. Restart application để reload cache
2. Đợi background service populate cache
3. Nudge bot lại

### Giải pháp 4: Manual Price Fallback (Temporary)

Nếu cần bot chạy ngay, có thể thêm hardcoded price fallback trong code (chỉ dùng để test):

```csharp
// Fallback to hardcoded prices if API fails (for testing only)
if (price == 0 && baseAsset.Equals("ETH", StringComparison.OrdinalIgnoreCase))
{
    _logger.LogWarning("Using fallback price for ETH: $3000");
    return 3000m;
}
```

## Logs mẫu (Sau khi sửa)

### Log tốt (Tìm thấy price):
```
[Info] Found price in cache: ETH = 3155.23
[Info] Got price from GetMidPriceAsync: $3155.23
```

### Log xấu (Không tìm thấy):
```
[Warning] Asset ETH not found in cache. Available symbols (first 10): BTC, ETH, BNB, SOL, XRP, ADA, DOGE, TRX, MATIC, DOT
[Warning] Asset ETH not found in API. Available symbols (first 10): BTC(bitcoin), ETH(ethereum), ...
[Error] Unable to get price for ETH/USD. GetMidPriceAsync returned 0, OHLCV returned 0 candles
```

## Next Steps

1. **Xem logs** để biết chính xác vấn đề
2. **Kiểm tra CoinGecko service** có hoạt động không
3. **Kiểm tra cache** có data không
4. **Restart bot** sau khi fix

## Test Commands

### Test Market Data API:
```http
GET http://localhost:5299/api/market/crypto
```

### Test Bot với nudge:
```http
POST http://localhost:5299/api/bots/{botId}/nudge
```

### Xem logs ngay sau nudge:
```http
GET http://localhost:5299/api/bots/{botId}/logs?page=1&pageSize=20
```

