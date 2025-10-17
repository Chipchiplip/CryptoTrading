-- =============================================
-- Crypto Trading Web - Seed Data
-- SQL Server 2022
-- Version: 1.0.0
-- =============================================

USE CtwDb;
GO

-- =============================================
-- 1) Seed Cryptocurrencies (Top 20)
-- =============================================

PRINT 'Seeding cryptocurrencies...';

IF NOT EXISTS (SELECT 1 FROM market.Cryptocurrencies WHERE Symbol='BTC')
BEGIN
  SET IDENTITY_INSERT market.Cryptocurrencies ON;

  INSERT INTO market.Cryptocurrencies (Id, CoinGeckoId, Symbol, Name, IconUrl, MarketCapRank, IsActive)
  VALUES
    (1, N'bitcoin',   'BTC',  N'Bitcoin',          N'https://assets.coingecko.com/coins/images/1/large/bitcoin.png', 1, 1),
    (2, N'ethereum',  'ETH',  N'Ethereum',         N'https://assets.coingecko.com/coins/images/279/large/ethereum.png', 2, 1),
    (3, N'tether',    'USDT', N'Tether',           N'https://assets.coingecko.com/coins/images/325/large/Tether.png', 3, 1),
    (4, N'binancecoin','BNB', N'BNB',              N'https://assets.coingecko.com/coins/images/825/large/bnb-icon2_2x.png', 4, 1),
    (5, N'solana',    'SOL',  N'Solana',           N'https://assets.coingecko.com/coins/images/4128/large/solana.png', 5, 1),
    (6, N'ripple',    'XRP',  N'XRP',              N'https://assets.coingecko.com/coins/images/44/large/xrp-symbol-white-128.png', 6, 1),
    (7, N'usd-coin',  'USDC', N'USD Coin',         N'https://assets.coingecko.com/coins/images/6319/large/USD_Coin_icon.png', 7, 1),
    (8, N'cardano',   'ADA',  N'Cardano',          N'https://assets.coingecko.com/coins/images/975/large/cardano.png', 8, 1),
    (9, N'dogecoin',  'DOGE', N'Dogecoin',         N'https://assets.coingecko.com/coins/images/5/large/dogecoin.png', 9, 1),
    (10,N'tron',      'TRX',  N'TRON',             N'https://assets.coingecko.com/coins/images/1094/large/tron-logo.png', 10, 1),
    (11,N'avalanche-2','AVAX',N'Avalanche',        N'https://assets.coingecko.com/coins/images/12559/large/coin-round-red.png', 11, 1),
    (12,N'wrapped-bitcoin','WBTC',N'Wrapped Bitcoin',N'https://assets.coingecko.com/coins/images/7598/large/wrapped_bitcoin_wbtc.png', 12, 1),
    (13,N'chainlink', 'LINK', N'Chainlink',        N'https://assets.coingecko.com/coins/images/877/large/chainlink-new-logo.png', 13, 1),
    (14,N'polkadot',  'DOT',  N'Polkadot',         N'https://assets.coingecko.com/coins/images/12171/large/polkadot.png', 14, 1),
    (15,N'polygon-ecosystem-token','POL',N'Polygon',N'https://assets.coingecko.com/coins/images/4713/large/matic-token-icon.png', 15, 1),
    (16,N'bitcoin-cash','BCH',N'Bitcoin Cash',     N'https://assets.coingecko.com/coins/images/780/large/bitcoin-cash-circle.png', 16, 1),
    (17,N'dai',       'DAI',  N'Dai',              N'https://assets.coingecko.com/coins/images/9956/large/4943.png', 17, 1),
    (18,N'litecoin',  'LTC',  N'Litecoin',         N'https://assets.coingecko.com/coins/images/2/large/litecoin.png', 18, 1),
    (19,N'uniswap',   'UNI',  N'Uniswap',          N'https://assets.coingecko.com/coins/images/12504/large/uniswap-logo.png', 19, 1),
    (20,N'stellar',   'XLM',  N'Stellar',          N'https://assets.coingecko.com/coins/images/100/large/Stellar_symbol_black_RGB.png', 20, 1);

  SET IDENTITY_INSERT market.Cryptocurrencies OFF;

  PRINT '  ✓ Seeded 20 cryptocurrencies';
END
ELSE
BEGIN
  PRINT '  ⊘ Cryptocurrencies already exist, skipping...';
END
GO

-- =============================================
-- 2) Seed Demo Users (for testing)
-- =============================================

PRINT 'Seeding demo users...';

