-- =====================================================
-- FIX MIGRATION STATUS
-- Đánh dấu migration AddAuditAndConfiguration đã hoàn thành
-- =====================================================

-- 1. Kiểm tra migration history hiện tại
SELECT * FROM `__EFMigrationsHistory` ORDER BY MigrationId;

-- 2. Đánh dấu migration đã hoàn thành
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251113035659_AddAuditAndConfiguration', '9.0.10')
ON DUPLICATE KEY UPDATE `ProductVersion` = '9.0.10';

-- 3. Verify
SELECT * FROM `__EFMigrationsHistory` ORDER BY MigrationId;

-- 4. Seed default configurations (nếu chưa có)
INSERT INTO `TradingConfigurations` (`ConfigKey`, `ConfigValue`, `Description`, `Environment`, `UpdatedAt`, `UpdatedBy`)
VALUES 
    ('FeeRate', '0.001', 'Trading fee rate (0.1% = 0.001)', 'Development', NOW(), 'System'),
    ('MarketPriceBuffer', '0.05', 'Market order price buffer (5% = 0.05)', 'Development', NOW(), 'System'),
    ('FeeRate', '0.001', 'Trading fee rate (0.1% = 0.001)', 'Staging', NOW(), 'System'),
    ('MarketPriceBuffer', '0.05', 'Market order price buffer (5% = 0.05)', 'Staging', NOW(), 'System'),
    ('FeeRate', '0.001', 'Trading fee rate (0.1% = 0.001)', 'Production', NOW(), 'System'),
    ('MarketPriceBuffer', '0.05', 'Market order price buffer (5% = 0.05)', 'Production', NOW(), 'System')
ON DUPLICATE KEY UPDATE 
    `ConfigValue` = VALUES(`ConfigValue`),
    `UpdatedAt` = NOW();

-- 5. Verify configurations
SELECT * FROM TradingConfigurations ORDER BY Environment, ConfigKey;


