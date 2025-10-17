-- =============================================
-- Crypto Trading Web - Initial Database Schema
-- SQL Server 2022
-- Version: 1.0.0
-- =============================================

-- 0) Create Database & Configuration
-- =============================================
IF DB_ID('CtwDb') IS NULL
BEGIN
  CREATE DATABASE CtwDb;
END
GO

USE CtwDb;
GO

-- Enable snapshot isolation (reduces deadlocks)
ALTER DATABASE [CtwDb] SET READ_COMMITTED_SNAPSHOT ON;
ALTER DATABASE [CtwDb] SET ALLOW_SNAPSHOT_ISOLATION ON;
GO

-- 1) Create Schemas
-- =============================================
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'auth')
  CREATE SCHEMA auth;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'market')
  CREATE SCHEMA market;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'portfolio')
  CREATE SCHEMA portfolio;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'trading')
  CREATE SCHEMA trading;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'billing')
  CREATE SCHEMA billing;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'ops')
  CREATE SCHEMA ops;
GO

-- =============================================
-- 2) AUTH SCHEMA
-- =============================================

-- auth.Users
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users' AND schema_id = SCHEMA_ID('auth'))
BEGIN
  CREATE TABLE auth.Users (
    Id               UNIQUEIDENTIFIER NOT NULL 
                       CONSTRAINT PK_auth_Users DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    Email            NVARCHAR(255) NOT NULL,
    PasswordHash     VARBINARY(256) NOT NULL,
    PasswordSalt     VARBINARY(128) NOT NULL,
    FirstName        NVARCHAR(100) NULL,
    LastName         NVARCHAR(100) NULL,
    PhoneNumber      NVARCHAR(20)  NULL,
    IsEmailVerified  BIT            NOT NULL CONSTRAINT DF_auth_Users_IsEmailVerified  DEFAULT 0,
    TwoFactorEnabled BIT            NOT NULL CONSTRAINT DF_auth_Users_TwoFactorEnabled DEFAULT 0,
    TwoFactorSecret  NVARCHAR(255) NULL,
    SubscriptionTier INT            NOT NULL CONSTRAINT DF_auth_Users_SubTier DEFAULT 0, -- 0=Free,1=Plus,2=Pro
    IsActive         BIT            NOT NULL CONSTRAINT DF_auth_Users_IsActive DEFAULT 1,
    ProfileImageUrl  NVARCHAR(500)  NULL,
    CreatedAtUtc     DATETIME2(3)   NOT NULL CONSTRAINT DF_auth_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc     DATETIME2(3)   NOT NULL CONSTRAINT DF_auth_Users_UpdatedAt DEFAULT SYSUTCDATETIME(),
    LastLoginAtUtc   DATETIME2(3)   NULL,
    RowVersion       ROWVERSION
  );

  CREATE UNIQUE INDEX UX_auth_Users_Email ON auth.Users(Email);
END
GO

-- auth.RefreshTokens
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RefreshTokens' AND schema_id = SCHEMA_ID('auth'))
BEGIN
  CREATE TABLE auth.RefreshTokens (
    Id           UNIQUEIDENTIFIER NOT NULL 
                   CONSTRAINT PK_auth_RefreshTokens DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId       UNIQUEIDENTIFIER NOT NULL 
        CONSTRAINT FK_auth_RefreshTokens_User 
        REFERENCES auth.Users(Id) ON DELETE CASCADE,
    Token        NVARCHAR(500) NOT NULL,
    ExpiresAtUtc DATETIME2(3)  NOT NULL,
    CreatedAtUtc DATETIME2(3)  NOT NULL CONSTRAINT DF_auth_RefreshTokens_Created DEFAULT SYSUTCDATETIME(),
    RevokedAtUtc DATETIME2(3)  NULL,
    ReplacedBy   NVARCHAR(500) NULL,
    IpAddress    NVARCHAR(45)  NULL,
    UserAgent    NVARCHAR(500) NULL,
    RowVersion   ROWVERSION
  );

  CREATE INDEX IX_auth_RefreshTokens_User ON auth.RefreshTokens(UserId);
  CREATE UNIQUE INDEX UX_auth_RefreshTokens_Token ON auth.RefreshTokens(Token);
