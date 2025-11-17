# Fix: Lỗi "Table 'crypto_trading.FeeLedger' doesn't exist"

## 🔴 Vấn Đề

Khi đặt order, gặp lỗi:
```
Table 'crypto_trading.FeeLedger' doesn't exist
```

## ✅ Giải Pháp

Bảng `FeeLedger` chưa được tạo trong database. Cần chạy migration để tạo bảng này.

## 📝 Cách Fix

### Option 1: Chạy SQL Script Trực Tiếp (Nhanh)

```bash
mysql -u your_username -p crypto_trading < database/mysql/005_CreateFeeLedgerTable.sql
```

Hoặc trong MySQL client:
```sql
USE crypto_trading;
SOURCE database/mysql/005_CreateFeeLedgerTable.sql;
```

### Option 2: Chạy Migration 003 (Nếu chưa chạy)

Nếu migration `003_AuditAndConfiguration_MySQL.sql` chưa được chạy:

```bash
mysql -u your_username -p crypto_trading < database/mysql/003_AuditAndConfiguration_MySQL.sql
```

### Option 3: Copy và Paste vào MySQL Workbench

1. Mở file `database/mysql/005_CreateFeeLedgerTable.sql`
2. Copy toàn bộ nội dung
3. Paste vào MySQL Workbench và chạy

## ✅ Kiểm Tra Sau Khi Fix

```sql
-- Kiểm tra bảng đã được tạo
SHOW TABLES LIKE 'FeeLedger';

-- Kiểm tra cấu trúc bảng
DESCRIBE FeeLedger;

-- Kiểm tra indexes
SHOW INDEXES FROM FeeLedger;
```

**Expected Output:**
- Bảng `FeeLedger` tồn tại
- Có các columns: `Id`, `UserId`, `TradeId`, `FeeType`, `FeeAmount`, `FeeCurrency`, `CreatedAt`
- Có indexes: `IX_FeeLedger_UserId_CreatedAt`, `IX_FeeLedger_TradeId`
- Có foreign key: `FK_FeeLedger_Users_UserId`

## 🔍 Giải Thích

Bảng `FeeLedger` được dùng để:
- Track fees từ các trades
- Record fee type (TRADING, WITHDRAWAL, DEPOSIT)
- Record fee amount và currency
- Link với User và Trade

Code trong `TradingService.cs` sẽ tự động tạo records trong `FeeLedger` khi execute trades.

## 📚 Tài Liệu Tham Khảo

- `database/mysql/003_AuditAndConfiguration_MySQL.sql` - Migration gốc
- `database/mysql/005_CreateFeeLedgerTable.sql` - Script fix nhanh
- `Models/AuditEvent.cs` - FeeLedger model definition

---

**Last Updated**: 2024-11-14

