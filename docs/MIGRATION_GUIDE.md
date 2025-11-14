# Migration Guide - Audit & Configuration Features

## Tổng quan

Migration này thêm các tính năng mới:
- Audit trail system
- Dynamic configuration management
- Fee ledger
- Reconciliation system
- Idempotency support

## Cách chạy migration

### Option 1: Sử dụng EF Core Migration (Khuyến nghị)

Nếu database chưa có các tables mới:

```bash
dotnet ef database update
```

### Option 2: Chạy SQL script trực tiếp

Nếu migration gặp lỗi do tables đã tồn tại, chạy SQL script trực tiếp:

```bash
# Kết nối MySQL và chạy:
mysql -u your_username -p your_database < database/mysql/003_AuditAndConfiguration_MySQL.sql
```

### Option 3: Chạy từng phần

Nếu một số tables đã tồn tại, chỉ cần tạo các tables còn thiếu:

1. Kiểm tra tables nào đã tồn tại:
```sql
SHOW TABLES LIKE 'AuditEvents';
SHOW TABLES LIKE 'FeeLedger';
SHOW TABLES LIKE 'TradingConfigurations';
SHOW TABLES LIKE 'BotRiskConfigurations';
SHOW TABLES LIKE 'ReconciliationResults';
SHOW TABLES LIKE 'ClientOrderIdempotency';
```

2. Chạy script SQL chỉ cho các tables còn thiếu từ file `database/mysql/003_AuditAndConfiguration_MySQL.sql`

## Seed Default Configurations

Sau khi migration thành công, chạy script seed data:

```bash
mysql -u your_username -p your_database < database/mysql/004_SeedDefaultConfigurations.sql
```

Hoặc chạy trực tiếp trong MySQL:

```sql
source database/mysql/004_SeedDefaultConfigurations.sql;
```

## Verify Migration

Kiểm tra các tables đã được tạo:

```sql
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'your_database_name'
AND TABLE_NAME IN (
    'AuditEvents',
    'FeeLedger', 
    'TradingConfigurations',
    'BotRiskConfigurations',
    'ReconciliationResults',
    'ClientOrderIdempotency'
);
```

Kiểm tra configurations đã được seed:

```sql
SELECT * FROM TradingConfigurations ORDER BY Environment, ConfigKey;
```

## Troubleshooting

### Lỗi: "Table already exists"
- Một số tables đã tồn tại từ trước
- Giải pháp: Chỉ tạo các tables còn thiếu hoặc skip các CREATE TABLE statements

### Lỗi: "Cannot drop index: needed in a foreign key constraint"
- Index đang được sử dụng bởi foreign key
- Giải pháp: Migration đã được sửa để skip việc drop các indexes này

### Lỗi: "Foreign key constraint fails"
- Kiểm tra các tables tham chiếu (Users, Orders, TradingBots) đã tồn tại
- Đảm bảo các foreign key columns có đúng kiểu dữ liệu

## Sau khi migration thành công

1. Verify application có thể kết nối database
2. Test các tính năng mới:
   - Place order với `clientOrderId` (idempotency)
   - Kiểm tra audit events được ghi lại
   - Verify dynamic configuration hoạt động
3. Monitor reconciliation background service logs