END
GO

-- auth.UserActivity
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserActivity' AND schema_id = SCHEMA_ID('auth'))
BEGIN
  CREATE TABLE auth.UserActivity (
    Id          UNIQUEIDENTIFIER NOT NULL 
                  CONSTRAINT PK_auth_UserActivity DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId      UNIQUEIDENTIFIER NOT NULL 
        CONSTRAINT FK_auth_UserActivity_User 
        REFERENCES auth.Users(Id) ON DELETE CASCADE,
    Action      NVARCHAR(100) NOT NULL,  -- 'LOGIN','VIEW_CRYPTO','PLACE_ORDER'
    Details     NVARCHAR(MAX) NULL,      -- JSON
    IpAddress   NVARCHAR(45)  NULL,
    UserAgent   NVARCHAR(500) NULL,
    CreatedAtUtc DATETIME2(3)  NOT NULL CONSTRAINT DF_auth_UserActivity_Created DEFAULT SYSUTCDATETIME(),
    RowVersion  ROWVERSION
  );

  CREATE INDEX IX_auth_UserActivity_User_Time 
  ON auth.UserActivity(UserId, CreatedAtUtc DESC) 
  INCLUDE(Action);
END
GO

-- auth.AuditLogs (security audit)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuditLogs' AND schema_id = SCHEMA_ID('auth'))
BEGIN
  CREATE TABLE auth.AuditLogs (
    Id            BIGINT IDENTITY(1,1) NOT NULL 
                    CONSTRAINT PK_auth_AuditLogs PRIMARY KEY,
    UserId        UNIQUEIDENTIFIER NULL 
        CONSTRAINT FK_auth_AuditLogs_User 
        REFERENCES auth.Users(Id) ON DELETE SET NULL,
    Action        NVARCHAR(100) NOT NULL,
    EntityType    NVARCHAR(50)  NOT NULL,  -- 'Order','User','Subscription'
    EntityId      NVARCHAR(100) NULL,
    OldValues     NVARCHAR(MAX) NULL,      -- JSON
    NewValues     NVARCHAR(MAX) NULL,      -- JSON
    IpAddress     NVARCHAR(45)  NULL,
    UserAgent     NVARCHAR(500) NULL,
    Severity      VARCHAR(20)   NOT NULL,  -- 'INFO','WARNING','CRITICAL'
    CreatedAtUtc  DATETIME2(3)  NOT NULL CONSTRAINT DF_auth_AuditLogs_Created DEFAULT SYSUTCDATETIME()
  );

  CREATE INDEX IX_auth_AuditLogs_User_Time ON auth.AuditLogs(UserId, CreatedAtUtc DESC);
  CREATE INDEX IX_auth_AuditLogs_Entity ON auth.AuditLogs(EntityType, EntityId);
END
GO

-- =============================================
-- 3) MARKET SCHEMA
-- =============================================

-- market.Cryptocurrencies
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Cryptocurrencies' AND schema_id = SCHEMA_ID('market'))
BEGIN
  CREATE TABLE market.Cryptocurrencies (
    Id             INT IDENTITY(1,1) NOT NULL 
                     CONSTRAINT PK_market_Cryptocurrencies PRIMARY KEY,
    CoinGeckoId    NVARCHAR(100) NOT NULL,
    Symbol         VARCHAR(24)   NOT NULL,
    Name           NVARCHAR(100) NOT NULL,
    IconUrl        NVARCHAR(500) NULL,
    MarketCapRank  INT           NULL,
    IsActive       BIT           NOT NULL CONSTRAINT DF_market_Crypto_IsActive DEFAULT 1,
    CreatedAtUtc   DATETIME2(3)  NOT NULL CONSTRAINT DF_market_Crypto_Created DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc   DATETIME2(3)  NOT NULL CONSTRAINT DF_market_Crypto_Updated DEFAULT SYSUTCDATETIME(),
    RowVersion     ROWVERSION
  );

  CREATE UNIQUE INDEX UX_market_Crypto_Symbol      ON market.Cryptocurrencies(Symbol);
  CREATE UNIQUE INDEX UX_market_Crypto_CoinGeckoId ON market.Cryptocurrencies(CoinGeckoId);
