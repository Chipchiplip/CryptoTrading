-- =============================================
-- Crypto Trading Web - Seed Data
-- MySQL 8.0+
-- Version: 1.0.0
-- =============================================

USE crypto_trading;

-- =============================================
-- 1) Seed Cryptocurrencies (Top 20)
-- =============================================

SELECT 'Seeding cryptocurrencies...' as message;

INSERT IGNORE INTO market_cryptocurrencies (Id, CoinGeckoId, Symbol, Name, IconUrl, MarketCapRank, IsActive)
VALUES
  (1, 'bitcoin',   'BTC',  'Bitcoin',          'https://assets.coingecko.com/coins/images/1/large/bitcoin.png', 1, TRUE),
  (2, 'ethereum',  'ETH',  'Ethereum',         'https://assets.coingecko.com/coins/images/279/large/ethereum.png', 2, TRUE),
  (3, 'tether',    'USDT', 'Tether',           'https://assets.coingecko.com/coins/images/325/large/Tether.png', 3, TRUE),
  (4, 'binancecoin','BNB', 'BNB',              'https://assets.coingecko.com/coins/images/825/large/bnb-icon2_2x.png', 4, TRUE),
  (5, 'solana',    'SOL',  'Solana',           'https://assets.coingecko.com/coins/images/4128/large/solana.png', 5, TRUE),
  (6, 'ripple',    'XRP',  'XRP',              'https://assets.coingecko.com/coins/images/44/large/xrp-symbol-white-128.png', 6, TRUE),
  (7, 'usd-coin',  'USDC', 'USD Coin',         'https://assets.coingecko.com/coins/images/6319/large/USD_Coin_icon.png', 7, TRUE),
  (8, 'cardano',   'ADA',  'Cardano',          'https://assets.coingecko.com/coins/images/975/large/cardano.png', 8, TRUE),
  (9, 'dogecoin',  'DOGE', 'Dogecoin',         'https://assets.coingecko.com/coins/images/5/large/dogecoin.png', 9, TRUE),
  (10,'tron',      'TRX',  'TRON',             'https://assets.coingecko.com/coins/images/1094/large/tron-logo.png', 10, TRUE),
  (11,'avalanche-2','AVAX','Avalanche',        'https://assets.coingecko.com/coins/images/12559/large/coin-round-red.png', 11, TRUE),
  (12,'wrapped-bitcoin','WBTC','Wrapped Bitcoin','https://assets.coingecko.com/coins/images/7598/large/wrapped_bitcoin_wbtc.png', 12, TRUE),
  (13,'chainlink', 'LINK', 'Chainlink',        'https://assets.coingecko.com/coins/images/877/large/chainlink-new-logo.png', 13, TRUE),
  (14,'polkadot',  'DOT',  'Polkadot',         'https://assets.coingecko.com/coins/images/12171/large/polkadot.png', 14, TRUE),
  (15,'polygon-ecosystem-token','POL','Polygon','https://assets.coingecko.com/coins/images/4713/large/matic-token-icon.png', 15, TRUE),
  (16,'bitcoin-cash','BCH','Bitcoin Cash',     'https://assets.coingecko.com/coins/images/780/large/bitcoin-cash-circle.png', 16, TRUE),
  (17,'dai',       'DAI',  'Dai',              'https://assets.coingecko.com/coins/images/9956/large/4943.png', 17, TRUE),
  (18,'litecoin',  'LTC',  'Litecoin',         'https://assets.coingecko.com/coins/images/2/large/litecoin.png', 18, TRUE),
  (19,'uniswap',   'UNI',  'Uniswap',          'https://assets.coingecko.com/coins/images/12504/large/uniswap-logo.png', 19, TRUE),
  (20,'stellar',   'XLM',  'Stellar',          'https://assets.coingecko.com/coins/images/100/large/Stellar_symbol_black_RGB.png', 20, TRUE);

SELECT '✓ Seeded 20 cryptocurrencies' as message;

-- =============================================
-- 2) Seed Demo Users (for testing)
-- =============================================

SELECT 'Seeding demo users...' as message;

-- Password: Admin@123 (you should hash properly in real app)
-- Using SHA2 for password hashing with fixed salt
SET @salt_hex = '01234567890123456789012345678901';

