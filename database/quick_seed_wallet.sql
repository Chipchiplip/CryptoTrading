-- =====================================================================
-- Quick Seed Wallet Script - Cho Bất Kỳ User Nào
-- =====================================================================
-- Script này sẽ tự động tìm user và tạo wallet + deposit USD
-- 
-- Cách dùng:
-- 1. Thay YOUR_EMAIL_HERE bằng email của bạn
-- 2. Hoặc set @userId trực tiếp
-- 3. Chạy trong MySQL Workbench hoặc CLI
-- =====================================================================

USE crypto_trading;

-- =====================================================================
-- CÁCH 1: Tìm User theo Email (Khuyến nghị)
-- =====================================================================
SET @userEmail = 'your-email@example.com';  -- ⚠️ THAY ĐỔI EMAIL Ở ĐÂY
SET @userId = (SELECT Id FROM Users WHERE Email = @userEmail LIMIT 1);

-- =====================================================================
-- CÁCH 2: Set User ID trực tiếp (nếu biết ID)
-- =====================================================================
-- SET @userId = 8;  -- ⚠️ Hoặc set trực tiếp User ID

-- =====================================================================
-- Kiểm tra User ID
-- =====================================================================
SELECT 
    CASE 
        WHEN @userId IS NULL THEN '❌ ERROR: Không tìm thấy user! Kiểm tra lại email hoặc User ID.'
        ELSE CONCAT('✅ User ID: ', @userId)
    END AS Status;

-- Nếu @userId NULL, dừng script
-- SELECT * FROM Users;  -- Uncomment để xem danh sách users

-- =====================================================================
-- 1. Tạo USD Wallet (nếu chưa có)
-- =====================================================================
INSERT INTO Wallets (UserId, AssetType, CurrencyCode, CryptocurrencyId)
SELECT @userId, 'FIAT', 'USD', NULL
WHERE @userId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM Wallets 
      WHERE UserId = @userId AND AssetType = 'FIAT' AND CurrencyCode = 'USD'
  );

SET @usdWalletId = (SELECT Id FROM Wallets WHERE UserId = @userId AND AssetType = 'FIAT' AND CurrencyCode = 'USD' LIMIT 1);

-- =====================================================================
-- 2. Deposit USD vào Wallet
-- =====================================================================
SET @depositAmount = 10000.00;  -- Số tiền muốn deposit (có thể thay đổi)

INSERT INTO WalletMovements (WalletId, RefType, RefId, Amount, Note, CreatedAt)
SELECT 
    @usdWalletId, 
    'DEPOSIT', 
    NULL, 
    @depositAmount, 
    CONCAT('Test deposit - ', NOW()), 
    NOW()
WHERE @usdWalletId IS NOT NULL
  AND NOT EXISTS (
      -- Tránh duplicate nếu đã có deposit gần đây
      SELECT 1 FROM WalletMovements 
      WHERE WalletId = @usdWalletId 
        AND RefType = 'DEPOSIT' 
        AND ABS(Amount - @depositAmount) < 0.01
        AND CreatedAt > DATE_SUB(NOW(), INTERVAL 1 HOUR)
  )
LIMIT 1;

-- =====================================================================
-- 3. Verify Wallet Balance
-- =====================================================================
SELECT 
    w.Id AS WalletId,
    u.Email,
    w.AssetType,
    w.CurrencyCode,
    COALESCE(SUM(wm.Amount), 0) AS Balance,
    CASE 
        WHEN COALESCE(SUM(wm.Amount), 0) > 0 THEN '✅ Success'
        ELSE '⚠️ Warning: Balance = 0'
    END AS Status
FROM Wallets w
INNER JOIN Users u ON u.Id = w.UserId
LEFT JOIN WalletMovements wm ON w.Id = wm.WalletId
WHERE w.UserId = @userId 
  AND w.AssetType = 'FIAT' 
  AND w.CurrencyCode = 'USD'
GROUP BY w.Id, u.Email, w.AssetType, w.CurrencyCode;

-- =====================================================================
-- KẾT QUẢ
-- =====================================================================
SELECT 
    CONCAT('✅ Setup hoàn tất! User ID: ', @userId) AS Message,
    CONCAT('💰 Balance: $', FORMAT(COALESCE((
        SELECT SUM(wm.Amount) 
        FROM Wallets w
        INNER JOIN WalletMovements wm ON w.Id = wm.WalletId
        WHERE w.UserId = @userId 
          AND w.AssetType = 'FIAT' 
          AND w.CurrencyCode = 'USD'
    ), 0), 2)) AS Balance;

