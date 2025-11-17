-- ============================================================
-- CREATE TEST BOT AND BOT RISK CONFIGURATION
-- Simple script to create a test bot using existing strategy
-- ============================================================

USE crypto_trading;

-- Use an existing strategy (momentum-scalping)
SET @strategy_id = '5d0aeb8c-8096-4848-b425-bcc80c4f2c69';  -- momentum-scalping
SET @bot_id = UUID();
SET @user_id = 1;  -- Change this to your user ID if needed

-- Create test bot
INSERT INTO TradingBots (
    Id, 
    UserId, 
    StrategyDefinitionId, 
    Name, 
    Status, 
    BaseAsset, 
    QuoteAsset, 
    ExecutionIntervalSeconds, 
    CreatedAt
)
VALUES (
    @bot_id,
    @user_id,
    @strategy_id,
    'Test Bot for Risk Config',
    'Draft',
    'BTC',
    'USDT',
    60,
    NOW()
);

-- Create BotRiskConfiguration for this bot
INSERT INTO BotRiskConfigurations (
    UserId, 
    BotId, 
    MaxAllowedCapital, 
    MaxSlippage, 
    MaxDailyLoss,
    MaxConsecutiveLosses,
    CooldownSeconds,
    KillSwitchEnabled
) 
VALUES (
    @user_id,
    @bot_id,
    1000,
    0.05,
    0.1,
    5,
    300,
    TRUE
);

-- Show results
SELECT 'Created Bot:' AS info, Id, Name, Status, BaseAsset, QuoteAsset FROM TradingBots WHERE Id = @bot_id;
SELECT 'Created Risk Config:' AS info, Id, UserId, BotId, MaxAllowedCapital, MaxSlippage, MaxDailyLoss FROM BotRiskConfigurations WHERE BotId = @bot_id;

SELECT CONCAT('✓ Test bot created with ID: ', @bot_id) AS result;