IF NOT EXISTS (SELECT 1 FROM auth.Users WHERE Email = N'admin@cryptotrading.dev')
BEGIN
  -- Password: Admin@123 (you should hash properly in real app)
  -- Salt: random 16 bytes (example)
  DECLARE @AdminSalt VARBINARY(128) = CONVERT(VARBINARY(128), '01234567890123456789012345678901', 2);
  DECLARE @AdminHash VARBINARY(256) = HASHBYTES('SHA2_256', CONCAT('Admin@123', CONVERT(NVARCHAR(256), @AdminSalt, 2)));

  INSERT INTO auth.Users (Email, PasswordHash, PasswordSalt, FirstName, LastName, IsEmailVerified, TwoFactorEnabled, SubscriptionTier, IsActive)
  VALUES 
    (N'admin@cryptotrading.dev', @AdminHash, @AdminSalt, N'Admin', N'User', 1, 0, 2, 1),  -- Pro tier
    (N'nhat.an@cryptotrading.dev', @AdminHash, @AdminSalt, N'Nhật', N'An', 1, 0, 1, 1),   -- Plus tier
    (N'huu.triet@cryptotrading.dev', @AdminHash, @AdminSalt, N'Hữu', N'Triết', 1, 0, 1, 1),
    (N'trung.hieu@cryptotrading.dev', @AdminHash, @AdminSalt, N'Trung', N'Hiếu', 1, 0, 1, 1),
    (N'vu.hoang@cryptotrading.dev', @AdminHash, @AdminSalt, N'Vũ', N'Hoàng', 1, 0, 2, 1),
    (N'dang.khoa@cryptotrading.dev', @AdminHash, @AdminSalt, N'Đăng', N'Khoa', 1, 0, 1, 1);

  PRINT '  ✓ Seeded 6 demo users (default password: Admin@123)';
END
ELSE
BEGIN
  PRINT '  ⊘ Demo users already exist, skipping...';
END
GO

-- =============================================
-- 3) Seed Initial Market Stats
-- =============================================

PRINT 'Seeding initial market stats...';

IF NOT EXISTS (SELECT 1 FROM market.MarketStats)
BEGIN
  INSERT INTO market.MarketStats (TotalMarketCap, TotalVolume24h, MarketCapChangePercentage24h, ActiveCryptocurrencies, FearGreedIndex, FearGreedClassification)
  VALUES 
    (2500000000000.00, 85000000000.00, 2.45, 20, 65, N'Greed');

  PRINT '  ✓ Seeded initial market stats';
END
ELSE
BEGIN
  PRINT '  ⊘ Market stats already exist, skipping...';
END
GO

-- =============================================
-- 4) Seed Initial Crypto Prices (current snapshot)
-- =============================================

PRINT 'Seeding initial crypto prices...';

DECLARE @CryptoCount INT = (SELECT COUNT(*) FROM market.CryptoPrices);

IF @CryptoCount = 0
BEGIN
  INSERT INTO market.CryptoPrices (CryptocurrencyId, PriceUsd, MarketCap, Volume24h, PercentChange1h, PercentChange24h, PercentChange7d, CirculatingSupply, TotalSupply)
  SELECT 
    Id,
    CASE Symbol
      WHEN 'BTC'  THEN 65000.50
      WHEN 'ETH'  THEN 3200.75
      WHEN 'USDT' THEN 1.00
      WHEN 'BNB'  THEN 580.30
      WHEN 'SOL'  THEN 145.20
      WHEN 'XRP'  THEN 0.55
      WHEN 'USDC' THEN 1.00
      WHEN 'ADA'  THEN 0.62
      WHEN 'DOGE' THEN 0.12
      WHEN 'TRX'  THEN 0.18
      WHEN 'AVAX' THEN 35.60
      WHEN 'WBTC' THEN 64980.00
      WHEN 'LINK' THEN 14.50
      WHEN 'DOT'  THEN 7.20
      WHEN 'POL'  THEN 0.85
      WHEN 'BCH'  THEN 420.00
      WHEN 'DAI'  THEN 1.00
      WHEN 'LTC'  THEN 95.00
      WHEN 'UNI'  THEN 8.50
      WHEN 'XLM'  THEN 0.13
      ELSE 100.00
    END AS PriceUsd,
    1000000000.00 AS MarketCap,
    50000000.00 AS Volume24h,
    0.5 AS PercentChange1h,
    2.3 AS PercentChange24h,
    5.8 AS PercentChange7d,
    1000000.00 AS CirculatingSupply,
    2100000.00 AS TotalSupply
  FROM market.Cryptocurrencies
  WHERE IsActive = 1;

  PRINT '  ✓ Seeded ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' initial crypto prices';
END
ELSE
BEGIN
  PRINT '  ⊘ Crypto prices already exist, skipping...';
END
GO

-- =============================================
-- 5) Summary
-- =============================================

PRINT '';
PRINT '================================================';
PRINT 'Seed data completed successfully!';
PRINT '================================================';
PRINT '';
PRINT 'Database Statistics:';
PRINT '  • Cryptocurrencies: ' + CAST((SELECT COUNT(*) FROM market.Cryptocurrencies) AS NVARCHAR(10));
PRINT '  • Users: ' + CAST((SELECT COUNT(*) FROM auth.Users) AS NVARCHAR(10));
PRINT '  • Crypto Prices: ' + CAST((SELECT COUNT(*) FROM market.CryptoPrices) AS NVARCHAR(10));
PRINT '  • Market Stats: ' + CAST((SELECT COUNT(*) FROM market.MarketStats) AS NVARCHAR(10));
PRINT '';
PRINT 'Demo Users (Password: Admin@123):';
PRINT '  • admin@cryptotrading.dev (Pro)';
PRINT '  • nhat.an@cryptotrading.dev (Plus)';
PRINT '  • huu.triet@cryptotrading.dev (Plus)';
PRINT '  • trung.hieu@cryptotrading.dev (Plus)';
PRINT '  • vu.hoang@cryptotrading.dev (Pro)';
PRINT '  • dang.khoa@cryptotrading.dev (Plus)';
PRINT '';
PRINT '⚠️  IMPORTANT: Change default passwords before production!';
PRINT '';
GO

