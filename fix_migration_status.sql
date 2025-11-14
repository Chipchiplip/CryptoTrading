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

-- 4. Tạo bảng TradingConfigurations (nếu chưa tồn tại)
CREATE TABLE IF NOT EXISTS `TradingConfigurations` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ConfigKey` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
    `ConfigValue` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
    `Description` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL,
    `Environment` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    `UpdatedBy` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_TradingConfigurations_ConfigKey_Environment` (`ConfigKey`, `Environment`)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;

-- 5. Seed default configurations (nếu chưa có)
INSERT INTO `TradingConfigurations` (`ConfigKey`, `ConfigValue`, `Description`, `Environment`, `UpdatedAt`, `UpdatedBy`)
VALUES 
    ('FeeRate', '0.001', 'Trading fee rate (0.1% = 0.001)', 'Development', NOW(), 'System'),
    ('MarketPriceBuffer', '0.05', 'Market order price buffer (5% = 0.05)', 'Development', NOW(), 'System'),
    ('FeeRate', '0.001', 'Trading fee rate (0.1% = 0.001)', 'Staging', NOW(), 'System'),
    ('MarketPriceBuffer', '0.05', 'Market order price buffer (5% = 0.05)', 'Staging', NOW(), 'System'),
    ('FeeRate', '0.001', 'Trading fee rate (0.1% = 0.001)', 'Production', NOW(), 'System'),
    ('MarketPriceBuffer', '0.05', 'Market order price buffer (5% = 0.05)', 'Production', NOW(), 'System')
AS new_values
ON DUPLICATE KEY UPDATE 
    `ConfigValue` = new_values.`ConfigValue`,
    `UpdatedAt` = NOW();

-- 6. Verify configurations
SELECT * FROM TradingConfigurations ORDER BY Environment, ConfigKey;


