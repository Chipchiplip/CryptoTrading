-- =====================================================================
-- Seed Dashboard Data for User ID=8
-- =====================================================================
USE crypto_trading;

SET @userId = 8;
SET @usdAmount = 10000.00;
SET @btcAmount = 0.5;
SET @ethAmount = 10.0;

-- =====================================================================
-- 1. Create Wallets (FIAT USD + COIN BTC/ETH)
-- =====================================================================

-- USD Wallet (FIAT)
INSERT INTO Wallets (UserId, AssetType, CurrencyCode, CryptocurrencyId)
SELECT @userId, 'FIAT', 'USD', NULL
WHERE NOT EXISTS (
    SELECT 1 FROM Wallets 
    WHERE UserId = @userId AND AssetType = 'FIAT' AND CurrencyCode = 'USD'
);

-- BTC Wallet (COIN)
INSERT INTO Wallets (UserId, AssetType, CurrencyCode, CryptocurrencyId)
SELECT @userId, 'COIN', NULL, c.Id
FROM Cryptocurrencies c
WHERE c.Symbol = 'BTC'
  AND NOT EXISTS (
      SELECT 1 FROM Wallets 
      WHERE UserId = @userId AND AssetType = 'COIN' AND CryptocurrencyId = c.Id
  );

-- ETH Wallet (COIN)
INSERT INTO Wallets (UserId, AssetType, CurrencyCode, CryptocurrencyId)
SELECT @userId, 'COIN', NULL, c.Id
FROM Cryptocurrencies c
WHERE c.Symbol = 'ETH'
  AND NOT EXISTS (
      SELECT 1 FROM Wallets 
      WHERE UserId = @userId AND AssetType = 'COIN' AND CryptocurrencyId = c.Id
  );

-- =====================================================================
-- 2. Insert Wallet Movements (DEPOSIT for USD, ADJUSTMENT for BTC/ETH)
-- =====================================================================

-- Get Wallet IDs into variables (for subsequent use)
SET @usdWalletId = (SELECT Id FROM Wallets WHERE UserId = @userId AND AssetType = 'FIAT' AND CurrencyCode = 'USD' LIMIT 1);
SET @btcWalletId = (SELECT Id FROM Wallets WHERE UserId = @userId AND AssetType = 'COIN' AND CryptocurrencyId = (SELECT Id FROM Cryptocurrencies WHERE Symbol = 'BTC' LIMIT 1) LIMIT 1);
SET @ethWalletId = (SELECT Id FROM Wallets WHERE UserId = @userId AND AssetType = 'COIN' AND CryptocurrencyId = (SELECT Id FROM Cryptocurrencies WHERE Symbol = 'ETH' LIMIT 1) LIMIT 1);

-- USD Deposit
INSERT INTO WalletMovements (WalletId, RefType, RefId, Amount, Note, CreatedAt)
SELECT @usdWalletId, 'DEPOSIT', NULL, @usdAmount, 'Initial deposit', NOW()
WHERE @usdWalletId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM WalletMovements 
      WHERE WalletId = @usdWalletId AND RefType = 'DEPOSIT' AND ABS(Amount - @usdAmount) < 0.01
  );

-- BTC Adjustment
INSERT INTO WalletMovements (WalletId, RefType, RefId, Amount, Note, CreatedAt)
SELECT @btcWalletId, 'ADJUSTMENT', NULL, @btcAmount, 'Initial balance', NOW()
WHERE @btcWalletId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM WalletMovements 
      WHERE WalletId = @btcWalletId AND RefType = 'ADJUSTMENT' AND ABS(Amount - @btcAmount) < 0.00000001
  );

-- ETH Adjustment
INSERT INTO WalletMovements (WalletId, RefType, RefId, Amount, Note, CreatedAt)
SELECT @ethWalletId, 'ADJUSTMENT', NULL, @ethAmount, 'Initial balance', NOW()
WHERE @ethWalletId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM WalletMovements 
      WHERE WalletId = @ethWalletId AND RefType = 'ADJUSTMENT' AND ABS(Amount - @ethAmount) < 0.00000001
  );