END
GO

-- market.CryptoPrices
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CryptoPrices' AND schema_id = SCHEMA_ID('market'))
BEGIN
  CREATE TABLE market.CryptoPrices (
    Id               BIGINT IDENTITY(1,1) NOT NULL 
                       CONSTRAINT PK_market_CryptoPrices PRIMARY KEY,
    CryptocurrencyId INT NOT NULL 
        CONSTRAINT FK_market_Prices_Crypto 
        REFERENCES market.Cryptocurrencies(Id) ON DELETE CASCADE,
    PriceUsd         DECIMAL(28,8) NOT NULL,
    MarketCap        DECIMAL(28,2) NULL,
    Volume24h        DECIMAL(28,2) NULL,
    PercentChange1h  DECIMAL(10,4)  NULL,
    PercentChange24h DECIMAL(10,4)  NULL,
    PercentChange7d  DECIMAL(10,4)  NULL,
    CirculatingSupply DECIMAL(28,2) NULL,
    TotalSupply      DECIMAL(28,2) NULL,
    CollectedAtUtc   DATETIME2(3)   NOT NULL CONSTRAINT DF_market_Prices_At DEFAULT SYSUTCDATETIME(),
    RowVersion       ROWVERSION
  );

  CREATE INDEX IX_market_Prices_Crypto_Time
  ON market.CryptoPrices(CryptocurrencyId, CollectedAtUtc DESC)
  INCLUDE (PriceUsd, Volume24h, MarketCap);
END
GO

-- market.MarketStats
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MarketStats' AND schema_id = SCHEMA_ID('market'))
BEGIN
  CREATE TABLE market.MarketStats (
    Id                           UNIQUEIDENTIFIER NOT NULL 
                                    CONSTRAINT PK_market_MarketStats DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    TotalMarketCap               DECIMAL(28,2) NOT NULL,
    TotalVolume24h               DECIMAL(28,2) NOT NULL,
    MarketCapChangePercentage24h DECIMAL(10,4) NULL,
    ActiveCryptocurrencies       INT NULL,
    FearGreedIndex               INT NULL,
    FearGreedClassification      NVARCHAR(50) NULL,
    TimestampUtc                 DATETIME2(3) NOT NULL CONSTRAINT DF_market_Stats_Ts DEFAULT SYSUTCDATETIME(),
    RowVersion                   ROWVERSION
  );

  CREATE INDEX IX_market_Stats_Ts ON market.MarketStats(TimestampUtc DESC);
END
GO

-- =============================================
-- 4) PORTFOLIO SCHEMA
-- =============================================

-- portfolio.UserWatchlists
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserWatchlists' AND schema_id = SCHEMA_ID('portfolio'))
BEGIN
  CREATE TABLE portfolio.UserWatchlists (
    Id           UNIQUEIDENTIFIER NOT NULL 
                   CONSTRAINT PK_portfolio_UserWatchlists DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId       UNIQUEIDENTIFIER NOT NULL 
        CONSTRAINT FK_portfolio_Watchlists_User 
        REFERENCES auth.Users(Id) ON DELETE CASCADE,
    Name         NVARCHAR(100) NOT NULL,
    IsDefault    BIT           NOT NULL CONSTRAINT DF_portfolio_Watch_IsDefault DEFAULT 0,
    CreatedAtUtc DATETIME2(3)  NOT NULL CONSTRAINT DF_portfolio_Watch_Created DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc DATETIME2(3)  NOT NULL CONSTRAINT DF_portfolio_Watch_Updated DEFAULT SYSUTCDATETIME(),
    RowVersion   ROWVERSION
  );

  CREATE INDEX IX_portfolio_Watchlists_User ON portfolio.UserWatchlists(UserId);
END
GO

