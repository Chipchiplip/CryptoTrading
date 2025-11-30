-- MySQL dump 10.13  Distrib 8.0.44, for Win64 (x86_64)
--
-- Host: cryptotrading-01-phantrunghieu0000-ad84.g.aivencloud.com    Database: crypto_trading
-- ------------------------------------------------------
-- Server version	8.0.35

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
SET @MYSQLDUMP_TEMP_LOG_BIN = @@SESSION.SQL_LOG_BIN;
SET @@SESSION.SQL_LOG_BIN= 0;

--
-- GTID state at the beginning of the backup 
--

SET @@GLOBAL.GTID_PURGED=/*!80000 '+'*/ '9c8f5c2b-adbe-11f0-a2b5-862ccfb0716c:1-10430';

--
-- Table structure for table `AiGeneratedBotProfiles`
--

DROP TABLE IF EXISTS `AiGeneratedBotProfiles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AiGeneratedBotProfiles` (
  `Id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL,
  `UserId` int NOT NULL,
  `SessionId` char(36) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Name` varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL,
  `SymbolsJson` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `StrategyType` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `RiskMode` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL,
  `MaxCapitalPerTrade` decimal(28,8) NOT NULL,
  `MaxDailyExposure` decimal(28,8) NOT NULL,
  `TimeHorizon` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `ExpectedReturnPct` decimal(9,4) DEFAULT NULL,
  `RiskNote` text COLLATE utf8mb4_unicode_ci,
  `SourceRecommendationId` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CreatedAtUtc` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `IX_AiGeneratedBotProfiles_UserId` (`UserId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `AuditEvents`
--

DROP TABLE IF EXISTS `AuditEvents`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `AuditEvents` (
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
  KEY `FK_AuditEvents_TradingBots_BotId` (`BotId`),
  CONSTRAINT `FK_AuditEvents_TradingBots_BotId` FOREIGN KEY (`BotId`) REFERENCES `TradingBots` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_AuditEvents_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `BotRiskConfigurations`
--

DROP TABLE IF EXISTS `BotRiskConfigurations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `BotRiskConfigurations` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int DEFAULT NULL,
  `BotId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  `MaxAllowedCapital` decimal(30,10) NOT NULL DEFAULT '0.0000000000',
  `MaxSlippage` decimal(10,4) NOT NULL DEFAULT '0.0500',
  `MaxDailyLoss` decimal(10,4) NOT NULL DEFAULT '0.1000',
  `MaxConsecutiveLosses` int NOT NULL DEFAULT '5',
  `CooldownSeconds` int NOT NULL DEFAULT '300',
  `KillSwitchEnabled` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  KEY `IX_BotRiskConfigurations_UserId_BotId` (`UserId`,`BotId`),
  KEY `FK_BotRiskConfigurations_TradingBots_BotId` (`BotId`),
  CONSTRAINT `FK_BotRiskConfigurations_TradingBots_BotId` FOREIGN KEY (`BotId`) REFERENCES `TradingBots` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_BotRiskConfigurations_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `BotRiskStates`
--

DROP TABLE IF EXISTS `BotRiskStates`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `BotRiskStates` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `BotId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `ConsecutiveLosses` int NOT NULL DEFAULT '0',
  `DailyLoss` decimal(18,8) NOT NULL DEFAULT '0.00000000',
  `DailyLossResetAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `TotalDrawdown` decimal(18,8) NOT NULL DEFAULT '0.00000000',
  `LastOrderAt` datetime(6) DEFAULT NULL,
  `OrderCountThisCycle` int NOT NULL DEFAULT '0',
  `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  KEY `IX_BotRiskStates_BotId` (`BotId`),
  CONSTRAINT `FK_BotRiskStates_TradingBots_BotId` FOREIGN KEY (`BotId`) REFERENCES `TradingBots` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `BotStrategyDefinitions`
--

DROP TABLE IF EXISTS `BotStrategyDefinitions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `BotStrategyDefinitions` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `StrategyKey` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Version` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DisplayName` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ParametersSchema` json DEFAULT NULL,
  `AssemblyName` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `EntryType` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `MaxConcurrency` int NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_BotStrategyDefinitions_StrategyKey_Version` (`StrategyKey`,`Version`),
  KEY `IX_BotStrategyDefinitions_IsActive` (`IsActive`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ClientOrderIdempotency`
--

DROP TABLE IF EXISTS `ClientOrderIdempotency`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ClientOrderIdempotency` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ClientOrderId` varchar(100) NOT NULL,
  `UserId` int NOT NULL,
  `OrderId` bigint unsigned NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_ClientOrderIdempotency_ClientOrderId` (`ClientOrderId`),
  KEY `IX_ClientOrderIdempotency_UserId_CreatedAt` (`UserId`,`CreatedAt`),
  KEY `FK_ClientOrderIdempotency_Orders_OrderId` (`OrderId`),
  CONSTRAINT `FK_ClientOrderIdempotency_Orders_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ClientOrderIdempotency_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `CryptoPrices`
--

DROP TABLE IF EXISTS `CryptoPrices`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `CryptoPrices` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CryptocurrencyId` int NOT NULL,
  `PriceUsd` decimal(28,8) NOT NULL,
  `MarketCap` decimal(28,2) DEFAULT NULL,
  `Volume24h` decimal(28,2) DEFAULT NULL,
  `PercentChange1h` decimal(10,4) DEFAULT NULL,
  `PercentChange24h` decimal(10,4) DEFAULT NULL,
  `PercentChange7d` decimal(10,4) DEFAULT NULL,
  `CirculatingSupply` decimal(28,2) DEFAULT NULL,
  `TotalSupply` decimal(28,2) DEFAULT NULL,
  `CollectedAtUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_CryptoPrices_CryptocurrencyId_CollectedAtUtc` (`CryptocurrencyId`,`CollectedAtUtc`),
  CONSTRAINT `FK_CryptoPrices_Cryptocurrencies_CryptocurrencyId` FOREIGN KEY (`CryptocurrencyId`) REFERENCES `Cryptocurrencies` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=18576 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Cryptocurrencies`
--

DROP TABLE IF EXISTS `Cryptocurrencies`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Cryptocurrencies` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CoinGeckoId` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Symbol` varchar(24) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `IconUrl` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `MarketCapRank` int DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `UpdatedAtUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Cryptocurrencies_CoinGeckoId` (`CoinGeckoId`),
  UNIQUE KEY `IX_Cryptocurrencies_Symbol` (`Symbol`)
) ENGINE=InnoDB AUTO_INCREMENT=56 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `DepositTransactions`
--

DROP TABLE IF EXISTS `DepositTransactions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `DepositTransactions` (
  `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `OrderId` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Amount` decimal(18,2) NOT NULL,
  `Currency` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Status` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `VnpayTransactionId` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `VnpayResponseCode` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `VnpayMessage` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `CompletedAt` datetime(6) DEFAULT NULL,
  `Provider` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT 'VNPAY',
  `StripeSessionId` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `StripePaymentIntentId` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `PaymentMethod` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_DepositTransactions_OrderId` (`OrderId`),
  KEY `IX_DepositTransactions_UserId_CreatedAt` (`UserId`,`CreatedAt`),
  CONSTRAINT `FK_DepositTransactions_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=18 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `FeeLedger`
--

DROP TABLE IF EXISTS `FeeLedger`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `FeeLedger` (
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
  CONSTRAINT `FK_FeeLedger_Trades_TradeId` FOREIGN KEY (`TradeId`) REFERENCES `Trades` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_FeeLedger_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `KillSwitchEvents`
--

DROP TABLE IF EXISTS `KillSwitchEvents`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `KillSwitchEvents` (
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
  CONSTRAINT `FK_KillSwitchEvents_TradingBots_BotId` FOREIGN KEY (`BotId`) REFERENCES `TradingBots` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Levels`
--

DROP TABLE IF EXISTS `Levels`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Levels` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Number` int NOT NULL,
  `MinBalance` decimal(65,30) DEFAULT NULL,
  `MaxBalance` decimal(65,30) DEFAULT NULL,
  `Description` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsDeleted` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `LoginActivity`
--

DROP TABLE IF EXISTS `LoginActivity`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `LoginActivity` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `Ip` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `UserAgent` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Success` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_LoginActivity_UserId_CreatedAt` (`UserId`,`CreatedAt`),
  CONSTRAINT `FK_LoginActivity_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=66 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `MarketStats`
--

DROP TABLE IF EXISTS `MarketStats`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `MarketStats` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `TotalMarketCap` decimal(28,2) NOT NULL,
  `TotalVolume` decimal(28,2) NOT NULL,
  `ActiveCryptocurrencies` int NOT NULL,
  `MarketCapChangePercentage24h` decimal(10,4) NOT NULL,
  `BtcDominance` decimal(10,4) DEFAULT NULL,
  `EthDominance` decimal(10,4) DEFAULT NULL,
  `CollectedAtUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_MarketStats_CollectedAtUtc` (`CollectedAtUtc`)
) ENGINE=InnoDB AUTO_INCREMENT=393 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Notifications`
--

DROP TABLE IF EXISTS `Notifications`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Notifications` (
  `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `Type` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Title` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Message` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Category` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `IsRead` tinyint(1) NOT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `ReadAtUtc` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Notifications_UserId_CreatedAtUtc` (`UserId`,`CreatedAtUtc`),
  KEY `IX_Notifications_UserId_IsRead` (`UserId`,`IsRead`),
  CONSTRAINT `FK_Notifications_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `OrderHolds`
--

DROP TABLE IF EXISTS `OrderHolds`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `OrderHolds` (
  `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `OrderId` bigint unsigned NOT NULL,
  `WalletId` bigint unsigned NOT NULL,
  `Amount` decimal(38,18) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `ReleasedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_OrderHolds_OrderId` (`OrderId`),
  KEY `IX_OrderHolds_WalletId` (`WalletId`),
  CONSTRAINT `FK_OrderHolds_Orders_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_OrderHolds_Wallets_WalletId` FOREIGN KEY (`WalletId`) REFERENCES `Wallets` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=57 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Orders`
--

DROP TABLE IF EXISTS `Orders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Orders` (
  `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `CryptocurrencyId` int NOT NULL,
  `Side` varchar(4) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Type` varchar(12) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Status` varchar(12) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `PriceUsd` decimal(30,10) DEFAULT NULL,
  `QuantityCoin` decimal(38,18) NOT NULL,
  `FilledQty` decimal(38,18) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Orders_CryptocurrencyId_Status` (`CryptocurrencyId`,`Status`),
  KEY `IX_Orders_UserId_CreatedAt` (`UserId`,`CreatedAt`),
  CONSTRAINT `FK_Orders_Cryptocurrencies_CryptocurrencyId` FOREIGN KEY (`CryptocurrencyId`) REFERENCES `Cryptocurrencies` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_Orders_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=35 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PaymentHistories`
--

DROP TABLE IF EXISTS `PaymentHistories`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `PaymentHistories` (
  `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `SubscriptionId` bigint unsigned DEFAULT NULL,
  `Amount` decimal(18,2) NOT NULL,
  `Currency` varchar(3) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT 'VND',
  `Status` varchar(16) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `VnpayTransactionId` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `VnpayOrderId` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `PaymentMethod` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `PlanType` int DEFAULT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `UpdatedAtUtc` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_PaymentHistories_SubscriptionId` (`SubscriptionId`),
  KEY `IX_PaymentHistories_UserId_CreatedAtUtc` (`UserId`,`CreatedAtUtc`),
  KEY `IX_PaymentHistories_VnpayOrderId` (`VnpayOrderId`),
  CONSTRAINT `FK_PaymentHistories_Subscriptions_SubscriptionId` FOREIGN KEY (`SubscriptionId`) REFERENCES `Subscriptions` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_PaymentHistories_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=26 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `PriceTriggers`
--

DROP TABLE IF EXISTS `PriceTriggers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `PriceTriggers` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `Symbol` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Side` varchar(10) COLLATE utf8mb4_unicode_ci NOT NULL,
  `TriggerPrice` decimal(28,8) NOT NULL,
  `AmountUsdt` decimal(28,8) NOT NULL,
  `Status` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'pending',
  `CreatedAtUtc` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `FiredAtUtc` datetime(6) DEFAULT NULL,
  `OrderId` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ErrorMessage` varchar(1000) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_PriceTriggers_UserId_Status` (`UserId`,`Status`),
  KEY `IX_PriceTriggers_Symbol_Status` (`Symbol`,`Status`),
  KEY `IX_PriceTriggers_Status` (`Status`),
  CONSTRAINT `FK_PriceTriggers_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ReconciliationResults`
--

DROP TABLE IF EXISTS `ReconciliationResults`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ReconciliationResults` (
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
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Roles`
--

DROP TABLE IF EXISTS `Roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Roles` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Subscriptions`
--

DROP TABLE IF EXISTS `Subscriptions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Subscriptions` (
  `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `PlanType` int NOT NULL,
  `Status` varchar(16) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `VnpayTransactionId` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CurrentPeriodStartUtc` datetime(6) NOT NULL,
  `CurrentPeriodEndUtc` datetime(6) NOT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `CanceledAtUtc` datetime(6) DEFAULT NULL,
  `UpdatedAtUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Subscriptions_UserId` (`UserId`),
  KEY `IX_Subscriptions_UserId_Status` (`UserId`,`Status`),
  CONSTRAINT `FK_Subscriptions_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Trades`
--

DROP TABLE IF EXISTS `Trades`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Trades` (
  `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `OrderId` bigint unsigned NOT NULL,
  `CryptocurrencyId` int NOT NULL,
  `PriceUsd` decimal(30,10) NOT NULL,
  `QuantityCoin` decimal(38,18) NOT NULL,
  `FeeUsd` decimal(30,10) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Trades_CryptocurrencyId` (`CryptocurrencyId`),
  KEY `IX_Trades_OrderId_CreatedAt` (`OrderId`,`CreatedAt`),
  CONSTRAINT `FK_Trades_Cryptocurrencies_CryptocurrencyId` FOREIGN KEY (`CryptocurrencyId`) REFERENCES `Cryptocurrencies` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_Trades_Orders_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=30 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `TradingBotLogs`
--

DROP TABLE IF EXISTS `TradingBotLogs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `TradingBotLogs` (
  `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `TradingBotId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Level` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Category` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Message` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Payload` json DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  KEY `IX_TradingBotLogs_Level` (`Level`),
  KEY `IX_TradingBotLogs_TradingBotId_CreatedAt` (`TradingBotId`,`CreatedAt`),
  CONSTRAINT `FK_TradingBotLogs_TradingBots_TradingBotId` FOREIGN KEY (`TradingBotId`) REFERENCES `TradingBots` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=227 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `TradingBotOrders`
--

DROP TABLE IF EXISTS `TradingBotOrders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `TradingBotOrders` (
  `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `TradingBotId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `OrderId` bigint unsigned NOT NULL,
  `Intent` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `SignalId` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_TradingBotOrders_OrderId` (`OrderId`),
  KEY `IX_TradingBotOrders_TradingBotId_CreatedAt` (`TradingBotId`,`CreatedAt`),
  CONSTRAINT `FK_TradingBotOrders_Orders_OrderId` FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_TradingBotOrders_TradingBots_TradingBotId` FOREIGN KEY (`TradingBotId`) REFERENCES `TradingBots` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `TradingBotParameters`
--

DROP TABLE IF EXISTS `TradingBotParameters`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `TradingBotParameters` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `TradingBotId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `ParameterKey` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ParameterValue` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ValueType` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  KEY `IX_TradingBotParameters_TradingBotId_ParameterKey` (`TradingBotId`,`ParameterKey`),
  CONSTRAINT `FK_TradingBotParameters_TradingBots_TradingBotId` FOREIGN KEY (`TradingBotId`) REFERENCES `TradingBots` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `TradingBotRuntimeSnapshots`
--

DROP TABLE IF EXISTS `TradingBotRuntimeSnapshots`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `TradingBotRuntimeSnapshots` (
  `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `TradingBotId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `CapturedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `RuntimeState` json NOT NULL,
  `LastSignal` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `OpenPositionSummary` json DEFAULT NULL,
  `NextTickAt` datetime(6) DEFAULT NULL,
  `Version` datetime(6) DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  KEY `IX_TradingBotRuntimeSnapshots_TradingBotId_CapturedAt` (`TradingBotId`,`CapturedAt`),
  CONSTRAINT `FK_TradingBotRuntimeSnapshots_TradingBots_TradingBotId` FOREIGN KEY (`TradingBotId`) REFERENCES `TradingBots` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=227 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `TradingBots`
--

DROP TABLE IF EXISTS `TradingBots`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `TradingBots` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `UserId` int NOT NULL,
  `StrategyDefinitionId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Name` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Status` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `RiskProfile` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `BaseAsset` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `QuoteAsset` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `PositionSizing` json DEFAULT NULL,
  `Parameters` json DEFAULT NULL,
  `ExecutionIntervalSeconds` int NOT NULL,
  `NextRunAt` datetime(6) DEFAULT NULL,
  `LastStatusReason` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_TradingBots_NextRunAt` (`NextRunAt`),
  KEY `IX_TradingBots_StrategyDefinitionId` (`StrategyDefinitionId`),
  KEY `IX_TradingBots_UserId_Status` (`UserId`,`Status`),
  CONSTRAINT `FK_TradingBots_BotStrategyDefinitions_StrategyDefinitionId` FOREIGN KEY (`StrategyDefinitionId`) REFERENCES `BotStrategyDefinitions` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_TradingBots_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `TradingConfigurations`
--

DROP TABLE IF EXISTS `TradingConfigurations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `TradingConfigurations` (
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
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `UserCapitalLimits`
--

DROP TABLE IF EXISTS `UserCapitalLimits`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `UserCapitalLimits` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `MaxTotalExposure` decimal(18,8) NOT NULL DEFAULT '10000.00000000',
  `MaxCapitalPerBot` decimal(18,8) NOT NULL DEFAULT '5000.00000000',
  `MaxBotsAllowed` int NOT NULL DEFAULT '5',
  `MaxDailyLoss` decimal(18,8) NOT NULL DEFAULT '500.00000000',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_UserCapitalLimits_User` (`UserId`),
  CONSTRAINT `FK_UserCapitalLimits_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `UserWatchlist`
--

DROP TABLE IF EXISTS `UserWatchlist`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `UserWatchlist` (
  `UserId` int NOT NULL,
  `CryptocurrencyId` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`UserId`,`CryptocurrencyId`),
  KEY `IX_UserWatchlist_CryptocurrencyId` (`CryptocurrencyId`),
  CONSTRAINT `FK_UserWatchlist_Cryptocurrencies_CryptocurrencyId` FOREIGN KEY (`CryptocurrencyId`) REFERENCES `Cryptocurrencies` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_UserWatchlist_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Users`
--

DROP TABLE IF EXISTS `Users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Users` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Email` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `PasswordHash` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `FullName` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `EmailConfirmed` tinyint(1) NOT NULL,
  `EmailConfirmationToken` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `EmailConfirmationTokenExpiry` datetime(6) DEFAULT NULL,
  `TwoFactorEnabled` tinyint(1) NOT NULL,
  `TwoFactorSecret` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `LastLoginAt` datetime(6) DEFAULT NULL,
  `RefreshToken` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `RefreshTokenExpiryTime` datetime(6) DEFAULT NULL,
  `PasswordResetToken` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `PasswordResetTokenExpiry` datetime(6) DEFAULT NULL,
  `Role` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT 'User',
  `Level` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT 'Free',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `AvatarUrl` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Bio` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `PhoneNumber` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Timezone` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Users_Email` (`Email`),
  KEY `IX_Users_IsActive` (`IsActive`),
  KEY `IX_Users_Level` (`Level`),
  KEY `IX_Users_Role` (`Role`)
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `WalletMovements`
--

DROP TABLE IF EXISTS `WalletMovements`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `WalletMovements` (
  `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `WalletId` bigint unsigned NOT NULL,
  `RefType` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `RefId` bigint unsigned DEFAULT NULL,
  `Amount` decimal(38,18) NOT NULL,
  `Note` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_WalletMovements_WalletId_CreatedAt` (`WalletId`,`CreatedAt`),
  CONSTRAINT `FK_WalletMovements_Wallets_WalletId` FOREIGN KEY (`WalletId`) REFERENCES `Wallets` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=83 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `Wallets`
--

DROP TABLE IF EXISTS `Wallets`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `Wallets` (
  `Id` bigint unsigned NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `AssetType` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CurrencyCode` varchar(3) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CryptocurrencyId` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Wallets_UserId_AssetType_CurrencyCode_CryptocurrencyId` (`UserId`,`AssetType`,`CurrencyCode`,`CryptocurrencyId`),
  KEY `IX_Wallets_CryptocurrencyId` (`CryptocurrencyId`),
  CONSTRAINT `FK_Wallets_Cryptocurrencies_CryptocurrencyId` FOREIGN KEY (`CryptocurrencyId`) REFERENCES `Cryptocurrencies` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_Wallets_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `__EFMigrationsHistory`
--

DROP TABLE IF EXISTS `__EFMigrationsHistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__EFMigrationsHistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ai_bot_profiles`
--

DROP TABLE IF EXISTS `ai_bot_profiles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ai_bot_profiles` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `ProfileId` varchar(100) NOT NULL,
  `CreatedAtUtc` datetime(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  `UpdatedAtUtc` datetime(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  `ProfileName` varchar(200) NOT NULL,
  `TradingPlanJson` longtext NOT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `AutoApplyRecommendations` tinyint(1) NOT NULL DEFAULT '0',
  `MinConfidenceThreshold` decimal(5,4) NOT NULL DEFAULT '0.7000',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `ProfileId` (`ProfileId`),
  KEY `IX_ai_bot_active` (`IsActive`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ai_risk_audit_log`
--

DROP TABLE IF EXISTS `ai_risk_audit_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ai_risk_audit_log` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `CreatedAtUtc` datetime(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  `RecommendationId` varchar(100) DEFAULT NULL,
  `AuditType` enum('capital_check','exposure_check','volatility_check','symbol_check','time_check') NOT NULL,
  `Passed` tinyint(1) NOT NULL,
  `Details` text,
  PRIMARY KEY (`Id`),
  KEY `IX_ai_audit_rec` (`RecommendationId`),
  KEY `IX_ai_audit_created` (`CreatedAtUtc` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ai_trading_recommendations`
--

DROP TABLE IF EXISTS `ai_trading_recommendations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ai_trading_recommendations` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `RecommendationId` varchar(100) NOT NULL,
  `CreatedAtUtc` datetime(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  `TradingPlanId` varchar(100) DEFAULT NULL,
  `MarketSnapshotJson` longtext,
  `Decision` enum('NO_TRADE','BUY','SELL') NOT NULL,
  `Symbol` varchar(20) NOT NULL,
  `AmountUsdt` decimal(28,8) NOT NULL DEFAULT '0.00000000',
  `Reason` text,
  `Confidence` decimal(5,4) NOT NULL DEFAULT '0.0000',
  `TimeHorizon` enum('scalping','intraday','swing') DEFAULT NULL,
  `Status` enum('pending','applied','rejected','expired') NOT NULL DEFAULT 'pending',
  `AppliedAtUtc` datetime(3) DEFAULT NULL,
  `AppliedOrderId` bigint DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `RecommendationId` (`RecommendationId`),
  KEY `IX_ai_rec_created` (`CreatedAtUtc` DESC),
  KEY `IX_ai_rec_status` (`Status`),
  KEY `IX_ai_rec_symbol` (`Symbol`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `ai_trading_signals`
--

DROP TABLE IF EXISTS `ai_trading_signals`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ai_trading_signals` (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `SignalId` varchar(100) NOT NULL,
  `CreatedAtUtc` datetime(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  `Symbol` varchar(20) NOT NULL,
  `SignalType` enum('trend','momentum','volume','volatility','support_resistance') NOT NULL,
  `SignalValue` decimal(28,8) NOT NULL,
  `SignalStrength` enum('weak','moderate','strong') NOT NULL,
  `PriceAtSignal` decimal(28,8) NOT NULL,
  `Timeframe` varchar(10) NOT NULL,
  `SnapshotJson` longtext,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `SignalId` (`SignalId`),
  KEY `IX_ai_sig_symbol_time` (`Symbol`,`CreatedAtUtc` DESC),
  KEY `IX_ai_sig_type` (`SignalType`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `sql_migrations_history`
--

DROP TABLE IF EXISTS `sql_migrations_history`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `sql_migrations_history` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ScriptName` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `AppliedAtUtc` datetime(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  `Checksum` varchar(64) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_sql_migrations_script` (`ScriptName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
SET @@SESSION.SQL_LOG_BIN = @MYSQLDUMP_TEMP_LOG_BIN;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-11-30 14:57:53
