-- =====================================================
-- RISK MANAGEMENT TABLES MIGRATION
-- Adds tables for Kill Switch, Risk State, and Capital Limits
-- =====================================================

USE CryptoTradingDB;

-- 1. Create KillSwitchEvents table
CREATE TABLE IF NOT EXISTS `KillSwitchEvents` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `BotId` INT NOT NULL,
    `TriggerReason` VARCHAR(500) NOT NULL,
    `TriggerTime` DATETIME(6) NOT NULL,
    `TotalLoss` DECIMAL(18,8) NULL,
    `ConsecutiveLosses` INT NULL,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT `FK_KillSwitchEvents_TradingBots`
        FOREIGN KEY (`BotId`) REFERENCES `TradingBots`(`Id`) ON DELETE CASCADE,
    INDEX `IX_KillSwitchEvents_BotId` (`BotId`),
    INDEX `IX_KillSwitchEvents_TriggerTime` (`TriggerTime`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 2. Create BotRiskState table
CREATE TABLE IF NOT EXISTS `BotRiskStates` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `BotId` INT NOT NULL UNIQUE,
    `ConsecutiveLosses` INT NOT NULL DEFAULT 0,
    `DailyLoss` DECIMAL(18,8) NOT NULL DEFAULT 0,
    `DailyLossResetAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `TotalDrawdown` DECIMAL(18,8) NOT NULL DEFAULT 0,
    `LastOrderAt` DATETIME(6) NULL,
    `OrderCountThisCycle` INT NOT NULL DEFAULT 0,
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    CONSTRAINT `FK_BotRiskStates_TradingBots`
        FOREIGN KEY (`BotId`) REFERENCES `TradingBots`(`Id`) ON DELETE CASCADE,
    INDEX `IX_BotRiskStates_BotId` (`BotId`),
    INDEX `IX_BotRiskStates_UpdatedAt` (`UpdatedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 3. Create UserCapitalLimits table
CREATE TABLE IF NOT EXISTS `UserCapitalLimits` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `UserId` INT NOT NULL,
    `MaxTotalExposure` DECIMAL(18,8) NOT NULL DEFAULT 10000.00000000,
    `MaxCapitalPerBot` DECIMAL(18,8) NOT NULL DEFAULT 5000.00000000,
    `MaxBotsAllowed` INT NOT NULL DEFAULT 5,
    `MaxDailyLoss` DECIMAL(18,8) NOT NULL DEFAULT 500.00000000,
    `CreatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    CONSTRAINT `FK_UserCapitalLimits_AspNetUsers`
        FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers`(`Id`) ON DELETE CASCADE,
    UNIQUE INDEX `IX_UserCapitalLimits_UserId` (`UserId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 4. Add new columns to BotRiskConfigurations (if table exists)
-- Check if columns already exist before adding

SET @table_exists = (
    SELECT COUNT(*)
    FROM information_schema.tables
    WHERE table_schema = 'CryptoTradingDB'
    AND table_name = 'BotRiskConfigurations'
);

-- Add ConsecutiveLossLimit column
SET @column_exists = (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = 'CryptoTradingDB'
    AND table_name = 'BotRiskConfigurations'
    AND column_name = 'ConsecutiveLossLimit'
);

SET @sql = IF(@table_exists > 0 AND @column_exists = 0,
    'ALTER TABLE `BotRiskConfigurations` ADD COLUMN `ConsecutiveLossLimit` INT NOT NULL DEFAULT 5',
    'SELECT "ConsecutiveLossLimit column already exists or table does not exist" AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add DailyLossLimit column
SET @column_exists = (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = 'CryptoTradingDB'
    AND table_name = 'BotRiskConfigurations'
    AND column_name = 'DailyLossLimit'
);

SET @sql = IF(@table_exists > 0 AND @column_exists = 0,
    'ALTER TABLE `BotRiskConfigurations` ADD COLUMN `DailyLossLimit` DECIMAL(18,8) NOT NULL DEFAULT 1000.00000000',
    'SELECT "DailyLossLimit column already exists or table does not exist" AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add MaxDrawdownPercent column
SET @column_exists = (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = 'CryptoTradingDB'
    AND table_name = 'BotRiskConfigurations'
    AND column_name = 'MaxDrawdownPercent'
);

SET @sql = IF(@table_exists > 0 AND @column_exists = 0,
    'ALTER TABLE `BotRiskConfigurations` ADD COLUMN `MaxDrawdownPercent` DECIMAL(5,2) NOT NULL DEFAULT 20.00',
    'SELECT "MaxDrawdownPercent column already exists or table does not exist" AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add MinOrderCooldownSeconds column
SET @column_exists = (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = 'CryptoTradingDB'
    AND table_name = 'BotRiskConfigurations'
    AND column_name = 'MinOrderCooldownSeconds'
);

SET @sql = IF(@table_exists > 0 AND @column_exists = 0,
    'ALTER TABLE `BotRiskConfigurations` ADD COLUMN `MinOrderCooldownSeconds` INT NOT NULL DEFAULT 30',
    'SELECT "MinOrderCooldownSeconds column already exists or table does not exist" AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Add MaxOrdersPerCycle column
SET @column_exists = (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = 'CryptoTradingDB'
    AND table_name = 'BotRiskConfigurations'
    AND column_name = 'MaxOrdersPerCycle'
);

SET @sql = IF(@table_exists > 0 AND @column_exists = 0,
    'ALTER TABLE `BotRiskConfigurations` ADD COLUMN `MaxOrdersPerCycle` INT NOT NULL DEFAULT 5',
    'SELECT "MaxOrdersPerCycle column already exists or table does not exist" AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 5. Seed default capital limits for existing users
INSERT INTO `UserCapitalLimits` (`UserId`, `MaxTotalExposure`, `MaxCapitalPerBot`, `MaxBotsAllowed`, `MaxDailyLoss`, `CreatedAt`, `UpdatedAt`)
SELECT
    u.Id,
    10000.00000000,  -- Default $10k total exposure
    5000.00000000,   -- Default $5k per bot
    5,               -- Default 5 bots
    500.00000000,    -- Default $500 daily loss limit
    NOW(),
    NOW()
FROM `AspNetUsers` u
WHERE NOT EXISTS (
    SELECT 1 FROM `UserCapitalLimits` ucl WHERE ucl.UserId = u.Id
);

-- 6. Create risk state for existing active bots
INSERT INTO `BotRiskStates` (`BotId`, `ConsecutiveLosses`, `DailyLoss`, `DailyLossResetAt`, `TotalDrawdown`, `UpdatedAt`)
SELECT
    b.Id,
    0,
    0,
    NOW(),
    0,
    NOW()
FROM `TradingBots` b
WHERE NOT EXISTS (
    SELECT 1 FROM `BotRiskStates` brs WHERE brs.BotId = b.Id
);

-- 7. Verify tables were created
SELECT
    'KillSwitchEvents' AS TableName,
    COUNT(*) AS RecordCount
FROM `KillSwitchEvents`
UNION ALL
SELECT
    'BotRiskStates' AS TableName,
    COUNT(*) AS RecordCount
FROM `BotRiskStates`
UNION ALL
SELECT
    'UserCapitalLimits' AS TableName,
    COUNT(*) AS RecordCount
FROM `UserCapitalLimits`;

-- 8. Show updated BotRiskConfigurations schema (if exists)
DESCRIBE `BotRiskConfigurations`;

SELECT 'Migration completed successfully' AS Status;
