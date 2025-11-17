-- ============================================================
-- QUICK TEST SCRIPT FOR NEW RISK TABLES
-- This script provisions a temporary bot plus baseline rows in:
--   • BotRiskConfigurations
--   • BotRiskStates
--   • KillSwitchEvents
-- so you can verify the tables without touching production bots.
-- ============================================================

USE crypto_trading;

SELECT 'Starting quick risk table smoke test...' AS message;

-- Pick the first active user as owner (fallback to 1 if table empty)
SET @demo_user_id = (
    SELECT Id FROM Users ORDER BY Id LIMIT 1
);
SET @demo_user_id = IFNULL(@demo_user_id, 1);

SET @demo_strategy_id = UUID();
SET @demo_bot_id = UUID();

-- Create a disposable bot + strategy so FK constraints pass
INSERT INTO BotStrategyDefinitions (Id, StrategyKey, Version, DisplayName, Description, IsActive, CreatedAt)
VALUES (@demo_strategy_id, 'RISK_SMOKE_TEST', '1.0.0', 'Risk Smoke Test', 'Temp strategy for smoke tests', TRUE, NOW())
ON DUPLICATE KEY UPDATE UpdatedAt = NOW();

INSERT INTO TradingBots (Id, UserId, StrategyDefinitionId, Name, Status, BaseAsset, QuoteAsset, ExecutionIntervalSeconds, CreatedAt, UpdatedAt)
VALUES (
    @demo_bot_id,
    @demo_user_id,
    @demo_strategy_id,
    'Risk Smoke Test Bot',
    'Running',
    'BTC',
    'USDT',
    60,
    NOW(),
    NOW()
)
ON DUPLICATE KEY UPDATE UpdatedAt = NOW(), Status = 'Running';

-- 1) BotRiskConfigurations demo-safe defaults
INSERT INTO BotRiskConfigurations (UserId, BotId, MaxAllowedCapital, MaxSlippage, MaxDailyLoss, MaxConsecutiveLosses, CooldownSeconds, KillSwitchEnabled, CreatedAt)
SELECT
    @demo_user_id,
    @demo_bot_id,
    100.00,
    0.0100,
    10.00,
    3,
    120,
    0,
    NOW()
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1 FROM BotRiskConfigurations WHERE BotId = @demo_bot_id
);

-- 2) BotRiskStates baseline row
INSERT INTO BotRiskStates (BotId, ConsecutiveLosses, DailyLoss, DailyLossResetAt, TotalDrawdown, LastOrderAt, OrderCountThisCycle, UpdatedAt)
VALUES (
    @demo_bot_id,
    0,
    0,
    NOW(),
    0,
    NULL,
    0,
    NOW()
)
ON DUPLICATE KEY UPDATE UpdatedAt = NOW();

-- 3) KillSwitchEvents sample record
INSERT INTO KillSwitchEvents (BotId, TriggerReason, TriggerTime, TotalLoss, ConsecutiveLosses, CreatedAt)
VALUES (
    @demo_bot_id,
    'Manual smoke test trigger',
    NOW(),
    5.00,
    1,
    NOW()
);

-- Show what we inserted
SELECT 'BotRiskConfigurations' AS table_name, * FROM BotRiskConfigurations WHERE BotId = @demo_bot_id;
SELECT 'BotRiskStates' AS table_name, * FROM BotRiskStates WHERE BotId = @demo_bot_id;
SELECT 'KillSwitchEvents' AS table_name, * FROM KillSwitchEvents WHERE BotId = @demo_bot_id ORDER BY TriggerTime DESC LIMIT 1;

SELECT '✓ Quick risk table smoke test complete.' AS message;

