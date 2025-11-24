# Portfolio API Debugging Guide

## 🐛 Vấn đề hiện tại
`ERR_EMPTY_RESPONSE` khi gọi `/api/portfolio/overview` - Backend không trả về response.

## ✅ Các thay đổi đã thực hiện

### 1. Backend Logging (C#)
- **Controllers/PortfolioController.cs**: Thêm chi tiết logging cho từng bước
- **Services/Portfolio/PortfolioService.cs**: Thêm logging cho các operations nặng

### 2. Frontend Retry & Error Handling (TypeScript)
- **frontend/src/api/portfolio.ts**: 
  - Retry logic (3 attempts)
  - Better error messages
  - Empty response detection

## 🔍 Các bước debug

### Bước 1: Kiểm tra Backend có chạy không
```bash
# Terminal 1 - Backend
cd d:\CODE\Intern\CryptoTrading
dotnet run
```

**Kết quả mong đợi:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5299
      Now listening on: http://localhost:5001
```

### Bước 2: Test API trực tiếp với curl/Postman

#### Option A: Postman
1. Mở Postman
2. Tạo GET request: `https://localhost:5299/api/portfolio/overview`
3. Thêm Header: 
   - `Authorization: Bearer YOUR_JWT_TOKEN`
   (Lấy token từ localStorage trong browser DevTools)
4. Send request

#### Option B: Terminal (PowerShell)
```powershell
# Lấy token từ browser console:
# localStorage.getItem('crypto_trading_access_token')

$token = "YOUR_JWT_TOKEN_HERE"
$headers = @{
    "Authorization" = "Bearer $token"
}

Invoke-WebRequest -Uri "https://localhost:5299/api/portfolio/overview" `
    -Method GET `
    -Headers $headers `
    -UseBasicParsing
```

### Bước 3: Kiểm tra Backend Logs

Khi gọi API, backend sẽ log theo thứ tự:

```
[PortfolioController] GET /api/portfolio/overview - Request received
[PortfolioController] Extracting userId from token...
[PortfolioController] Processing portfolio overview for userId=X
[PortfolioService] GetPortfolioOverviewAsync called for userId=X
[PortfolioService] Fetching wallets...
[PortfolioService] Found X wallets
[PortfolioService] Fetching market data from CoinGecko...
[PortfolioService] Received X market data items
[PortfolioService] Calculating realized PnL...
[PortfolioService] Fetching NAV history...
[PortfolioService] Portfolio overview completed for userId=X
[PortfolioController] Successfully retrieved portfolio overview
```

**Nếu bị stuck ở dòng nào** → Đó là nơi xảy ra lỗi.

### Bước 4: Các lỗi thường gặp và cách fix

#### ❌ Lỗi 1: Backend không log gì cả
**Nguyên nhân:** Request không tới backend (proxy hoặc routing sai)

**Cách fix:**
1. Kiểm tra `frontend/vite.config.ts`:
```typescript
proxy: {
  '/api': {
    target: 'http://localhost:5299',  // Đảm bảo port đúng
    changeOrigin: true,
    secure: false,
  }
}
```

2. Restart frontend:
```bash
cd frontend
npm run dev
```

#### ❌ Lỗi 2: Stuck ở "Fetching market data from CoinGecko..."
**Nguyên nhân:** CoinGecko API bị timeout hoặc rate limit

**Cách fix:**
1. Kiểm tra internet connection
2. Tăng timeout trong `Program.cs`:
```csharp
builder.Services.AddHttpClient<ICoinGeckoService, CoinGeckoService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30); // Tăng từ 10 lên 30
});
```

#### ❌ Lỗi 3: Database connection error
**Nguyên nhân:** MySQL không chạy hoặc connection string sai

**Cách fix:**
```bash
# Kiểm tra MySQL đang chạy
net start MySQL80

# Hoặc qua Services: Win+R → services.msc → tìm MySQL
```

#### ❌ Lỗi 4: 401 Unauthorized
**Nguyên nhân:** Token hết hạn hoặc không hợp lệ

**Cách fix:**
1. Login lại để lấy token mới
2. Kiểm tra token trong localStorage:
```javascript
// Browser console
localStorage.getItem('crypto_trading_access_token')
```

### Bước 5: Test với Frontend

```bash
# Terminal 2 - Frontend
cd frontend
npm run dev
```

Mở browser DevTools (F12) → Console tab → reload trang Portfolio

**Logs mong đợi:**
```
[http] Fetching: /api/portfolio/overview {method: 'GET', hasAuth: true}
[portfolio] Retrying request (attempt 2/3) (nếu lần 1 fail)
[http] Response: /api/portfolio/overview {status: 200, statusText: 'OK'}
```

## 📊 Checklist Debug

- [ ] Backend đang chạy (`dotnet run`)
- [ ] MySQL đang chạy
- [ ] Frontend đang chạy (`npm run dev`)
- [ ] Token hợp lệ (không 401)
- [ ] Proxy config đúng (vite.config.ts)
- [ ] Backend logs hiển thị request
- [ ] CoinGecko API response (<30s)
- [ ] Database queries hoàn thành

## 🚀 Test nhanh

Sau khi restart backend, mở browser console và chạy:

```javascript
// Test API trực tiếp
const token = localStorage.getItem('crypto_trading_access_token');
fetch('/api/portfolio/overview', {
    headers: { 'Authorization': `Bearer ${token}` }
})
.then(r => r.json())
.then(console.log)
.catch(console.error);
```

**Kết quả mong đợi:**
```json
{
  "totalValue": 1000.00,
  "totalCost": 900.00,
  "unrealizedPnL": 100.00,
  "unrealizedPnLPercent": 11.11,
  "realizedPnL": 0,
  "holdings": [...],
  "navHistory": [...]
}
```

## 📝 Report Bug

Nếu vẫn lỗi, copy logs từ:
1. Backend terminal (tất cả dòng có `[PortfolioController]` hoặc `[PortfolioService]`)
2. Browser console (tất cả dòng có `[http]` hoặc `[portfolio]`)
3. Network tab trong DevTools (status, response headers)

## 💡 Quick Fixes

### Fix 1: Restart tất cả
```bash
# Stop backend (Ctrl+C)
# Stop frontend (Ctrl+C)

# Restart backend
dotnet run

# Restart frontend (new terminal)
cd frontend
npm run dev
```

### Fix 2: Clear cache
```javascript
// Browser console
localStorage.clear()
// Login lại
```

### Fix 3: Check database
```sql
-- Kiểm tra user có wallets không
SELECT * FROM Wallets WHERE UserId = YOUR_USER_ID;

-- Kiểm tra movements
SELECT * FROM WalletMovements WHERE WalletId IN (
    SELECT Id FROM Wallets WHERE UserId = YOUR_USER_ID
);
```

## 🎯 Expected Behavior

Khi hoạt động bình thường:
- **Request time**: 5-15 giây (lần đầu)
- **Retry**: Không có (thành công ngay lần 1)
- **Status**: 200 OK
- **Data**: JSON object với holdings và navHistory

Good luck debugging! 🚀