-- portfolio.WatchlistItems
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WatchlistItems' AND schema_id = SCHEMA_ID('portfolio'))
BEGIN
  CREATE TABLE portfolio.WatchlistItems (
    Id               UNIQUEIDENTIFIER NOT NULL 
                       CONSTRAINT PK_portfolio_WatchlistItems DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    WatchlistId      UNIQUEIDENTIFIER NOT NULL 
        CONSTRAINT FK_portfolio_WatchItems_List 
        REFERENCES portfolio.UserWatchlists(Id) ON DELETE CASCADE,
    CryptocurrencyId INT NOT NULL 
        CONSTRAINT FK_portfolio_WatchItems_Crypto 
        REFERENCES market.Cryptocurrencies(Id),
    AddedAtUtc       DATETIME2(3) NOT NULL CONSTRAINT DF_portfolio_WatchItems_Added DEFAULT SYSUTCDATETIME(),
    RowVersion       ROWVERSION
  );

  CREATE UNIQUE INDEX UX_portfolio_WatchItems_UQ 
  ON portfolio.WatchlistItems(WatchlistId, CryptocurrencyId);
END
GO

-- portfolio.Positions
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Positions' AND schema_id = SCHEMA_ID('portfolio'))
BEGIN
  CREATE TABLE portfolio.Positions (
    Id            UNIQUEIDENTIFIER NOT NULL 
                    CONSTRAINT PK_portfolio_Positions DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId        UNIQUEIDENTIFIER NOT NULL 
        CONSTRAINT FK_portfolio_Positions_User 
        REFERENCES auth.Users(Id) ON DELETE CASCADE,
    CryptocurrencyId INT NOT NULL 
        CONSTRAINT FK_portfolio_Positions_Crypto 
        REFERENCES market.Cryptocurrencies(Id),
    Quantity      DECIMAL(28,8) NOT NULL,
    AvgPriceUsd   DECIMAL(28,8) NOT NULL,
    UpdatedAtUtc  DATETIME2(3)   NOT NULL CONSTRAINT DF_portfolio_Positions_Updated DEFAULT SYSUTCDATETIME(),
    RowVersion    ROWVERSION
  );

  CREATE UNIQUE INDEX UX_portfolio_Positions_User_Coin 
  ON portfolio.Positions(UserId, CryptocurrencyId);
END
GO

-- =============================================
-- 5) TRADING SCHEMA
-- =============================================

-- trading.Balances
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Balances' AND schema_id = SCHEMA_ID('trading'))
BEGIN
  CREATE TABLE trading.Balances (
    Id            UNIQUEIDENTIFIER NOT NULL 
                    CONSTRAINT PK_trading_Balances DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId        UNIQUEIDENTIFIER NOT NULL 
        CONSTRAINT FK_trading_Balances_User 
        REFERENCES auth.Users(Id) ON DELETE CASCADE,
    Asset         VARCHAR(16) NOT NULL, -- 'USDT','BTC'
    Amount        DECIMAL(28,8) NOT NULL 
        CONSTRAINT CK_trading_Balances_Amount_NonNegative CHECK (Amount >= 0),
    UpdatedAtUtc  DATETIME2(3) NOT NULL CONSTRAINT DF_trading_Balances_Updated DEFAULT SYSUTCDATETIME(),
    RowVersion    ROWVERSION
  );

  CREATE UNIQUE INDEX UX_trading_Balances_User_Asset ON trading.Balances(UserId, Asset);
END
GO

-- trading.Bots
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Bots' AND schema_id = SCHEMA_ID('trading'))
BEGIN
  CREATE TABLE trading.Bots (
    Id           UNIQUEIDENTIFIER NOT NULL 
                   CONSTRAINT PK_trading_Bots DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId       UNIQUEIDENTIFIER NOT NULL 
        CONSTRAINT FK_trading_Bots_User 
        REFERENCES auth.Users(Id) ON DELETE CASCADE,
    Name         NVARCHAR(100) NOT NULL,
    Strategy     VARCHAR(32)   NOT NULL, -- 'DCA','GRID','MARTINGALE'
    Symbol       VARCHAR(24)   NOT NULL, -- 'BTCUSDT'
    IsActive     BIT           NOT NULL CONSTRAINT DF_trading_Bots_IsActive DEFAULT 0,
    Config       NVARCHAR(MAX) NOT NULL, -- JSON config
    CreatedAtUtc DATETIME2(3)  NOT NULL CONSTRAINT DF_trading_Bots_Created DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc DATETIME2(3)  NOT NULL CONSTRAINT DF_trading_Bots_Updated DEFAULT SYSUTCDATETIME(),
    LastRunAtUtc DATETIME2(3)  NULL,
    RowVersion   ROWVERSION
  );

  CREATE INDEX IX_trading_Bots_User ON trading.Bots(UserId);
  CREATE INDEX IX_trading_Bots_Active ON trading.Bots(IsActive) WHERE IsActive = 1;
