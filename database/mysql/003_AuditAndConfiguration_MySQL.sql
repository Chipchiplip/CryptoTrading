-- =====================================================
-- CRYPTO TRADING PLATFORM - AUDIT & CONFIGURATION MIGRATION
-- Version: 1.1.0
-- Date: November 2025
-- Description: Adds audit trail, configuration management, 
--              idempotency, reconciliation, and enhanced risk management
-- =====================================================

-- =====================================================
-- 1. AUDIT EVENTS TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS `AuditEvents` (
    `Id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `EventType` VARCHAR(50) NOT NULL COMMENT 'ORDER_PLACED, TRADE_EXECUTED, BALANCE_CHANGED, etc.',
    `UserId` INT NOT NULL,
    `BotId` CHAR(36) NULL,
    `CorrelationId` VARCHAR(50) NOT NULL,
    `EntityType` VARCHAR(50) NOT NULL COMMENT 'Order, Trade, Wallet, etc.',
    `EntityId` BIGINT UNSIGNED NULL,
    `BeforeState` JSON NULL COMMENT 'State before operation',
    `AfterState` JSON NULL COMMENT 'State after operation',
    `Metadata` TEXT NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `IpAddress` VARCHAR(100) NULL,
    `UserAgent` VARCHAR(500) NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_AuditEvents_UserId_CreatedAt` (`UserId`, `CreatedAt`),
    INDEX `IX_AuditEvents_CorrelationId` (`CorrelationId`),
    INDEX `IX_AuditEvents_EntityType_EntityId` (`EntityType`, `EntityId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Complete audit trail for all critical operations';

-- =====================================================
-- 2. FEE LEDGER TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS `FeeLedger` (
    `Id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `UserId` INT NOT NULL,
    `TradeId` BIGINT UNSIGNED NULL,
    `FeeType` VARCHAR(10) NOT NULL COMMENT 'TRADING, WITHDRAWAL, DEPOSIT',
    `FeeAmount` DECIMAL(30,10) NOT NULL,
    `FeeCurrency` VARCHAR(10) NOT NULL COMMENT 'USD, BTC, ETH, etc.',
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    INDEX `IX_FeeLedger_UserId_CreatedAt` (`UserId`, `CreatedAt`),
    INDEX `IX_FeeLedger_TradeId` (`TradeId`),
    FOREIGN KEY (`UserId`) REFERENCES `Users`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Detailed fee tracking by currency type';

-- =====================================================
-- 3. TRADING CONFIGURATION TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS `TradingConfigurations` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ConfigKey` VARCHAR(50) NOT NULL,
    `ConfigValue` VARCHAR(255) NOT NULL,
    `Description` VARCHAR(500) NULL,
    `Environment` VARCHAR(20) NOT NULL DEFAULT 'Production' COMMENT 'Development, Sandbox, Production',
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    `UpdatedBy` VARCHAR(100) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_TradingConfigurations_ConfigKey_Environment` (`ConfigKey`, `Environment`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Dynamic trading system configuration by environment';

-- =====================================================
-- 4. BOT RISK CONFIGURATION TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS `BotRiskConfigurations` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `UserId` INT NULL COMMENT 'NULL for global defaults',
    `BotId` CHAR(36) NULL COMMENT 'NULL for user defaults',
    `MaxAllowedCapital` DECIMAL(30,10) NOT NULL DEFAULT 100000,
    `MaxSlippage` DECIMAL(10,4) NOT NULL DEFAULT 0.05 COMMENT '5%',
    `MaxDailyLoss` DECIMAL(10,4) NOT NULL DEFAULT 0.10 COMMENT '10%',
    `MaxConsecutiveLosses` INT NOT NULL DEFAULT 5,
    `CooldownSeconds` INT NOT NULL DEFAULT 300 COMMENT '5 minutes',
    `KillSwitchEnabled` BOOLEAN NOT NULL DEFAULT TRUE,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NULL ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    INDEX `IX_BotRiskConfigurations_UserId_BotId` (`UserId`, `BotId`),
    FOREIGN KEY (`UserId`) REFERENCES `Users`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Per-user and per-bot risk management configuration';

-- =====================================================
-- 5. RECONCILIATION RESULTS TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS `ReconciliationResults` (
    `Id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `ReconciliationTime` DATETIME(6) NOT NULL,
    `EntityType` VARCHAR(50) NOT NULL COMMENT 'ORDERS_TRADES, WALLET_BALANCES, etc.',
    `TotalChecked` INT NOT NULL,
    `MismatchCount` INT NOT NULL,
    `Mismatches` JSON NULL COMMENT 'Detailed mismatch information',
    `Status` VARCHAR(20) NOT NULL DEFAULT 'COMPLETED' COMMENT 'RUNNING, COMPLETED, FAILED',
    `ErrorMessage` TEXT NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    INDEX `IX_ReconciliationResults_EntityType_ReconciliationTime` (`EntityType`, `ReconciliationTime`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Automated reconciliation results and mismatch tracking';

-- =====================================================
-- 6. CLIENT ORDER IDEMPOTENCY TABLE
-- =====================================================
CREATE TABLE IF NOT EXISTS `ClientOrderIdempotency` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ClientOrderId` VARCHAR(100) NOT NULL,
    `UserId` INT NOT NULL,
    `OrderId` BIGINT UNSIGNED NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_ClientOrderIdempotency_ClientOrderId` (`ClientOrderId`),
    INDEX `IX_ClientOrderIdempotency_UserId_CreatedAt` (`UserId`, `CreatedAt`),
    FOREIGN KEY (`UserId`) REFERENCES `Users`(`Id`) ON DELETE CASCADE,
    FOREIGN KEY (`OrderId`) REFERENCES `Orders`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Ensures idempotent order placement';

-- =====================================================
-- 7. SEED DEFAULT CONFIGURATIONS
-- =====================================================

-- Default Trading Configurations
INSERT INTO `TradingConfigurations` (`ConfigKey`, `ConfigValue`, `Description`, `Environment`, `UpdatedAt`)
VALUES 
    ('FeeRate', '0.001', 'Trading fee rate (0.1%)', 'Production', NOW()),
    ('MarketPriceBuffer', '0.05', 'Market order price buffer (5%)', 'Production', NOW()),
    ('FeeRate', '0.0005', 'Trading fee rate (0.05%) - Reduced for testing', 'Sandbox', NOW()),
    ('MarketPriceBuffer', '0.10', 'Market order price buffer (10%) - Higher for safety', 'Sandbox', NOW()),
    ('FeeRate', '0', 'No fees in development', 'Development', NOW()),
    ('MarketPriceBuffer', '0.10', 'Market order price buffer (10%)', 'Development', NOW())
ON DUPLICATE KEY UPDATE 
    `ConfigValue` = VALUES(`ConfigValue`),
    `Description` = VALUES(`Description`),
    `UpdatedAt` = NOW();

-- Global Default Bot Risk Configuration
INSERT INTO `BotRiskConfigurations` (`UserId`, `BotId`, `MaxAllowedCapital`, `MaxSlippage`, `MaxDailyLoss`, `MaxConsecutiveLosses`, `CooldownSeconds`, `KillSwitchEnabled`, `CreatedAt`)
VALUES 
    (NULL, NULL, 100000, 0.05, 0.10, 5, 300, TRUE, NOW())
ON DUPLICATE KEY UPDATE 
    `MaxAllowedCapital` = VALUES(`MaxAllowedCapital`),
    `MaxSlippage` = VALUES(`MaxSlippage`),
    `MaxDailyLoss` = VALUES(`MaxDailyLoss`),
    `MaxConsecutiveLosses` = VALUES(`MaxConsecutiveLosses`),
    `CooldownSeconds` = VALUES(`CooldownSeconds`),
    `KillSwitchEnabled` = VALUES(`KillSwitchEnabled`);

-- =====================================================
-- 8. ADD CONSTRAINTS TO EXISTING TABLES (IF NOT EXISTS)
-- =====================================================

-- Add check constraint for wallet movements (prevent extreme negative balances)
-- Note: MySQL doesn't support CHECK constraints in older versions, but 8.0+ does
ALTER TABLE `WalletMovements` 
ADD CONSTRAINT `CHK_WalletMovements_ReasonableAmount` 
CHECK (`Amount` > -1000000 AND `Amount` < 1000000)
;

-- Add check constraint for order holds
ALTER TABLE `OrderHolds`
ADD CONSTRAINT `CHK_OrderHolds_PositiveAmount`
CHECK (`Amount` >= 0)
;

-- =====================================================
-- 9. CREATE INDEXES FOR PERFORMANCE
-- =====================================================

-- Optimize wallet balance queries
CREATE INDEX IF NOT EXISTS `IX_WalletMovements_WalletId_CreatedAt` 
ON `WalletMovements`(`WalletId`, `CreatedAt`);

-- Optimize order hold queries
CREATE INDEX IF NOT EXISTS `IX_OrderHolds_WalletId_ReleasedAt` 
ON `OrderHolds`(`WalletId`, `ReleasedAt`);

-- Optimize order matching queries
CREATE INDEX IF NOT EXISTS `IX_Orders_CryptocurrencyId_Side_Type_Status` 
ON `Orders`(`CryptocurrencyId`, `Side`, `Type`, `Status`);

-- =====================================================
-- 10. VERIFICATION QUERIES
-- =====================================================

-- Verify tables were created
SELECT 
    TABLE_NAME, 
    TABLE_ROWS, 
    CREATE_TIME 
FROM 
    INFORMATION_SCHEMA.TABLES 
WHERE 
    TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME IN (
        'AuditEvents', 
        'FeeLedger', 
        'TradingConfigurations', 
        'BotRiskConfigurations', 
        'ReconciliationResults', 
        'ClientOrderIdempotency'
    )
ORDER BY 
    TABLE_NAME;

-- Verify default configurations
SELECT * FROM `TradingConfigurations` ORDER BY `Environment`, `ConfigKey`;
SELECT * FROM `BotRiskConfigurations` WHERE `UserId` IS NULL AND `BotId` IS NULL;

-- =====================================================
-- ROLLBACK SCRIPT (Use with caution!)
-- =====================================================

/*
-- To rollback this migration (use with extreme caution in production):

DROP TABLE IF EXISTS `ClientOrderIdempotency`;
DROP TABLE IF EXISTS `ReconciliationResults`;
DROP TABLE IF EXISTS `BotRiskConfigurations`;
DROP TABLE IF EXISTS `TradingConfigurations`;
DROP TABLE IF EXISTS `FeeLedger`;
DROP TABLE IF EXISTS `AuditEvents`;

ALTER TABLE `WalletMovements` DROP CONSTRAINT IF EXISTS `CHK_WalletMovements_ReasonableAmount`;
ALTER TABLE `OrderHolds` DROP CONSTRAINT IF EXISTS `CHK_OrderHolds_PositiveAmount`;
*/

-- =====================================================
-- END OF MIGRATION
-- =====================================================