-- =====================================================================
-- 3. Insert Latest Crypto Prices for BTC/ETH
-- =====================================================================

-- BTC Price
INSERT INTO CryptoPrices (CryptocurrencyId, PriceUsd, MarketCap, Volume24h, PercentChange1h, PercentChange24h, PercentChange7d, CirculatingSupply, TotalSupply, CollectedAtUtc)
SELECT 
    c.Id,
    65000.00 AS PriceUsd,
    1280000000000 AS MarketCap,
    25000000000 AS Volume24h,
    0.5 AS PercentChange1h,
    2.5 AS PercentChange24h,
    -1.2 AS PercentChange7d,
    19680000 AS CirculatingSupply,
    21000000 AS TotalSupply,
    NOW() AS CollectedAtUtc
FROM Cryptocurrencies c
WHERE c.Symbol = 'BTC'
  AND NOT EXISTS (
      SELECT 1 FROM CryptoPrices 
      WHERE CryptocurrencyId = c.Id 
        AND DATE(CollectedAtUtc) = CURRENT_DATE
        AND ABS(PriceUsd - 65000) < 1
  )
LIMIT 1;

-- ETH Price
INSERT INTO CryptoPrices (CryptocurrencyId, PriceUsd, MarketCap, Volume24h, PercentChange1h, PercentChange24h, PercentChange7d, CirculatingSupply, TotalSupply, CollectedAtUtc)
SELECT 
    c.Id,
    3200.00 AS PriceUsd,
    385000000000 AS MarketCap,
    12000000000 AS Volume24h,
    0.3 AS PercentChange1h,
    1.8 AS PercentChange24h,
    -0.5 AS PercentChange7d,
    120300000 AS CirculatingSupply,
    NULL AS TotalSupply,
    NOW() AS CollectedAtUtc
FROM Cryptocurrencies c
WHERE c.Symbol = 'ETH'
  AND NOT EXISTS (
      SELECT 1 FROM CryptoPrices 
      WHERE CryptocurrencyId = c.Id 
        AND DATE(CollectedAtUtc) = CURRENT_DATE
        AND ABS(PriceUsd - 3200) < 1
  )
LIMIT 1;

-- =====================================================================
-- 4. Create Orders (1 BUY BTC NEW, 1 SELL ETH PARTIAL)
-- =====================================================================

SET @btcCryptoId = (SELECT Id FROM Cryptocurrencies WHERE Symbol = 'BTC' LIMIT 1);
SET @ethCryptoId = (SELECT Id FROM Cryptocurrencies WHERE Symbol = 'ETH' LIMIT 1);

-- Order 1: BUY BTC (LIMIT, NEW status)
INSERT INTO Orders (UserId, CryptocurrencyId, Side, Type, Status, PriceUsd, QuantityCoin, FilledQty, CreatedAt, UpdatedAt)
SELECT 
    @userId,
    @btcCryptoId,
    'BUY',
    'LIMIT',
    'NEW',
    64000.00 AS PriceUsd,
    0.1 AS QuantityCoin,
    0 AS FilledQty,
    NOW(),
    NULL
WHERE @btcCryptoId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM Orders 
      WHERE UserId = @userId 
        AND CryptocurrencyId = @btcCryptoId
        AND Side = 'BUY'
        AND Status = 'NEW'
        AND ABS(PriceUsd - 64000) < 1
        AND ABS(QuantityCoin - 0.1) < 0.0001
  )
LIMIT 1;

SET @buyOrderId = LAST_INSERT_ID();

-- Order 2: SELL ETH (LIMIT, PARTIAL status)
INSERT INTO Orders (UserId, CryptocurrencyId, Side, Type, Status, PriceUsd, QuantityCoin, FilledQty, CreatedAt, UpdatedAt)
SELECT 
    @userId,
    @ethCryptoId,
    'SELL',
    'LIMIT',
    'PARTIAL',
    3250.00 AS PriceUsd,
    5.0 AS QuantityCoin,
    2.5 AS FilledQty,
    NOW(),
    NOW()