INSERT IGNORE INTO auth_users (Id, Email, PasswordHash, PasswordSalt, FirstName, LastName, IsEmailVerified, TwoFactorEnabled, SubscriptionTier, IsActive)
VALUES 
  (UUID(), 'admin@cryptotrading.dev', 
   UNHEX(SHA2(CONCAT('Admin@123', @salt_hex), 256)), 
   UNHEX(@salt_hex), 
   'Admin', 'User', TRUE, FALSE, 2, TRUE),  -- Pro tier
  
  (UUID(), 'nhat.an@cryptotrading.dev', 
   UNHEX(SHA2(CONCAT('Admin@123', @salt_hex), 256)), 
   UNHEX(@salt_hex), 
   'Nhật', 'An', TRUE, FALSE, 1, TRUE),   -- Plus tier
   
  (UUID(), 'huu.triet@cryptotrading.dev', 
   UNHEX(SHA2(CONCAT('Admin@123', @salt_hex), 256)), 
   UNHEX(@salt_hex), 
   'Hữu', 'Triết', TRUE, FALSE, 1, TRUE),
   
  (UUID(), 'trung.hieu@cryptotrading.dev', 
   UNHEX(SHA2(CONCAT('Admin@123', @salt_hex), 256)), 
   UNHEX(@salt_hex), 
   'Trung', 'Hiếu', TRUE, FALSE, 1, TRUE),
   
  (UUID(), 'vu.hoang@cryptotrading.dev', 
   UNHEX(SHA2(CONCAT('Admin@123', @salt_hex), 256)), 
   UNHEX(@salt_hex), 
   'Vũ', 'Hoàng', TRUE, FALSE, 2, TRUE),
   
  (UUID(), 'dang.khoa@cryptotrading.dev', 
   UNHEX(SHA2(CONCAT('Admin@123', @salt_hex), 256)), 
   UNHEX(@salt_hex), 
   'Đăng', 'Khoa', TRUE, FALSE, 1, TRUE);

SELECT '✓ Seeded 6 demo users (default password: Admin@123)' as message;

-- =============================================
-- 3) Seed Initial Market Stats
-- =============================================

SELECT 'Seeding initial market stats...' as message;

INSERT IGNORE INTO market_market_stats (Id, TotalMarketCap, TotalVolume24h, MarketCapChangePercentage24h, ActiveCryptocurrencies, FearGreedIndex, FearGreedClassification)
VALUES 
  (UUID(), 2500000000000.00, 85000000000.00, 2.45, 20, 65, 'Greed');

SELECT '✓ Seeded initial market stats' as message;

-- =============================================
-- 4) Seed Initial Crypto Prices (current snapshot)
-- =============================================

SELECT 'Seeding initial crypto prices...' as message;

INSERT IGNORE INTO market_crypto_prices (CryptocurrencyId, PriceUsd, MarketCap, Volume24h, PercentChange1h, PercentChange24h, PercentChange7d, CirculatingSupply, TotalSupply)
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
FROM market_cryptocurrencies
WHERE IsActive = TRUE;

SELECT CONCAT('✓ Seeded ', ROW_COUNT(), ' initial crypto prices') as message;

-- =============================================
-- 5) Summary
-- =============================================

SELECT '' as message;
SELECT '================================================' as message;
SELECT 'Seed data completed successfully!' as message;
SELECT '================================================' as message;
SELECT '' as message;
SELECT 'Database Statistics:' as message;
SELECT CONCAT('  • Cryptocurrencies: ', (SELECT COUNT(*) FROM market_cryptocurrencies)) as message;
SELECT CONCAT('  • Users: ', (SELECT COUNT(*) FROM auth_users)) as message;
SELECT CONCAT('  • Crypto Prices: ', (SELECT COUNT(*) FROM market_crypto_prices)) as message;
SELECT CONCAT('  • Market Stats: ', (SELECT COUNT(*) FROM market_market_stats)) as message;
SELECT '' as message;
SELECT 'Demo Users (Password: Admin@123):' as message;
SELECT '  • admin@cryptotrading.dev (Pro)' as message;
SELECT '  • nhat.an@cryptotrading.dev (Plus)' as message;
SELECT '  • huu.triet@cryptotrading.dev (Plus)' as message;
SELECT '  • trung.hieu@cryptotrading.dev (Plus)' as message;
SELECT '  • vu.hoang@cryptotrading.dev (Pro)' as message;
SELECT '  • dang.khoa@cryptotrading.dev (Plus)' as message;
SELECT '' as message;
SELECT '⚠️  IMPORTANT: Change default passwords before production!' as message;
SELECT '' as message;
