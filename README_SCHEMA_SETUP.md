# Database Schema Setup Guide

## Quick Start: Sử dụng Database Cũ (INT UserId) - Tương thích với Code

### Bước 1: Set Environment Variable

**Windows (PowerShell):**
```powershell
$env:USE_OLD_SCHEMA="true"
dotnet run
```

**Windows (CMD):**
```cmd
set USE_OLD_SCHEMA=true
dotnet run
```

**Linux/Mac:**
```bash
export USE_OLD_SCHEMA=true
dotnet run
```

**Hoặc trong `appsettings.json` hoặc `launchSettings.json`:**
```json
{
  "EnvironmentVariables": {
    "USE_OLD_SCHEMA": "true"
  }
}
```

### Bước 2: Chạy Application

```bash
dotnet run
```

Bạn sẽ thấy log:
```
📌 Using OLD SCHEMA (INT UserId) - Compatible with existing code
```

### Bước 3: Verify

Kiểm tra database schema:
```sql
USE crypto_trading;
DESCRIBE Users;
-- Should show: Id INT AUTO_INCREMENT PRIMARY KEY

DESCRIBE Orders;
-- Should show: UserId INT NOT NULL
```

---

## So sánh 2 Schemas

### OLD SCHEMA (INT UserId) ✅ - Tương thích với Code

**Đặc điểm:**
- `Users.Id` = `INT AUTO_INCREMENT`
- `Orders.UserId` = `INT`
- `Wallets.UserId` = `INT`
- `TradingBots.UserId` = `INT`

**Ưu điểm:**
- ✅ Code hiện tại không cần thay đổi
- ✅ EF Core migrations đã match
- ✅ Dễ migrate từ database cũ

**Cách dùng:**
```bash
export USE_OLD_SCHEMA=true
dotnet run
```

---

### NEW SCHEMA (CHAR(36) UserId) ⚠️ - Cần Update Code

**Đặc điểm:**
- `auth_users.Id` = `CHAR(36)` (GUID)
- `trading_orders.UserId` = `CHAR(36)`
- `wallets.UserId` = `CHAR(36)`

**Ưu điểm:**
- ✅ Better for distributed systems
- ✅ No auto-increment conflicts
- ✅ More scalable

**Nhược điểm:**
- ❌ Code phải update (int → string)
- ❌ Phải migrate data

**Cách dùng:**
```bash
# Không set USE_OLD_SCHEMA (hoặc set = false)
dotnet run
```

---

## Files Quan Trọng

### SQL Scripts

1. **`database/mysql/000_CheckCurrentSchema.sql`**
   - Check schema hiện tại đang dùng

2. **`database/mysql/001_InitialSchema_MySQL_OLD_SCHEMA.sql`**
   - Schema cũ với INT UserId (tương thích code)

3. **`database/mysql/001_InitialSchema_MySQL.sql`**
   - Schema mới với CHAR(36) UserId (cần update code)

### Code Files

- **`Program.cs`** - Auto-detect schema dựa trên `USE_OLD_SCHEMA` env var
- **`Models/User.cs`** - `Id` là `int` (match OLD SCHEMA)
- **`Models/Order.cs`** - `UserId` là `int` (match OLD SCHEMA)

---

## Troubleshooting

### Lỗi: "Table 'Users' doesn't exist"

**Nguyên nhân:** Database đang dùng NEW SCHEMA (auth_users)

**Giải pháp:**
1. Set `USE_OLD_SCHEMA=true`
2. Chạy lại `dotnet run`
3. Hoặc chạy manual: `mysql < database/mysql/001_InitialSchema_MySQL_OLD_SCHEMA.sql`

### Lỗi: "Cannot convert int to CHAR(36)"

**Nguyên nhân:** Code dùng INT nhưng database dùng CHAR(36)

**Giải pháp:**
1. Check schema: `DESCRIBE Users;`
2. Nếu là CHAR(36) → Set `USE_OLD_SCHEMA=true` và recreate database
3. Hoặc update code để dùng CHAR(36) (xem `docs/DATABASE_COMPATIBILITY_ANALYSIS.md`)

### Lỗi: "Foreign key constraint fails"

**Nguyên nhân:** Type mismatch giữa foreign key và referenced column

**Giải pháp:**
1. Check cả 2 tables có cùng type không
2. Drop và recreate foreign keys với đúng type
3. Đảm bảo dùng cùng schema (OLD hoặc NEW)

---

## Migration Path

### Từ NEW SCHEMA → OLD SCHEMA

**⚠️ Chỉ làm nếu thực sự cần:**

1. Backup database
2. Drop database
3. Set `USE_OLD_SCHEMA=true`
4. Chạy `dotnet run` (sẽ tạo OLD SCHEMA)
5. Restore data (nếu có)

### Từ OLD SCHEMA → NEW SCHEMA

**⚠️ Cần update code:**

1. Update tất cả models (int → string)
2. Update controllers
3. Update services
4. Migrate data
5. Test kỹ

(Xem `docs/DATABASE_COMPATIBILITY_ANALYSIS.md`)

---

## Khuyến Nghị

### ✅ Nên làm:
1. **Giữ OLD SCHEMA** nếu code đã ổn định
2. **Set `USE_OLD_SCHEMA=true`** trong development
3. **Document** schema đang dùng
4. **Test** trước khi deploy

### ❌ Không nên:
1. Mix 2 schemas
2. Thay đổi schema khi đã có production data
3. Bỏ qua testing

---

## Quick Reference

| Feature | OLD SCHEMA | NEW SCHEMA |
|---------|------------|------------|
| UserId Type | `INT` | `CHAR(36)` |
| Table Name | `Users` | `auth_users` |
| Code Compatible | ✅ Yes | ❌ No (cần update) |
| Setup | `USE_OLD_SCHEMA=true` | Default |
| SQL Script | `001_InitialSchema_MySQL_OLD_SCHEMA.sql` | `001_InitialSchema_MySQL.sql` |

---

## Kết Luận

**Để giữ database cũ và code không cần thay đổi:**

1. ✅ Set `USE_OLD_SCHEMA=true`
2. ✅ Chạy `dotnet run`
3. ✅ Code sẽ chạy bình thường với INT UserId

**Code của bạn đã tương thích với OLD SCHEMA!** 🎉

