-- Script để fix locked balance bị stuck (release OrderHolds của các Order đã FILLED/CANCELED/REJECTED)
-- ⚠️ CHẠY CẨN THẬN: Script này sẽ release tất cả OrderHolds của các Order đã FILLED/CANCELED/REJECTED
-- Thay @userId = user ID của bạn (theo hình ảnh là 2)

SET @userId = 2; -- ⚠️ THAY ĐỔI USER ID CỦA BẠN Ở ĐÂY

-- =====================================================================
-- BƯỚC 1: XEM CÁC OrderHolds SẼ ĐƯỢC RELEASE (KIỂM TRA TRƯỚC)
-- =====================================================================
SELECT 
    '🔍 OrderHolds sẽ được RELEASE' AS Action,
    oh.Id AS OrderHoldId,
    oh.OrderId,
    oh.Amount AS LockedAmount,
    oh.CreatedAt AS LockedAt,
    o.Status AS OrderStatus,
    o.Side,
    o.Type,
    o.QuantityCoin,
    o.FilledQty,
    o.PriceUsd,
    c.Symbol,
    CASE 
        WHEN o.Status = 'FILLED' THEN '✅ Order đã FILLED - sẽ release'
        WHEN o.Status = 'CANCELED' THEN '✅ Order đã CANCELED - sẽ release'
        WHEN o.Status = 'REJECTED' THEN '✅ Order đã REJECTED - sẽ release'
        ELSE '⚠️ Status khác'
    END AS Reason
FROM Wallets w
JOIN OrderHolds oh ON oh.WalletId = w.Id
JOIN Orders o ON o.Id = oh.OrderId
LEFT JOIN Cryptocurrencies c ON c.Id = o.CryptocurrencyId
WHERE w.UserId = @userId 
  AND w.AssetType = 'FIAT' 
  AND w.CurrencyCode = 'USD'
  AND oh.ReleasedAt IS NULL
  AND o.Status IN ('FILLED', 'CANCELED', 'REJECTED')
ORDER BY oh.CreatedAt DESC;

-- =====================================================================
-- BƯỚC 2: TỔNG HỢP SỐ TIỀN SẼ ĐƯỢC RELEASE
-- =====================================================================
SELECT 
    '📊 Tổng hợp số tiền sẽ được RELEASE' AS SummaryType,
    COUNT(*) AS NumberOfHolds,
    SUM(oh.Amount) AS TotalLockedAmount,
    MIN(oh.CreatedAt) AS OldestHold,
    MAX(oh.CreatedAt) AS NewestHold,
    GROUP_CONCAT(DISTINCT o.Status) AS OrderStatuses
FROM Wallets w
JOIN OrderHolds oh ON oh.WalletId = w.Id
JOIN Orders o ON o.Id = oh.OrderId
WHERE w.UserId = @userId 
  AND w.AssetType = 'FIAT' 
  AND w.CurrencyCode = 'USD'
  AND oh.ReleasedAt IS NULL
  AND o.Status IN ('FILLED', 'CANCELED', 'REJECTED');

-- =====================================================================
-- BƯỚC 3: KIỂM TRA BALANCE TRƯỚC KHI RELEASE
-- =====================================================================
SELECT 
    '💰 Balance TRƯỚC khi release' AS CheckType,
    COALESCE(SUM(wm.Amount), 0) AS TotalBalance,
    (SELECT COALESCE(SUM(oh2.Amount), 0) 
     FROM Wallets w2
     JOIN OrderHolds oh2 ON oh2.WalletId = w2.Id
     WHERE w2.UserId = @userId 
       AND w2.AssetType = 'FIAT' 
       AND w2.CurrencyCode = 'USD'
       AND oh2.ReleasedAt IS NULL) AS LockedBalance,
    COALESCE(SUM(wm.Amount), 0) - 
    (SELECT COALESCE(SUM(oh2.Amount), 0) 
     FROM Wallets w2
     JOIN OrderHolds oh2 ON oh2.WalletId = w2.Id
     WHERE w2.UserId = @userId 
       AND w2.AssetType = 'FIAT' 
       AND w2.CurrencyCode = 'USD'
       AND oh2.ReleasedAt IS NULL) AS AvailableBalance
