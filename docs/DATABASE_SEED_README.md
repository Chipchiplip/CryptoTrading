# Dashboard Seed Data

File `seed_dashboard.sql` chứa dữ liệu mẫu cho User ID=8.

## Nội dung seed:

1. **Wallets**: Tạo ví USD (FIAT), BTC (COIN), ETH (COIN)
2. **WalletMovements**: 
   - DEPOSIT 10,000 USD
   - ADJUSTMENT 0.5 BTC
   - ADJUSTMENT 10 ETH
3. **CryptoPrices**: Giá mới nhất cho BTC (65,000) và ETH (3,200)
4. **Orders**:
   - 1 BUY BTC (LIMIT, NEW) - 0.1 BTC @ 64,000 USD
   - 1 SELL ETH (LIMIT, PARTIAL) - 5 ETH @ 3,250 USD (đã fill 2.5)
5. **OrderHolds**: 
   - Hold 6,400 USD cho BUY BTC
   - Hold 2.5 ETH cho SELL ETH
6. **Trades**: 1 trade fill cho SELL ETH (2.5 ETH @ 3,250)

## Cách sử dụng:

```sql
-- Chạy file trong MySQL Workbench hoặc MySQL CLI
mysql -u root -p crypto_trading < database/seed_dashboard.sql

-- Hoặc copy nội dung file và chạy trong MySQL Workbench
```

## Kiểm tra dữ liệu:

Sau khi seed, chạy các query cuối file để verify:
- Wallet balances
- Open orders count
- Portfolio NAV
- Today's PnL

## Thay đổi User ID:

Mở file và thay đổi dòng:
```sql
SET @userId = 8;  -- Đổi số này thành User ID khác
```