WHERE @ethCryptoId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM Orders 
      WHERE UserId = @userId 
        AND CryptocurrencyId = @ethCryptoId
        AND Side = 'SELL'
        AND Status = 'PARTIAL'
        AND ABS(PriceUsd - 3250) < 1
        AND ABS(QuantityCoin - 5.0) < 0.01
  )
LIMIT 1;

SET @sellOrderId = LAST_INSERT_ID();

-- =====================================================================
-- 5. Create OrderHolds (USD hold for BUY, ETH hold for SELL)
-- =====================================================================

-- Hold USD for BUY BTC order (6400 USD = 0.1 * 64000)
INSERT INTO OrderHolds (OrderId, WalletId, Amount, CreatedAt, ReleasedAt)
SELECT 
    @buyOrderId,
    @usdWalletId,
    6400.00 AS Amount,
    NOW(),
    NULL
WHERE @buyOrderId > 0 AND @usdWalletId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM OrderHolds 
      WHERE OrderId = @buyOrderId AND ReleasedAt IS NULL
  )
LIMIT 1;

-- Hold ETH for SELL ETH order (2.5 ETH remaining = 5.0 - 2.5)
SET @ethRemaining = 5.0 - 2.5;
INSERT INTO OrderHolds (OrderId, WalletId, Amount, CreatedAt, ReleasedAt)
SELECT 
    @sellOrderId,
    @ethWalletId,
    @ethRemaining AS Amount,
    NOW(),
    NULL
WHERE @sellOrderId > 0 AND @ethWalletId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM OrderHolds 
      WHERE OrderId = @sellOrderId AND ReleasedAt IS NULL
  )
LIMIT 1;

-- Create corresponding WalletMovements for holds (negative amounts)
-- USD hold (debit)
INSERT INTO WalletMovements (WalletId, RefType, RefId, Amount, Note, CreatedAt)
SELECT 
    @usdWalletId,
    'ORDER_HOLD',
    @buyOrderId,
    -6400.00 AS Amount,
    'Hold for BUY BTC order',
    NOW()
WHERE @buyOrderId > 0 AND @usdWalletId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM WalletMovements 
      WHERE WalletId = @usdWalletId 
        AND RefType = 'ORDER_HOLD' 
        AND RefId = @buyOrderId
  )
LIMIT 1;

-- ETH hold (debit)
INSERT INTO WalletMovements (WalletId, RefType, RefId, Amount, Note, CreatedAt)
SELECT 
    @ethWalletId,
    'ORDER_HOLD',
    @sellOrderId,
    -@ethRemaining AS Amount,
    'Hold for SELL ETH order',
    NOW()
WHERE @sellOrderId > 0 AND @ethWalletId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM WalletMovements 
      WHERE WalletId = @ethWalletId 
        AND RefType = 'ORDER_HOLD' 
        AND RefId = @sellOrderId
  )
LIMIT 1;

-- =====================================================================
-- 6. Create Trade (small fill for SELL ETH order for Today PnL)
-- =====================================================================

-- Trade for SELL ETH order (2.5 ETH sold at 3250 USD)
INSERT INTO Trades (OrderId, CryptocurrencyId, PriceUsd, QuantityCoin, FeeUsd, CreatedAt)
SELECT 
    @sellOrderId,
    @ethCryptoId,
    3250.00 AS PriceUsd,
    2.5 AS QuantityCoin,
    20.31 AS FeeUsd,  -- 0.25% fee = 8125 * 0.0025 ≈ 20.31
    NOW()
WHERE @sellOrderId > 0 AND @ethCryptoId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM Trades 
      WHERE OrderId = @sellOrderId 
        AND DATE(CreatedAt) = CURRENT_DATE
        AND ABS(QuantityCoin - 2.5) < 0.01
  )
