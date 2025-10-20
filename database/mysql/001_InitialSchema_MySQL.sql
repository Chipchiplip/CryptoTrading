-- =============================================
-- Crypto Trading Web - Initial Database Schema
-- MySQL 8.0+
-- Version: 1.0.0
-- =============================================

-- 0) Create Database & Configuration
-- =============================================
CREATE DATABASE IF NOT EXISTS crypto_trading;
USE crypto_trading;

-- =============================================
-- 2) AUTH TABLES
-- =============================================

-- auth.Users
CREATE TABLE IF NOT EXISTS auth_users (
    Id               CHAR(36) NOT NULL PRIMARY KEY,
    Email            VARCHAR(255) NOT NULL,
    PasswordHash     LONGBLOB NOT NULL,
    PasswordSalt     LONGBLOB NOT NULL,
    FirstName        VARCHAR(100) NULL,
    LastName         VARCHAR(100) NULL,
    PhoneNumber      VARCHAR(20)  NULL,
    IsEmailVerified  BOOLEAN NOT NULL DEFAULT FALSE,
    TwoFactorEnabled BOOLEAN NOT NULL DEFAULT FALSE,
    TwoFactorSecret  VARCHAR(255) NULL,
    SubscriptionTier INT NOT NULL DEFAULT 0,
    IsActive         BOOLEAN NOT NULL DEFAULT TRUE,
    ProfileImageUrl  VARCHAR(500)  NULL,
    CreatedAtUtc     DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    UpdatedAtUtc     DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    LastLoginAtUtc   DATETIME(3) NULL
);

CREATE UNIQUE INDEX UX_auth_users_email ON auth_users(Email);

-- auth.RefreshTokens
CREATE TABLE IF NOT EXISTS auth_refresh_tokens (
    Id           CHAR(36) NOT NULL PRIMARY KEY,
    UserId       CHAR(36) NOT NULL,
    Token        VARCHAR(500) NOT NULL,
    ExpiresAtUtc DATETIME(3) NOT NULL,
    CreatedAtUtc DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    RevokedAtUtc DATETIME(3) NULL,
    ReplacedBy   VARCHAR(500) NULL,
    IpAddress    VARCHAR(45) NULL,
    UserAgent    VARCHAR(500) NULL,
    FOREIGN KEY (UserId) REFERENCES auth_users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_auth_refresh_tokens_user ON auth_refresh_tokens(UserId);
CREATE UNIQUE INDEX UX_auth_refresh_tokens_token ON auth_refresh_tokens(Token);

-- auth.UserActivity
CREATE TABLE IF NOT EXISTS auth_user_activity (
    Id          CHAR(36) NOT NULL PRIMARY KEY,
    UserId      CHAR(36) NOT NULL,
    Action      VARCHAR(100) NOT NULL,
    Details     JSON NULL,
    IpAddress   VARCHAR(45) NULL,
    UserAgent   VARCHAR(500) NULL,
    CreatedAtUtc DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    FOREIGN KEY (UserId) REFERENCES auth_users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_auth_user_activity_user_time 
ON auth_user_activity(UserId, CreatedAtUtc DESC);

-- auth.AuditLogs
CREATE TABLE IF NOT EXISTS auth_audit_logs (
    Id            BIGINT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    UserId        CHAR(36) NULL,
    Action        VARCHAR(100) NOT NULL,
    EntityType    VARCHAR(50) NOT NULL,
    EntityId      VARCHAR(100) NULL,
    OldValues     JSON NULL,
    NewValues     JSON NULL,
    IpAddress     VARCHAR(45) NULL,
    UserAgent     VARCHAR(500) NULL,
    Severity      VARCHAR(20) NOT NULL,
    CreatedAtUtc  DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    FOREIGN KEY (UserId) REFERENCES auth_users(Id) ON DELETE SET NULL
);

CREATE INDEX IX_auth_audit_logs_user_time ON auth_audit_logs(UserId, CreatedAtUtc DESC);
CREATE INDEX IX_auth_audit_logs_entity ON auth_audit_logs(EntityType, EntityId);

-- =============================================
-- 3) MARKET TABLES
-- =============================================

-- market.Cryptocurrencies
CREATE TABLE IF NOT EXISTS market_cryptocurrencies (
    Id             INT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    CoinGeckoId    VARCHAR(100) NOT NULL,
    Symbol         VARCHAR(24) NOT NULL,
    Name           VARCHAR(100) NOT NULL,
    IconUrl        VARCHAR(500) NULL,
    MarketCapRank  INT NULL,
    IsActive       BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAtUtc   DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    UpdatedAtUtc   DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3)
);