END
GO

-- trading.Orders (with temporal/history table)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Orders' AND schema_id = SCHEMA_ID('trading'))
BEGIN
  CREATE TABLE trading.Orders (
    Id             UNIQUEIDENTIFIER NOT NULL 
                      CONSTRAINT PK_trading_Orders DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId         UNIQUEIDENTIFIER NOT NULL 
        CONSTRAINT FK_trading_Orders_User 
        REFERENCES auth.Users(Id) ON DELETE CASCADE,
    BotId          UNIQUEIDENTIFIER NULL
        CONSTRAINT FK_trading_Orders_Bot 
        REFERENCES trading.Bots(Id) ON DELETE SET NULL,
    BinanceOrderId NVARCHAR(128) NULL,
    Symbol         VARCHAR(24)   NOT NULL, -- 'BTCUSDT'
    Side           VARCHAR(4)    NOT NULL  CONSTRAINT CK_trading_Orders_Side CHECK (Side IN ('BUY','SELL')),
    Type           VARCHAR(12)   NOT NULL  CONSTRAINT CK_trading_Orders_Type CHECK (Type IN ('MARKET','LIMIT','STOP','STOP_LIMIT')),
    Quantity       DECIMAL(28,8) NOT NULL CONSTRAINT CK_trading_Orders_Qty_Positive CHECK (Quantity > 0),
    PriceUsd       DECIMAL(28,8) NULL     CONSTRAINT CK_trading_Orders_Price_Positive CHECK (PriceUsd IS NULL OR PriceUsd > 0),
    Status         VARCHAR(12)   NOT NULL, -- 'PENDING','OPEN','FILLED','CANCELED'
    ExecutedQty    DECIMAL(28,8) NOT NULL CONSTRAINT DF_trading_Orders_ExecQty DEFAULT 0,
    ExecutedPrice  DECIMAL(28,8) NULL,
    Commission     DECIMAL(28,8) NULL,
    CommissionAsset VARCHAR(16)  NULL,
    CreatedAtUtc   DATETIME2(3)  NOT NULL CONSTRAINT DF_trading_Orders_Created DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc   DATETIME2(3)  NOT NULL CONSTRAINT DF_trading_Orders_Updated DEFAULT SYSUTCDATETIME(),
    RowVersion     ROWVERSION,

    -- Temporal columns
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN,
    ValidTo   DATETIME2 GENERATED ALWAYS AS ROW END   HIDDEN,
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
  ) 
  WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = trading.OrdersHistory));

  CREATE INDEX IX_trading_Orders_User_Time 
  ON trading.Orders(UserId, CreatedAtUtc DESC)
  INCLUDE (Symbol, Side, Type, PriceUsd, Quantity, Status);
  
  CREATE INDEX IX_trading_Orders_ExternalId ON trading.Orders(BinanceOrderId);
END
GO

-- trading.Trades
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Trades' AND schema_id = SCHEMA_ID('trading'))
BEGIN
  CREATE TABLE trading.Trades (
    Id            BIGINT IDENTITY(1,1) NOT NULL 
                    CONSTRAINT PK_trading_Trades PRIMARY KEY,
    OrderId       UNIQUEIDENTIFIER NOT NULL 
        CONSTRAINT FK_trading_Trades_Order 
        REFERENCES trading.Orders(Id) ON DELETE CASCADE,
    FillPriceUsd  DECIMAL(28,8) NOT NULL,
    FillQty       DECIMAL(28,8) NOT NULL,
    FeeAsset      VARCHAR(16) NULL,
    FeeAmount     DECIMAL(28,8) NULL,
    FilledAtUtc   DATETIME2(3) NOT NULL,
    RowVersion    ROWVERSION
  );

  CREATE INDEX IX_trading_Trades_Order ON trading.Trades(OrderId);
END
GO

