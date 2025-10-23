# 🎯 Hướng dẫn Deploy Database cho CryptoTrading

## ✅ Đã hoàn thành

Tất cả code đã sẵn sàng! Bạn chỉ cần chạy migration vào database.

## 🔧 Cách xử lý:

### **Option 1: Xóa tables hiện có và chạy lại migration (KHUYẾN NGHỊ)**

1. **Mở MySQL Workbench 8.0 CE**
2. **Kết nối tới database `crypto_trading`**
3. **Chạy các lệnh sau để xóa tables đã tạo một phần:**

```sql
USE crypto_trading;

-- Drop tables nếu tồn tại (theo thứ tự đúng vì có foreign keys)
DROP TABLE IF EXISTS CryptoPrices;
DROP TABLE IF EXISTS Cryptocurrencies;
DROP TABLE IF EXISTS MarketStats;
DROP TABLE IF EXISTS Users;

-- Xóa bảng migration history nếu có
DROP TABLE IF EXISTS __EFMigrationsHistory;
```

4. **Quay lại terminal và chạy migration:**
```bash
cd C:\Users\nhuut\Downloads\CryptoTrading
dotnet ef database update
```

### **Option 2: Sử dụng database mới (NẾU MUỐN)**

1. Tạo database mới trong MySQL:
```sql
CREATE DATABASE crypto_trading_new CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

2. Update connection string trong `appsettings.Development.json`:
```json
"DefaultConnection": "server=...;database=crypto_trading_new;user=...;password=..."
```

3. Chạy migration:
```bash
dotnet ef database update
```

## 🚀 Sau khi Migration thành công:

### 1. Chạy ứng dụng:
```bash
# Backend
dotnet run

# Frontend (terminal khác)
cd frontend
npm start
```

### 2. Kiểm tra Background Service đang sync:
- Xem logs trong terminal backend
- Sau 10 giây, sẽ thấy logs như:
  ```
  info: Starting cryptocurrency sync...
  info: Cryptocurrency sync completed. Synced 100 cryptocurrencies
  info: Starting price sync...
  info: Price sync completed. Saved 100 price records
  ```

### 3. Kiểm tra database trong MySQL Workbench:
```sql
USE crypto_trading;

-- Xem tất cả tables
SHOW TABLES;

-- Kiểm tra cryptocurrencies
SELECT COUNT(*) as TotalCoins FROM Cryptocurrencies;
SELECT * FROM Cryptocurrencies ORDER BY Id LIMIT 10;

-- Kiểm tra prices
SELECT COUNT(*) as TotalPrices FROM CryptoPrices;
SELECT 
    c.Symbol, 
    cp.PriceUsd, 
    cp.PercentChange24h,
    cp.CollectedAtUtc 
FROM CryptoPrices cp
JOIN Cryptocurrencies c ON cp.CryptocurrencyId = c.Id
ORDER BY cp.CollectedAtUtc DESC
LIMIT 20;

-- Kiểm tra market stats
SELECT * FROM MarketStats ORDER BY CollectedAtUtc DESC LIMIT 5;
```

## 📊 Kết quả mong đợi:

Sau khi chạy thành công, bạn sẽ có:

✅ **Cryptocurrencies** table với ~100 coins từ CoinGecko
✅ **CryptoPrices** table với giá real-time (mỗi 5 phút thêm 100 records mới)
✅ **MarketStats** table với thống kê thị trường global
✅ **Users** table cho authentication

## ⚙️ Background Service hoạt động:

- **Khởi động**: Sau 10 giây khi app start
- **Tần suất**: Mỗi 5 phút
- **Thứ tự sync**:
  1. Sync Cryptocurrencies (thêm coins mới nếu có)
  2. Sync Prices (lưu giá hiện tại)
  3. Sync Market Stats (lưu thống kê global)

## 🐛 Troubleshooting:

### Nếu migration vẫn báo lỗi "Table already exists":
```sql
-- Trong MySQL Workbench, xóa hết:
DROP DATABASE crypto_trading;
CREATE DATABASE crypto_trading CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

Sau đó chạy lại migration.

### Nếu không kết nối được database:
- Kiểm tra connection string trong `appsettings.Development.json`
- Đảm bảo MySQL server đang chạy
- Kiểm tra username/password đúng
- Thử ping server: `ping cryptotrading-01-phantrunghieu0000-ad84.g.aivencloud.com`

---

**🎉 Sau khi làm xong, bạn sẽ có hệ thống tự động lưu giá crypto vào database mỗi 5 phút!**