CREATE UNIQUE INDEX UX_market_crypto_symbol ON market_cryptocurrencies(Symbol);
CREATE UNIQUE INDEX UX_market_crypto_coingecko_id ON market_cryptocurrencies(CoinGeckoId);

-- market.CryptoPrices
CREATE TABLE IF NOT EXISTS market_crypto_prices (
    Id               BIGINT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    CryptocurrencyId INT NOT NULL,
    PriceUsd         DECIMAL(28,8) NOT NULL,
    MarketCap        DECIMAL(28,2) NULL,
    Volume24h        DECIMAL(28,2) NULL,
    PercentChange1h  DECIMAL(10,4) NULL,
    PercentChange24h DECIMAL(10,4) NULL,
    PercentChange7d  DECIMAL(10,4) NULL,
    CirculatingSupply DECIMAL(28,2) NULL,
    TotalSupply      DECIMAL(28,2) NULL,
    CollectedAtUtc   DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    FOREIGN KEY (CryptocurrencyId) REFERENCES market_cryptocurrencies(Id) ON DELETE CASCADE
);

CREATE INDEX IX_market_prices_crypto_time
ON market_crypto_prices(CryptocurrencyId, CollectedAtUtc DESC);

-- market.MarketStats
CREATE TABLE IF NOT EXISTS market_market_stats (
    Id                           CHAR(36) NOT NULL PRIMARY KEY,
    TotalMarketCap               DECIMAL(28,2) NOT NULL,
    TotalVolume24h               DECIMAL(28,2) NOT NULL,
    MarketCapChangePercentage24h DECIMAL(10,4) NULL,
    ActiveCryptocurrencies       INT NULL,
    FearGreedIndex               INT NULL,
    FearGreedClassification      VARCHAR(50) NULL,
    TimestampUtc                 DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3)
);

CREATE INDEX IX_market_stats_ts ON market_market_stats(TimestampUtc DESC);

-- =============================================
-- 4) PORTFOLIO TABLES
-- =============================================

-- portfolio.UserWatchlists
CREATE TABLE IF NOT EXISTS portfolio_user_watchlists (
    Id           CHAR(36) NOT NULL PRIMARY KEY,
    UserId       CHAR(36) NOT NULL,
    Name         VARCHAR(100) NOT NULL,
    IsDefault    BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedAtUtc DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    UpdatedAtUtc DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    FOREIGN KEY (UserId) REFERENCES auth_users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_portfolio_watchlists_user ON portfolio_user_watchlists(UserId);

-- portfolio.WatchlistItems
CREATE TABLE IF NOT EXISTS portfolio_watchlist_items (
    Id               CHAR(36) NOT NULL PRIMARY KEY,
    WatchlistId      CHAR(36) NOT NULL,
    CryptocurrencyId INT NOT NULL,
    AddedAtUtc       DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    FOREIGN KEY (WatchlistId) REFERENCES portfolio_user_watchlists(Id) ON DELETE CASCADE,
    FOREIGN KEY (CryptocurrencyId) REFERENCES market_cryptocurrencies(Id)
);

CREATE UNIQUE INDEX UX_portfolio_watch_items_uq 
ON portfolio_watchlist_items(WatchlistId, CryptocurrencyId);

-- portfolio.Positions
CREATE TABLE IF NOT EXISTS portfolio_positions (
    Id            CHAR(36) NOT NULL PRIMARY KEY,
    UserId        CHAR(36) NOT NULL,
    CryptocurrencyId INT NOT NULL,
    Quantity      DECIMAL(28,8) NOT NULL,
    AvgPriceUsd   DECIMAL(28,8) NOT NULL,
    UpdatedAtUtc  DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    FOREIGN KEY (UserId) REFERENCES auth_users(Id) ON DELETE CASCADE,
    FOREIGN KEY (CryptocurrencyId) REFERENCES market_cryptocurrencies(Id),
    UNIQUE KEY UX_portfolio_positions_user_coin (UserId, CryptocurrencyId)
);

-- =============================================
-- 5) TRADING TABLES
-- =============================================

