# Fix Migration - Database đã có bảng AuditEvents

## Vấn đề
Database đã có các bảng từ migration `AddAuditAndConfiguration` nhưng EF Core không nhận ra migration đã chạy.

## Giải pháp

### Bước 1: Đánh dấu migration đã hoàn thành

Kết nối MySQL client và chạy:

```sql
-- Kiểm tra migration history hiện tại
SELECT * FROM `__EFMigrationsHistory` ORDER BY MigrationId;

-- Thêm migration record
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251113035659_AddAuditAndConfiguration', '9.0.10');
```

### Bước 2: Verify

```sql
-- Kiểm tra lại
SELECT * FROM `__EFMigrationsHistory` ORDER BY MigrationId;
```

### Bước 3: Seed default configurations

Chạy SQL script:

```bash
# Nếu dùng MySQL client
mysql -h cryptotrading-01-phantrunghieu0000-ad84.g.aivencloud.com \
      -P 20158 \
      -u avnadmin \
      -p \
      --ssl-mode=REQUIRED \
      crypto_trading < database/mysql/004_SeedDefaultConfigurations.sql
```

Hoặc copy nội dung file `database/mysql/004_SeedDefaultConfigurations.sql` và chạy trong MySQL Workbench/DBeaver.

### Bước 4: Verify application

```bash
dotnet run
```

Check logs để đảm bảo application start thành công.

## Alternative: Reset và chạy lại migration

Nếu muốn chạy lại từ đầu:

```sql
-- 1. Drop các bảng mới
DROP TABLE IF EXISTS `ClientOrderIdempotency`;
DROP TABLE IF EXISTS `ReconciliationResults`;
DROP TABLE IF EXISTS `BotRiskConfigurations`;
DROP TABLE IF EXISTS `TradingConfigurations`;
DROP TABLE IF EXISTS `FeeLedger`;
DROP TABLE IF EXISTS `DepositTransactions`;
DROP TABLE IF EXISTS `AuditEvents`;

-- 2. Xóa migration record (nếu có)
DELETE FROM `__EFMigrationsHistory` 
WHERE `MigrationId` = '20251113035659_AddAuditAndConfiguration';
```

Sau đó chạy:

```bash
dotnet ef database update
```

## Verify sau khi fix

```sql
-- Check tables
SHOW TABLES;

-- Check configurations
SELECT * FROM TradingConfigurations ORDER BY Environment, ConfigKey;

-- Check migration history
SELECT * FROM `__EFMigrationsHistory` ORDER BY MigrationId;
```



