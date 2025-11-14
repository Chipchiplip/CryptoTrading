-- =====================================================
-- CRYPTO TRADING PLATFORM - SEED DEFAULT CONFIGURATIONS
-- Version: 1.0.0
-- Date: November 2025
-- Description: Seeds default trading configurations for all environments
-- =====================================================

-- =====================================================
-- DEFAULT TRADING CONFIGURATIONS
-- =====================================================

-- Development Environment
INSERT INTO `TradingConfigurations` (`ConfigKey`, `ConfigValue`, `Description`, `Environment`, `UpdatedAt`, `UpdatedBy`)
VALUES 
    ('FeeRate', '0.001', 'Trading fee rate (0.1% = 0.001)', 'Development', NOW(), 'System'),
    ('MarketPriceBuffer', '0.05', 'Market order price buffer (5% = 0.05)', 'Development', NOW(), 'System')
ON DUPLICATE KEY UPDATE 
    `ConfigValue` = VALUES(`ConfigValue`),
    `UpdatedAt` = NOW();

-- Staging Environment
INSERT INTO `TradingConfigurations` (`ConfigKey`, `ConfigValue`, `Description`, `Environment`, `UpdatedAt`, `UpdatedBy`)
VALUES 
    ('FeeRate', '0.001', 'Trading fee rate (0.1% = 0.001)', 'Staging', NOW(), 'System'),
    ('MarketPriceBuffer', '0.05', 'Market order price buffer (5% = 0.05)', 'Staging', NOW(), 'System')
ON DUPLICATE KEY UPDATE 
    `ConfigValue` = VALUES(`ConfigValue`),
    `UpdatedAt` = NOW();

-- Production Environment
INSERT INTO `TradingConfigurations` (`ConfigKey`, `ConfigValue`, `Description`, `Environment`, `UpdatedAt`, `UpdatedBy`)
VALUES 
    ('FeeRate', '0.001', 'Trading fee rate (0.1% = 0.001)', 'Production', NOW(), 'System'),
    ('MarketPriceBuffer', '0.05', 'Market order price buffer (5% = 0.05)', 'Production', NOW(), 'System')
ON DUPLICATE KEY UPDATE 
    `ConfigValue` = VALUES(`ConfigValue`),
    `UpdatedAt` = NOW();

-- =====================================================
-- VERIFICATION QUERY
-- =====================================================
-- Run this to verify configurations were seeded:
-- SELECT * FROM TradingConfigurations ORDER BY Environment, ConfigKey;



