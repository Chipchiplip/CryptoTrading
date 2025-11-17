-- ============================================================
-- 007_AddRiskAndOpsTables.sql
-- Purpose: bootstrap the missing operational/risk tables so the
--          current EF models can work without altering legacy data
-- NOTE: run this once against the existing database before starting
--       the application. All statements are CREATE TABLE only.
-- ============================================================

-- 1) Bot risk configuration (per user/bot limits)
CREATE TABLE IF NOT EXISTS `BotRiskConfigurations` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NULL,
    `BotId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NULL,
    `MaxAllowedCapital` decimal(30,10) NOT NULL DEFAULT 0,
    `MaxSlippage` decimal(10,4) NOT NULL DEFAULT 0.0500,
    `MaxDailyLoss` decimal(10,4) NOT NULL DEFAULT 0.1000,
    `MaxConsecutiveLosses` int NOT NULL DEFAULT 5,
    `CooldownSeconds` int NOT NULL DEFAULT 300,
    `KillSwitchEnabled` tinyint(1) NOT NULL DEFAULT 1,
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    KEY `IX_BotRiskConfigurations_UserId_BotId` (`UserId`, `BotId`),
    CONSTRAINT `FK_BotRiskConfigurations_Users_UserId`
        FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_BotRiskConfigurations_TradingBots_BotId`
        FOREIGN KEY (`BotId`) REFERENCES `TradingBots` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 2) Bot risk states (runtime counters / guard rails)
CREATE TABLE IF NOT EXISTS `BotRiskStates` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `BotId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
    `ConsecutiveLosses` int NOT NULL DEFAULT 0,
    `DailyLoss` decimal(18,8) NOT NULL DEFAULT 0,
    `DailyLossResetAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `TotalDrawdown` decimal(18,8) NOT NULL DEFAULT 0,
    `LastOrderAt` datetime(6) DEFAULT NULL,
    `OrderCountThisCycle` int NOT NULL DEFAULT 0,
    `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    KEY `IX_BotRiskStates_BotId` (`BotId`),
    CONSTRAINT `FK_BotRiskStates_TradingBots_BotId`
        FOREIGN KEY (`BotId`) REFERENCES `TradingBots` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 3) Kill switch events history
CREATE TABLE IF NOT EXISTS `KillSwitchEvents` (
    `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
    `BotId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
    `TriggerReason` varchar(500) NOT NULL,
    `TriggerTime` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `TotalLoss` decimal(18,8) DEFAULT NULL,
    `ConsecutiveLosses` int DEFAULT NULL,
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    KEY `IX_KillSwitchEvents_BotId` (`BotId`),
    KEY `IX_KillSwitchEvents_TriggerTime` (`TriggerTime`),
    CONSTRAINT `FK_KillSwitchEvents_TradingBots_BotId`
        FOREIGN KEY (`BotId`) REFERENCES `TradingBots` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 4) User capital limits (per user guardrail)
CREATE TABLE IF NOT EXISTS `UserCapitalLimits` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `MaxTotalExposure` decimal(18,8) NOT NULL DEFAULT 10000,
    `MaxCapitalPerBot` decimal(18,8) NOT NULL DEFAULT 5000,
    `MaxBotsAllowed` int NOT NULL DEFAULT 5,
    `MaxDailyLoss` decimal(18,8) NOT NULL DEFAULT 500,
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_UserCapitalLimits_User` (`UserId`),
    CONSTRAINT `FK_UserCapitalLimits_Users_UserId`
        FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 5) Fee ledger
CREATE TABLE IF NOT EXISTS `FeeLedger` (
    `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `TradeId` bigint unsigned DEFAULT NULL,
    `FeeType` varchar(10) NOT NULL,
    `FeeAmount` decimal(30,10) NOT NULL,
    `FeeCurrency` varchar(10) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    KEY `IX_FeeLedger_UserId_CreatedAt` (`UserId`,`CreatedAt`),
    KEY `IX_FeeLedger_TradeId` (`TradeId`),
    CONSTRAINT `FK_FeeLedger_Users_UserId`
        FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_FeeLedger_Trades_TradeId`
        FOREIGN KEY (`TradeId`) REFERENCES `Trades` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 6) Trading configurations (key/value settings)
CREATE TABLE IF NOT EXISTS `TradingConfigurations` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ConfigKey` varchar(50) NOT NULL,
    `ConfigValue` varchar(255) NOT NULL,
    `Description` varchar(500) DEFAULT NULL,
    `Environment` varchar(20) NOT NULL DEFAULT 'Production',
    `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    `UpdatedBy` varchar(100) DEFAULT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_TradingConfigurations_KeyEnv` (`ConfigKey`,`Environment`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 7) Client order idempotency
CREATE TABLE IF NOT EXISTS `ClientOrderIdempotency` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ClientOrderId` varchar(100) NOT NULL,
    `UserId` int NOT NULL,
    `OrderId` bigint unsigned NOT NULL,
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_ClientOrderIdempotency_ClientOrderId` (`ClientOrderId`),
    KEY `IX_ClientOrderIdempotency_UserId_CreatedAt` (`UserId`,`CreatedAt`),
    CONSTRAINT `FK_ClientOrderIdempotency_Users_UserId`
        FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ClientOrderIdempotency_Orders_OrderId`
        FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 8) Audit events
CREATE TABLE IF NOT EXISTS `AuditEvents` (
    `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
    `EventType` varchar(50) NOT NULL,
    `UserId` int NOT NULL,
    `BotId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
    `CorrelationId` varchar(50) NOT NULL,
    `EntityType` varchar(50) NOT NULL,
    `EntityId` bigint unsigned DEFAULT NULL,
    `BeforeState` json DEFAULT NULL,
    `AfterState` json DEFAULT NULL,
    `Metadata` text,
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `IpAddress` varchar(100) DEFAULT NULL,
    `UserAgent` varchar(500) DEFAULT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_AuditEvents_UserId_CreatedAt` (`UserId`,`CreatedAt`),
    KEY `IX_AuditEvents_Entity` (`EntityType`,`EntityId`),
    CONSTRAINT `FK_AuditEvents_Users_UserId`
        FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_AuditEvents_TradingBots_BotId`
        FOREIGN KEY (`BotId`) REFERENCES `TradingBots` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 9) Reconciliation results
CREATE TABLE IF NOT EXISTS `ReconciliationResults` (
    `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
    `ReconciliationTime` datetime(6) NOT NULL,
    `EntityType` varchar(50) NOT NULL,
    `TotalChecked` int NOT NULL,
    `MismatchCount` int NOT NULL,
    `Mismatches` json DEFAULT NULL,
    `Status` varchar(20) NOT NULL,
    `ErrorMessage` text,
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    KEY `IX_ReconciliationResults_EntityType_Time` (`EntityType`,`ReconciliationTime`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