LIMIT 1;

SET @tradeId = LAST_INSERT_ID();

-- Create WalletMovements for the trade
-- Credit USD (received from SELL)
INSERT INTO WalletMovements (WalletId, RefType, RefId, Amount, Note, CreatedAt)
SELECT 
    @usdWalletId,
    'TRADE_FILL',
    @tradeId,
    8125.00 - 20.31 AS Amount,  -- 2.5 * 3250 - fee
    'Sell ETH trade fill',
    NOW()
WHERE @tradeId > 0 AND @usdWalletId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM WalletMovements 
      WHERE WalletId = @usdWalletId 
        AND RefType = 'TRADE_FILL' 
        AND RefId = @tradeId
  )
LIMIT 1;

-- Debit ETH (sold)
INSERT INTO WalletMovements (WalletId, RefType, RefId, Amount, Note, CreatedAt)
SELECT 
    @ethWalletId,
    'TRADE_FILL',
    @tradeId,
    -2.5 AS Amount,
    'Sell ETH trade fill',
    NOW()
WHERE @tradeId > 0 AND @ethWalletId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM WalletMovements 
      WHERE WalletId = @ethWalletId 
        AND RefType = 'TRADE_FILL' 
        AND RefId = @tradeId
  )
LIMIT 1;

-- =====================================================================
-- 7. Verification Queries
-- =====================================================================

-- Check: Available balances (using V_WalletBalances view if exists, otherwise raw query)
SELECT 
    w.Id AS WalletId,
    w.UserId,
    w.AssetType,
    w.CurrencyCode,
    w.CryptocurrencyId,
    COALESCE(SUM(m.Amount), 0) AS Balance
FROM Wallets w
LEFT JOIN WalletMovements m ON m.WalletId = w.Id
WHERE w.UserId = @userId
GROUP BY w.Id, w.UserId, w.AssetType, w.CurrencyCode, w.CryptocurrencyId;

-- Check: Open orders count
SELECT COUNT(*) AS OpenOrders
FROM Orders
WHERE UserId = @userId
  AND Status IN ('NEW', 'PARTIAL');

-- Check: NAV (Portfolio Net Asset Value)
SELECT 
    p.UserId,
    SUM(p.QtyCoin * COALESCE(cp.PriceUsd, 0)) AS PortfolioUsd
FROM (
    SELECT 
        o.UserId,
        t.CryptocurrencyId,
        ROUND(SUM(CASE 
            WHEN o.Side = 'BUY' THEN t.QuantityCoin
            WHEN o.Side = 'SELL' THEN -t.QuantityCoin
            ELSE 0 
        END), 18) AS QtyCoin
    FROM Trades t
    JOIN Orders o ON o.Id = t.OrderId
    WHERE o.UserId = @userId
    GROUP BY o.UserId, t.CryptocurrencyId
    HAVING QtyCoin <> 0
) p
LEFT JOIN (
    SELECT CryptocurrencyId, PriceUsd
    FROM CryptoPrices
    WHERE (CryptocurrencyId, CollectedAtUtc) IN (
        SELECT CryptocurrencyId, MAX(CollectedAtUtc) AS MaxTs
        FROM CryptoPrices
        GROUP BY CryptocurrencyId
    )
) cp ON cp.CryptocurrencyId = p.CryptocurrencyId
GROUP BY p.UserId;

-- Check: Today's PnL
SELECT 
    DATE(t.CreatedAt) AS d,
    SUM(CASE 
        WHEN o.Side = 'SELL' THEN (t.PriceUsd * t.QuantityCoin - t.FeeUsd)
        ELSE -(t.PriceUsd * t.QuantityCoin + t.FeeUsd)
    END) AS TodayPnl
FROM Trades t
JOIN Orders o ON o.Id = t.OrderId
WHERE o.UserId = @userId
  AND DATE(t.CreatedAt) = CURRENT_DATE()
GROUP BY DATE(t.CreatedAt);