FROM Wallets w
JOIN WalletMovements wm ON wm.WalletId = w.Id
WHERE w.UserId = @userId 
  AND w.AssetType = 'FIAT' 
  AND w.CurrencyCode = 'USD';

-- =====================================================================
-- BƯỚC 4: RELEASE CÁC OrderHolds (CHẠY SAU KHI XÁC NHẬN Ở CÁC BƯỚC TRƯỚC)
-- =====================================================================
-- ⚠️ QUAN TRỌNG: Chạy các query BƯỚC 1, 2, 3 trước để xác nhận dữ liệu
-- Sau khi xác nhận đúng, chạy UPDATE này:

UPDATE OrderHolds oh
JOIN Wallets w ON w.Id = oh.WalletId
JOIN Orders o ON o.Id = oh.OrderId
SET oh.ReleasedAt = NOW()
WHERE w.UserId = @userId 
  AND w.AssetType = 'FIAT' 
  AND w.CurrencyCode = 'USD'
  AND oh.ReleasedAt IS NULL
  AND o.Status IN ('FILLED', 'CANCELED', 'REJECTED');

-- =====================================================================
-- BƯỚC 5: KIỂM TRA BALANCE SAU KHI RELEASE
-- =====================================================================
-- Chạy query này SAU KHI chạy UPDATE ở bước 4 để xác nhận kết quả:

SELECT 
    '💰 Balance SAU khi release' AS CheckType,
    COALESCE(SUM(wm.Amount), 0) AS TotalBalance,
    (SELECT COALESCE(SUM(oh2.Amount), 0) 
     FROM Wallets w2
     JOIN OrderHolds oh2 ON oh2.WalletId = w2.Id
     WHERE w2.UserId = @userId 
       AND w2.AssetType = 'FIAT' 
       AND w2.CurrencyCode = 'USD'
       AND oh2.ReleasedAt IS NULL) AS LockedBalance,
    COALESCE(SUM(wm.Amount), 0) - 
    (SELECT COALESCE(SUM(oh2.Amount), 0) 
     FROM Wallets w2
     JOIN OrderHolds oh2 ON oh2.WalletId = w2.Id
     WHERE w2.UserId = @userId 
       AND w2.AssetType = 'FIAT' 
       AND w2.CurrencyCode = 'USD'
       AND oh2.ReleasedAt IS NULL) AS AvailableBalance
FROM Wallets w
JOIN WalletMovements wm ON wm.WalletId = w.Id
WHERE w.UserId = @userId 
  AND w.AssetType = 'FIAT' 
  AND w.CurrencyCode = 'USD';

-- =====================================================================
-- BƯỚC 6: KIỂM TRA CÁC OrderHolds CÒN LẠI (NẾU CÓ)
-- =====================================================================
-- Chạy query này để xem các OrderHolds còn active (nên chỉ còn NEW/PARTIAL orders):

SELECT 
    '🔍 OrderHolds còn active (nên là NEW/PARTIAL orders)' AS CheckType,
    oh.Id AS OrderHoldId,
    oh.OrderId,
    oh.Amount AS LockedAmount,
    oh.CreatedAt AS LockedAt,
    o.Status AS OrderStatus,
    o.Side,
    o.Type,
    o.QuantityCoin,
    o.FilledQty,
    c.Symbol,
    CASE 
        WHEN o.Status IN ('NEW', 'PARTIAL') THEN '✅ OK: Order đang chờ'
        WHEN o.Status IS NULL THEN '❌ WARNING: Order không tồn tại (orphaned)'
        ELSE CONCAT('⚠️ WARNING: Order status không mong đợi: ', o.Status)
    END AS StatusCheck
FROM Wallets w
JOIN OrderHolds oh ON oh.WalletId = w.Id
LEFT JOIN Orders o ON o.Id = oh.OrderId
LEFT JOIN Cryptocurrencies c ON c.Id = o.CryptocurrencyId
WHERE w.UserId = @userId 
  AND w.AssetType = 'FIAT' 
  AND w.CurrencyCode = 'USD'
  AND oh.ReleasedAt IS NULL
ORDER BY oh.CreatedAt DESC;

