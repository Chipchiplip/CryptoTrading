# Hướng Dẫn Debug Lỗi 500 Khi Đặt Order

## 🔍 Vấn Đề

Khi đặt order, bạn nhận được lỗi 500 với message: "An error occurred while placing the order. Please try again."

## ✅ Đã Cải Thiện

1. **Error Handling trong Controller**: 
   - Bây giờ trả về exception type và message chi tiết
   - Trong Development mode, trả về stack trace và inner exception

2. **Logging**: 
   - Log exception type và message đầy đủ
   - Dễ dàng tìm trong backend logs

## 🔧 Cách Debug

### Bước 1: Kiểm Tra Error Response

Khi gặp lỗi 500, check response body trong browser console hoặc Network tab:

**Development Mode:**
```json
{
  "message": "An error occurred while placing the order. Please try again.",
  "error": "Exception message here",
  "type": "ExceptionTypeName",
  "stackTrace": "...",
  "innerException": {
    "message": "...",
    "type": "..."
  }
}
```

**Production Mode:**
```json
{
  "message": "An error occurred while placing the order. Please try again.",
  "error": "Exception message here",
  "type": "ExceptionTypeName"
}
```

### Bước 2: Kiểm Tra Backend Logs

Check backend console/terminal logs để xem chi tiết:

```
[Error] Unexpected error placing order: ExceptionTypeName - Exception message
```

### Bước 3: Các Exception Thường Gặp

#### 1. **DbUpdateException** hoặc **MySqlException**
**Nguyên nhân**: Database connection issue hoặc constraint violation

**Cách fix**:
- Kiểm tra database connection string
- Kiểm tra database có đang chạy không
- Kiểm tra migration đã chạy chưa
- Kiểm tra foreign key constraints

**Ví dụ**:
```json
{
  "type": "DbUpdateException",
  "error": "Cannot insert duplicate key row..."
}
```

#### 2. **NullReferenceException**
**Nguyên nhân**: Object null không được check

**Cách fix**:
- Check backend logs để xem object nào null
- Thường là: `_configService`, `_context`, hoặc database entity

**Ví dụ**:
```json
{
  "type": "NullReferenceException",
  "error": "Object reference not set to an instance of an object"
}
```

#### 3. **InvalidOperationException**
**Nguyên nhân**: Đã được handle, nhưng có thể xảy ra trong transaction

**Cách fix**:
- Check message cụ thể trong error response
- Thường là: insufficient balance, invalid symbol, etc.

#### 4. **ArgumentException**
**Nguyên nhân**: Invalid parameters

**Cách fix**:
- Check request body
- Validate symbol format, quantity, price

#### 5. **TimeoutException** hoặc **TaskCanceledException**
**Nguyên nhân**: Database query timeout hoặc API call timeout

**Cách fix**:
- Kiểm tra database performance
- Kiểm tra network connection
- Tăng timeout nếu cần

### Bước 4: Kiểm Tra Các Điểm Có Thể Lỗi

#### 1. **Configuration Service**
```csharp
var feeRate = await _configService.GetFeeRateAsync();
var marketPriceBuffer = await _configService.GetMarketPriceBufferAsync();
```
- Check xem `TradingConfigurations` table có data không
- Check xem service có được register trong DI không

#### 2. **Parse Symbol**
```csharp
var (coinSymbol, quoteSymbol) = ParseSymbol(request.Symbol);
```
- Symbol phải có format: "BTC/USDT" hoặc "BTC-USDT"
- Không được null hoặc empty

#### 3. **Database Query**
```csharp
var crypto = await _context.Cryptocurrencies
    .FirstOrDefaultAsync(c => c.Symbol.ToUpper() == coinSymbol.ToUpper());
```
- Cryptocurrency phải tồn tại trong database
- Check xem có seed data chưa

#### 4. **Get Market Price**
```csharp
currentPrice = await GetMarketPriceAsync(coinSymbol, _marketOrderCacheTTL);
```
- CoinGecko API có thể fail
- Cache có thể empty
- Network issue

#### 5. **Get or Create Wallet**
```csharp
wallet = await GetOrCreateWalletAsync(userId, "FIAT", "USD", null);
```
- Database transaction có thể fail
- Foreign key constraint violation

#### 6. **Lock Wallet**
```csharp
var lockedWallet = await _context.LockWalletForUpdateAsync(wallet.Id);
```
- MySQL `SELECT ... FOR UPDATE` có thể fail
- Transaction isolation level issue

#### 7. **Save Changes**
```csharp
await _context.SaveChangesAsync();
```
- Constraint violations
- Foreign key violations
- Database connection lost

## 🛠️ Cách Fix Cụ Thể

### Fix 1: Kiểm Tra Database Connection

```bash
# Test database connection
mysql -u your_username -p crypto_trading -e "SELECT 1;"
```

### Fix 2: Kiểm Tra Migrations

```bash
# Check migration status
dotnet ef migrations list

# Apply migrations nếu cần
dotnet ef database update
```

### Fix 3: Kiểm Tra Seed Data

```sql
-- Check cryptocurrencies
SELECT COUNT(*) FROM Cryptocurrencies;

-- Check trading configurations
SELECT * FROM TradingConfigurations;

-- Check wallets
SELECT * FROM Wallets WHERE UserId = YOUR_USER_ID;
```

### Fix 4: Kiểm Tra Service Registration

Check `Program.cs`:
```csharp
builder.Services.AddScoped<ITradingConfigurationService, TradingConfigurationService>();
builder.Services.AddScoped<ITradingService, TradingService>();
```

### Fix 5: Test API Trực Tiếp

```bash
# Test với curl hoặc Postman
curl -X POST http://localhost:5186/api/trading/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "symbol": "BTC/USDT",
    "side": "BUY",
    "type": "MARKET",
    "quantity": 0.01
  }'
```

## 📝 Checklist Debug

- [ ] Check error response body (type, message, stackTrace)
- [ ] Check backend logs
- [ ] Check database connection
- [ ] Check migrations applied
- [ ] Check seed data exists
- [ ] Check service registration
- [ ] Check network/API connectivity
- [ ] Test với Postman/curl
- [ ] Check user có balance không
- [ ] Check symbol format đúng không

## 🚨 Nếu Vẫn Không Fix Được

1. **Copy full error response** (bao gồm stackTrace nếu có)
2. **Copy backend logs** (full exception details)
3. **Check database logs** (nếu có)
4. **Test với simple request** (ví dụ: GET /api/trading/balances)

## 📚 Tài Liệu Tham Khảo

- `FIX_500_ERROR_AND_SIGNALR.md` - Fix lỗi 500 ban đầu
- `CHANGES_DEBUG_TRADING_BRANCH.md` - Tổng hợp thay đổi
- Backend logs trong console/terminal

---

**Last Updated**: 2024-11-14