-- trading.Balances
CREATE TABLE IF NOT EXISTS trading_balances (
    Id            CHAR(36) NOT NULL PRIMARY KEY,
    UserId        CHAR(36) NOT NULL,
    Asset         VARCHAR(16) NOT NULL,
    Amount        DECIMAL(28,8) NOT NULL CHECK (Amount >= 0),
    UpdatedAtUtc  DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    FOREIGN KEY (UserId) REFERENCES auth_users(Id) ON DELETE CASCADE,
    UNIQUE KEY UX_trading_balances_user_asset (UserId, Asset)
);

-- trading.Bots
CREATE TABLE IF NOT EXISTS trading_bots (
    Id           CHAR(36) NOT NULL PRIMARY KEY,
    UserId       CHAR(36) NOT NULL,
    Name         VARCHAR(100) NOT NULL,
    Strategy     VARCHAR(32) NOT NULL,
    Symbol       VARCHAR(24) NOT NULL,
    IsActive     BOOLEAN NOT NULL DEFAULT FALSE,
    Config       JSON NOT NULL,
    CreatedAtUtc DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    UpdatedAtUtc DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    LastRunAtUtc DATETIME(3) NULL,
    FOREIGN KEY (UserId) REFERENCES auth_users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_trading_bots_user ON trading_bots(UserId);
CREATE INDEX IX_trading_bots_active ON trading_bots(IsActive);

-- trading.Orders
CREATE TABLE IF NOT EXISTS trading_orders (
    Id             CHAR(36) NOT NULL PRIMARY KEY,
    UserId         CHAR(36) NOT NULL,
    BotId          CHAR(36) NULL,
    BinanceOrderId VARCHAR(128) NULL,
    Symbol         VARCHAR(24) NOT NULL,
    Side           VARCHAR(4) NOT NULL CHECK (Side IN ('BUY','SELL')),
    Type           VARCHAR(12) NOT NULL CHECK (Type IN ('MARKET','LIMIT','STOP','STOP_LIMIT')),
    Quantity       DECIMAL(28,8) NOT NULL CHECK (Quantity > 0),
    PriceUsd       DECIMAL(28,8) NULL CHECK (PriceUsd IS NULL OR PriceUsd > 0),
    Status         VARCHAR(12) NOT NULL,
    ExecutedQty    DECIMAL(28,8) NOT NULL DEFAULT 0,
    ExecutedPrice  DECIMAL(28,8) NULL,
    Commission     DECIMAL(28,8) NULL,
    CommissionAsset VARCHAR(16) NULL,
    CreatedAtUtc   DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    UpdatedAtUtc   DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    FOREIGN KEY (UserId) REFERENCES auth_users(Id) ON DELETE CASCADE,
    FOREIGN KEY (BotId) REFERENCES trading_bots(Id) ON DELETE SET NULL
);

CREATE INDEX IX_trading_orders_user_time 
ON trading_orders(UserId, CreatedAtUtc DESC);
CREATE INDEX IX_trading_orders_external_id ON trading_orders(BinanceOrderId);

-- trading.Trades
CREATE TABLE IF NOT EXISTS trading_trades (
    Id            BIGINT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    OrderId       CHAR(36) NOT NULL,
    FillPriceUsd  DECIMAL(28,8) NOT NULL,
    FillQty       DECIMAL(28,8) NOT NULL,
    FeeAsset      VARCHAR(16) NULL,
    FeeAmount     DECIMAL(28,8) NULL,
    FilledAtUtc   DATETIME(3) NOT NULL,
    FOREIGN KEY (OrderId) REFERENCES trading_orders(Id) ON DELETE CASCADE
);

CREATE INDEX IX_trading_trades_order ON trading_trades(OrderId);

-- trading.BinanceApiKeys
CREATE TABLE IF NOT EXISTS trading_binance_api_keys (
    Id           CHAR(36) NOT NULL PRIMARY KEY,
    UserId       CHAR(36) NOT NULL,
    ApiKeyEnc    LONGBLOB NOT NULL,
    SecretKeyEnc LONGBLOB NOT NULL,
    Last4        VARCHAR(8) NULL,
    IsTestnet    BOOLEAN NOT NULL DEFAULT TRUE,
    Permissions  VARCHAR(100) NULL,
    IsActive     BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAtUtc DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    LastUsedAtUtc DATETIME(3) NULL,
    FOREIGN KEY (UserId) REFERENCES auth_users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_trading_api_keys_user ON trading_binance_api_keys(UserId);

-- =============================================
-- 6) BILLING TABLES
-- =============================================

-- billing.Subscriptions
CREATE TABLE IF NOT EXISTS billing_subscriptions (
    Id               CHAR(36) NOT NULL PRIMARY KEY,
    UserId           CHAR(36) NOT NULL,
    PlanType         INT NOT NULL,
    Status           VARCHAR(16) NOT NULL,
    StripeSubscriptionId VARCHAR(128) NULL,
    CurrentPeriodStartUtc DATETIME(3) NOT NULL,
    CurrentPeriodEndUtc   DATETIME(3) NOT NULL,
    CreatedAtUtc     DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    CanceledAtUtc    DATETIME(3) NULL,
    UpdatedAtUtc     DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    FOREIGN KEY (UserId) REFERENCES auth_users(Id) ON DELETE CASCADE,
    UNIQUE KEY UX_billing_sub_user (UserId)
);

-- billing.PaymentHistory
CREATE TABLE IF NOT EXISTS billing_payment_history (
    Id                   CHAR(36) NOT NULL PRIMARY KEY,
    UserId               CHAR(36) NOT NULL,
    SubscriptionId       CHAR(36) NULL,
    Amount               DECIMAL(18,2) NOT NULL,
    Currency             VARCHAR(3) NOT NULL DEFAULT 'USD',
    Status               VARCHAR(16) NOT NULL,
    StripePaymentIntentId VARCHAR(128) NULL,
    PaymentMethod        VARCHAR(32) NULL,
    CreatedAtUtc         DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    FOREIGN KEY (UserId) REFERENCES auth_users(Id) ON DELETE CASCADE,
    FOREIGN KEY (SubscriptionId) REFERENCES billing_subscriptions(Id) ON DELETE SET NULL
);

CREATE INDEX IX_billing_payment_user_time ON billing_payment_history(UserId, CreatedAtUtc DESC);

-- =============================================
-- 7) OPS TABLES
-- =============================================