-- trading.BinanceApiKeys
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BinanceApiKeys' AND schema_id = SCHEMA_ID('trading'))
BEGIN
  CREATE TABLE trading.BinanceApiKeys (
    Id           UNIQUEIDENTIFIER NOT NULL 
                   CONSTRAINT PK_trading_BinanceApiKeys DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId       UNIQUEIDENTIFIER NOT NULL 
        CONSTRAINT FK_trading_ApiKeys_User 
        REFERENCES auth.Users(Id) ON DELETE CASCADE,
    ApiKeyEnc    VARBINARY(MAX)   NOT NULL,   -- encrypted
    SecretKeyEnc VARBINARY(MAX)   NOT NULL,   -- encrypted
    Last4        VARCHAR(8)       NULL,       -- for display
    IsTestnet    BIT              NOT NULL CONSTRAINT DF_trading_ApiKeys_Test DEFAULT 1,
    Permissions  NVARCHAR(100)    NULL,       -- 'SPOT','FUTURES'
    IsActive     BIT              NOT NULL CONSTRAINT DF_trading_ApiKeys_Active DEFAULT 1,
    CreatedAtUtc DATETIME2(3)     NOT NULL CONSTRAINT DF_trading_ApiKeys_Created DEFAULT SYSUTCDATETIME(),
    LastUsedAtUtc DATETIME2(3)    NULL,
    RowVersion   ROWVERSION
  );

  CREATE INDEX IX_trading_ApiKeys_User ON trading.BinanceApiKeys(UserId);
END
GO

-- =============================================
-- 6) BILLING SCHEMA
-- =============================================

-- billing.Subscriptions (with temporal/history table)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Subscriptions' AND schema_id = SCHEMA_ID('billing'))
BEGIN
  CREATE TABLE billing.Subscriptions (
    Id               UNIQUEIDENTIFIER NOT NULL 
                        CONSTRAINT PK_billing_Subscriptions DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId           UNIQUEIDENTIFIER NOT NULL 
        CONSTRAINT FK_billing_Subscriptions_User 
        REFERENCES auth.Users(Id) ON DELETE CASCADE,
    PlanType         INT NOT NULL,         -- 1=Plus, 2=Pro
    Status           VARCHAR(16) NOT NULL, -- 'ACTIVE','PAST_DUE','CANCELED','EXPIRED'
    StripeSubscriptionId NVARCHAR(128) NULL,
    CurrentPeriodStartUtc DATETIME2(3) NOT NULL,
    CurrentPeriodEndUtc   DATETIME2(3) NOT NULL,
    CreatedAtUtc     DATETIME2(3) NOT NULL CONSTRAINT DF_billing_Sub_Created DEFAULT SYSUTCDATETIME(),
    CanceledAtUtc    DATETIME2(3) NULL,
    UpdatedAtUtc     DATETIME2(3) NOT NULL CONSTRAINT DF_billing_Sub_Updated DEFAULT SYSUTCDATETIME(),
    RowVersion       ROWVERSION,

    -- Temporal
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN,
    ValidTo   DATETIME2 GENERATED ALWAYS AS ROW END   HIDDEN,
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
  )
  WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = billing.SubscriptionsHistory));

  CREATE UNIQUE INDEX UX_billing_Sub_User ON billing.Subscriptions(UserId);
END
GO

-- billing.PaymentHistory
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PaymentHistory' AND schema_id = SCHEMA_ID('billing'))
BEGIN
  CREATE TABLE billing.PaymentHistory (
    Id                   UNIQUEIDENTIFIER NOT NULL 
                            CONSTRAINT PK_billing_PaymentHistory DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    UserId               UNIQUEIDENTIFIER NOT NULL 
        CONSTRAINT FK_billing_PaymentHistory_User 
        REFERENCES auth.Users(Id) ON DELETE CASCADE,
    SubscriptionId       UNIQUEIDENTIFIER NULL 
        CONSTRAINT FK_billing_PaymentHistory_Sub 
        REFERENCES billing.Subscriptions(Id) ON DELETE SET NULL,
    Amount               DECIMAL(18,2) NOT NULL,
    Currency             VARCHAR(3)     NOT NULL CONSTRAINT DF_billing_Payment_Currency DEFAULT 'USD',
    Status               VARCHAR(16)    NOT NULL, -- 'SUCCESS','FAILED','PENDING'
    StripePaymentIntentId NVARCHAR(128) NULL,
    PaymentMethod        VARCHAR(32)    NULL,    -- 'CARD','PAYPAL'
    CreatedAtUtc         DATETIME2(3)   NOT NULL CONSTRAINT DF_billing_Payment_Created DEFAULT SYSUTCDATETIME(),
    RowVersion           ROWVERSION
  );

  CREATE INDEX IX_billing_Payment_User_Time ON billing.PaymentHistory(UserId, CreatedAtUtc DESC);
