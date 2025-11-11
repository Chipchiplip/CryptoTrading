-- Debug script to check wallet balance calculations
-- Replace @userId with your actual user ID

SET @userId = 8; -- Change this to your user ID

-- 1. Check USD Wallet Movements
SELECT 
    'USD Wallet Movements' AS Section,
    wm.Id,
    wm.RefType,
    wm.RefId,
    wm.Amount,
    wm.Note,
    wm.CreatedAt
FROM Wallets w
JOIN WalletMovements wm ON wm.WalletId = w.Id
WHERE w.UserId = @userId 
  AND w.AssetType = 'FIAT' 
  AND w.CurrencyCode = 'USD'
ORDER BY wm.CreatedAt DESC;

-- 2. Calculate USD Balance
SELECT 
    'USD Balance Summary' AS Section,
    COALESCE(SUM(wm.Amount), 0) AS TotalBalance,
    (SELECT COALESCE(SUM(oh.Amount), 0) 
     FROM Wallets w2
     JOIN OrderHolds oh ON oh.WalletId = w2.Id
     WHERE w2.UserId = @userId 
       AND w2.AssetType = 'FIAT' 
       AND w2.CurrencyCode = 'USD'
       AND oh.ReleasedAt IS NULL) AS LockedBalance,
    COALESCE(SUM(wm.Amount), 0) - 
    (SELECT COALESCE(SUM(oh.Amount), 0) 
     FROM Wallets w2
     JOIN OrderHolds oh ON oh.WalletId = w2.Id
     WHERE w2.UserId = @userId 
       AND w2.AssetType = 'FIAT' 
       AND w2.CurrencyCode = 'USD'
       AND oh.ReleasedAt IS NULL) AS AvailableBalance
FROM Wallets w
JOIN WalletMovements wm ON wm.WalletId = w.Id
WHERE w.UserId = @userId 
  AND w.AssetType = 'FIAT' 
  AND w.CurrencyCode = 'USD';

-- 3. Check Order Holds (active only)
SELECT 
    'Active Order Holds' AS Section,
    oh.Id,
    oh.OrderId,
    oh.Amount,
    oh.CreatedAt AS LockedAt,
    oh.ReleasedAt,
    o.Side,
    o.Type,
    o.Status,
    o.QuantityCoin,
    o.PriceUsd
FROM Wallets w
JOIN OrderHolds oh ON oh.WalletId = w.Id
LEFT JOIN Orders o ON o.Id = oh.OrderId
WHERE w.UserId = @userId 
  AND w.AssetType = 'FIAT' 
  AND w.CurrencyCode = 'USD'
  AND oh.ReleasedAt IS NULL
ORDER BY oh.CreatedAt DESC;

-- 4. Check Orders
SELECT 
    'Recent Orders' AS Section,
    o.Id,
    o.Side,
    o.Type,
    o.Status,
    o.QuantityCoin,
    o.FilledQty,
    o.PriceUsd,
    o.CreatedAt,
    c.Symbol
FROM Orders o
JOIN Cryptocurrencies c ON c.Id = o.CryptocurrencyId
WHERE o.UserId = @userId
ORDER BY o.CreatedAt DESC
LIMIT 10;

-- 5. Check Trades
SELECT 
    'Recent Trades' AS Section,
    t.Id,
    t.OrderId,
    o.Side,
    t.QuantityCoin,
    t.PriceUsd,
    t.FeeUsd,
    (t.QuantityCoin * t.PriceUsd + t.FeeUsd) AS TotalCost,
    t.CreatedAt,
    c.Symbol
FROM Trades t
JOIN Orders o ON o.Id = t.OrderId
JOIN Cryptocurrencies c ON c.Id = t.CryptocurrencyId
WHERE o.UserId = @userId
ORDER BY t.CreatedAt DESC
LIMIT 10;

-- 6. Check Crypto Holdings
SELECT 
    'Crypto Holdings' AS Section,
    c.Symbol,
    c.Name,
    COALESCE(SUM(wm.Amount), 0) AS Balance,
    (SELECT COALESCE(SUM(oh.Amount), 0) 
     FROM OrderHolds oh
     WHERE oh.WalletId = w.Id 
       AND oh.ReleasedAt IS NULL) AS Locked
FROM Wallets w
LEFT JOIN WalletMovements wm ON wm.WalletId = w.Id
JOIN Cryptocurrencies c ON c.Id = w.CryptocurrencyId
WHERE w.UserId = @userId 
  AND w.AssetType = 'COIN'
GROUP BY w.Id, c.Symbol, c.Name, w.Id
HAVING COALESCE(SUM(wm.Amount), 0) != 0;


