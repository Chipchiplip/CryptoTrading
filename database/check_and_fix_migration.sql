-- =====================================================
-- CHECK AND FIX MIGRATION STATUS
-- =====================================================

-- 1. Kiểm tra các bảng đã tồn tại
SELECT 
    'AuditEvents' as TableName,
    IF(COUNT(*) > 0, 'EXISTS', 'NOT EXISTS') as Status
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'AuditEvents'
UNION ALL
SELECT 
    'FeeLedger',
    IF(COUNT(*) > 0, 'EXISTS', 'NOT EXISTS')
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'FeeLedger'
UNION ALL
SELECT 
    'TradingConfigurations',
    IF(COUNT(*) > 0, 'EXISTS', 'NOT EXISTS')
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'TradingConfigurations'
UNION ALL
SELECT 
    'BotRiskConfigurations',
    IF(COUNT(*) > 0, 'EXISTS', 'NOT EXISTS')
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'BotRiskConfigurations'
UNION ALL
SELECT 
    'ReconciliationResults',
    IF(COUNT(*) > 0, 'EXISTS', 'NOT EXISTS')
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ReconciliationResults'
UNION ALL
SELECT 
    'ClientOrderIdempotency',
    IF(COUNT(*) > 0, 'EXISTS', 'NOT EXISTS')
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ClientOrderIdempotency'
UNION ALL
SELECT 
    'DepositTransactions',
    IF(COUNT(*) > 0, 'EXISTS', 'NOT EXISTS')
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'DepositTransactions';

-- 2. Kiểm tra migration history
SELECT * FROM `__EFMigrationsHistory` ORDER BY MigrationId;

-- 3. Nếu migration chưa được ghi nhận, chạy lệnh này:
-- INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
-- VALUES ('20251113035659_AddAuditAndConfiguration', '9.0.10');



