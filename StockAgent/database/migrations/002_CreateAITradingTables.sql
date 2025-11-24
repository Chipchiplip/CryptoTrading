-- ============================================
-- NEW AUXILIARY TABLES FOR AI TRADING SYSTEM
-- These tables are ADD-ON only, do NOT modify legacy tables
-- ============================================

-- Table: ai_trading_recommendations
-- Stores live trading recommendations from AI engine
CREATE TABLE IF NOT EXISTS ai_trading_recommendations (
    Id BIGINT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    RecommendationId VARCHAR(100) NOT NULL UNIQUE,
    CreatedAtUtc DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    
    -- Input context
    TradingPlanId VARCHAR(100) NULL,
    MarketSnapshotJson LONGTEXT NULL,
    
    -- Recommendation output
    Decision ENUM('NO_TRADE', 'BUY', 'SELL') NOT NULL,
    Symbol VARCHAR(20) NOT NULL,
    AmountUsdt DECIMAL(28, 8) NOT NULL DEFAULT 0,
    Reason TEXT NULL,
    Confidence DECIMAL(5, 4) NOT NULL DEFAULT 0.0,
    TimeHorizon ENUM('scalping', 'intraday', 'swing') NULL,
    
    -- Metadata
    Status ENUM('pending', 'applied', 'rejected', 'expired') NOT NULL DEFAULT 'pending',
    AppliedAtUtc DATETIME(3) NULL,
    AppliedOrderId BIGINT NULL,
    
    INDEX IX_ai_rec_created (CreatedAtUtc DESC),
    INDEX IX_ai_rec_status (Status),
    INDEX IX_ai_rec_symbol (Symbol)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Table: ai_trading_signals
-- Stores AI-generated trading signals for analysis
CREATE TABLE IF NOT EXISTS ai_trading_signals (
    Id BIGINT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    SignalId VARCHAR(100) NOT NULL UNIQUE,
    CreatedAtUtc DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    
    Symbol VARCHAR(20) NOT NULL,
    SignalType ENUM('trend', 'momentum', 'volume', 'volatility', 'support_resistance') NOT NULL,
    SignalValue DECIMAL(28, 8) NOT NULL,
    SignalStrength ENUM('weak', 'moderate', 'strong') NOT NULL,
    
    -- Context
    PriceAtSignal DECIMAL(28, 8) NOT NULL,
    Timeframe VARCHAR(10) NOT NULL,
    SnapshotJson LONGTEXT NULL,
    
    INDEX IX_ai_sig_symbol_time (Symbol, CreatedAtUtc DESC),
    INDEX IX_ai_sig_type (SignalType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Table: ai_risk_audit_log
-- Audit log for risk checks and decisions
CREATE TABLE IF NOT EXISTS ai_risk_audit_log (
    Id BIGINT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    CreatedAtUtc DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    
    RecommendationId VARCHAR(100) NULL,
    AuditType ENUM('capital_check', 'exposure_check', 'volatility_check', 'symbol_check', 'time_check') NOT NULL,
    Passed BOOLEAN NOT NULL,
    Details TEXT NULL,
    
    INDEX IX_ai_audit_rec (RecommendationId),
    INDEX IX_ai_audit_created (CreatedAtUtc DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Table: ai_bot_profiles
-- Stores bot configurations that use AI recommendations
CREATE TABLE IF NOT EXISTS ai_bot_profiles (
    Id BIGINT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    ProfileId VARCHAR(100) NOT NULL UNIQUE,
    CreatedAtUtc DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    UpdatedAtUtc DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    
    ProfileName VARCHAR(200) NOT NULL,
    TradingPlanJson LONGTEXT NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    
    -- Auto-apply settings
    AutoApplyRecommendations BOOLEAN NOT NULL DEFAULT FALSE,
    MinConfidenceThreshold DECIMAL(5, 4) NOT NULL DEFAULT 0.7,
    
    INDEX IX_ai_bot_active (IsActive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