-- ops.Idempotency
CREATE TABLE IF NOT EXISTS ops_idempotency (
    `Key`          VARCHAR(100) NOT NULL PRIMARY KEY,
    UserId         CHAR(36) NULL,
    RequestHash    BINARY(32) NOT NULL,
    ResponseCode   INT NOT NULL,
    ResponseBody   LONGBLOB NULL,
    CreatedAtUtc   DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    FOREIGN KEY (UserId) REFERENCES auth_users(Id) ON DELETE SET NULL
);

CREATE INDEX IX_ops_idempotency_user ON ops_idempotency(UserId);

-- ops.Outbox
CREATE TABLE IF NOT EXISTS ops_outbox (
    Id             BIGINT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    Aggregate      VARCHAR(32) NOT NULL,
    AggregateId    CHAR(36) NOT NULL,
    EventType      VARCHAR(64) NOT NULL,
    Payload        JSON NOT NULL,
    OccurredAtUtc  DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    ProcessedAtUtc DATETIME(3) NULL
);

CREATE INDEX IX_ops_outbox_unprocessed ON ops_outbox(ProcessedAtUtc);

-- ops.Inbox
CREATE TABLE IF NOT EXISTS ops_inbox (
    Id             BIGINT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    Source         VARCHAR(32) NOT NULL,
    ExternalId     VARCHAR(128) NOT NULL,
    EventType      VARCHAR(64) NULL,
    ReceivedAtUtc  DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    ProcessedAtUtc DATETIME(3) NULL,
    UNIQUE KEY UX_ops_inbox_source_external (Source, ExternalId)
);

SELECT 'Initial schema created successfully!' as message;