END
GO

-- =============================================
-- 7) OPS SCHEMA (Idempotency, Outbox, Inbox)
-- =============================================

-- ops.Idempotency
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Idempotency' AND schema_id = SCHEMA_ID('ops'))
BEGIN
  CREATE TABLE ops.Idempotency (
    [Key]          NVARCHAR(100) NOT NULL CONSTRAINT PK_ops_Idempotency PRIMARY KEY,
    UserId         UNIQUEIDENTIFIER NULL,
    RequestHash    BINARY(32)    NOT NULL,
    ResponseCode   INT           NOT NULL,
    ResponseBody   VARBINARY(MAX) NULL,
    CreatedAtUtc   DATETIME2(3)  NOT NULL CONSTRAINT DF_ops_Idem_Created DEFAULT SYSUTCDATETIME()
  );

  CREATE INDEX IX_ops_Idempotency_User ON ops.Idempotency(UserId);
END
GO

-- ops.Outbox
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Outbox' AND schema_id = SCHEMA_ID('ops'))
BEGIN
  CREATE TABLE ops.Outbox (
    Id             BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ops_Outbox PRIMARY KEY,
    Aggregate      VARCHAR(32)  NOT NULL,      -- 'Order','Subscription'
    AggregateId    UNIQUEIDENTIFIER NOT NULL,
    EventType      VARCHAR(64)  NOT NULL,
    Payload        NVARCHAR(MAX) NOT NULL,
    OccurredAtUtc  DATETIME2(3) NOT NULL CONSTRAINT DF_ops_Outbox_Occ DEFAULT SYSUTCDATETIME(),
    ProcessedAtUtc DATETIME2(3) NULL
  );

  CREATE INDEX IX_ops_Outbox_Unprocessed ON ops.Outbox(ProcessedAtUtc) WHERE ProcessedAtUtc IS NULL;
END
GO

-- ops.Inbox
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Inbox' AND schema_id = SCHEMA_ID('ops'))
BEGIN
  CREATE TABLE ops.Inbox (
    Id             BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ops_Inbox PRIMARY KEY,
    Source         VARCHAR(32)   NOT NULL,   -- 'Stripe','Binance'
    ExternalId     NVARCHAR(128) NOT NULL,   -- external event id
    EventType      VARCHAR(64)   NULL,
    ReceivedAtUtc  DATETIME2(3)  NOT NULL CONSTRAINT DF_ops_Inbox_Recv DEFAULT SYSUTCDATETIME(),
    ProcessedAtUtc DATETIME2(3)  NULL
  );

  CREATE UNIQUE INDEX UX_ops_Inbox_Source_External ON ops.Inbox(Source, ExternalId);
END
GO

-- =============================================
-- 8) Database Roles & Security
-- =============================================

-- Create application roles
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'role_auth_owner')
  CREATE ROLE role_auth_owner;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'role_market_writer')
  CREATE ROLE role_market_writer;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'role_portfolio_writer')
  CREATE ROLE role_portfolio_writer;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'role_trading_writer')
  CREATE ROLE role_trading_writer;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'role_billing_writer')
  CREATE ROLE role_billing_writer;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'role_ops_writer')
  CREATE ROLE role_ops_writer;
GO

-- Grant permissions (adjust as needed)
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::auth      TO role_auth_owner;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::market    TO role_market_writer;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::portfolio TO role_portfolio_writer;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::trading   TO role_trading_writer;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::billing   TO role_billing_writer;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::ops       TO role_ops_writer;
GO

PRINT 'Initial schema created successfully!';
GO

