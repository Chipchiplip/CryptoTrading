CREATE TABLE AiGeneratedBotProfiles (
    Id CHAR(36) NOT NULL PRIMARY KEY,
    UserId INT NOT NULL,
    SessionId CHAR(36) NULL,
    Name VARCHAR(200) NOT NULL,
    SymbolsJson TEXT NOT NULL,
    StrategyType VARCHAR(50) NOT NULL,
    RiskMode VARCHAR(20) NOT NULL,
    MaxCapitalPerTrade DECIMAL(28,8) NOT NULL,
    MaxDailyExposure DECIMAL(28,8) NOT NULL,
    TimeHorizon VARCHAR(50) NOT NULL,
    ExpectedReturnPct DECIMAL(9,4) NULL,
    RiskNote TEXT NULL,
    SourceRecommendationId VARCHAR(100) NULL,
    CreatedAtUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX IX_AiGeneratedBotProfiles_UserId (UserId)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

