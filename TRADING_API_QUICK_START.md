# Trading API Quick Start Guide

## 🚀 Getting Started

### 1. Start the Application
```bash
cd D:\CODE\Intern\CryptoTrading
dotnet run
```

The API will be available at: `http://localhost:5000`

### 2. Access Swagger UI
Open your browser: `http://localhost:5000/swagger`

---

## 🔐 Authentication

All trading endpoints require JWT authentication.

### Get Token
```bash
# 1. Register a user
POST http://localhost:5000/api/auth/register
{
  "email": "trader@example.com",
  "password": "Test123!",
  "fullName": "Test Trader"
}

# 2. Login
POST http://localhost:5000/api/auth/login
{
  "email": "trader@example.com",
  "password": "Test123!"
}

# Response includes: { "token": "eyJ...", ... }
```

### Use Token in Requests
```bash
# Add to headers:
Authorization: Bearer eyJ...your-token-here...
```

---

## 💰 Add Balance (For Testing)

Before trading, you need to add funds to your wallet:

```sql
-- Connect to your MySQL database and run:

-- 1. Find your user ID
SELECT * FROM Users WHERE Email = 'trader@example.com';
-- Note the Id (e.g., 1)

-- 2. Create a USD wallet (if not exists)
INSERT INTO Wallets (UserId, AssetType, CurrencyCode, CryptocurrencyId)
VALUES (1, 'FIAT', 'USD', NULL);
-- Note the Id (e.g., 1)

-- 3. Add $10,000 USD
INSERT INTO WalletMovements (WalletId, RefType, RefId, Amount, Note, CreatedAt)
VALUES (1, 'DEPOSIT', NULL, 10000, 'Initial deposit for testing', NOW());

-- 4. Verify balance
SELECT w.Id, w.AssetType, w.CurrencyCode, SUM(wm.Amount) as Balance
FROM Wallets w
LEFT JOIN WalletMovements wm ON w.Id = wm.WalletId
WHERE w.UserId = 1
GROUP BY w.Id;
```

---

## 📝 API Endpoints

### 1. Place Market Order (Buy BTC)

**Endpoint**: `POST /api/trading/orders`

**Request**:
```json
{
  "symbol": "BTC/USDT",
  "side": "BUY",
  "type": "MARKET",
  "quantity": 0.01
}
```

**Response**:
```json
{
  "id": "1",
  "symbol": "BTC/USDT",
  "side": "BUY",
  "type": "MARKET",
  "quantity": 0.01,
  "price": null,
  "filled": 0.01,
  "remaining": 0,
  "status": "FILLED",
  "createdAt": "2025-11-03T10:30:00Z",
  "updatedAt": "2025-11-03T10:30:01Z"
}
```

**What Happens**:
1. Validates you have sufficient USD balance
2. Locks balance (quantity × current_price × 1.05 × 1.001)
3. Gets current BTC price from CoinGecko
4. Executes at market price
5. Creates trade record
6. Updates your wallets:
   - Debits USD (cost + 0.1% fee)
   - Credits BTC (quantity)
7. Releases OrderHold
8. Returns filled order

---

### 2. Place Limit Order (Sell BTC)

**Endpoint**: `POST /api/trading/orders`

**Request**:
```json
{
  "symbol": "BTC/USDT",
  "side": "SELL",
  "type": "LIMIT",
  "quantity": 0.005,
  "price": 50000
}
```

**Response**:
```json
{
  "id": "2",
  "symbol": "BTC/USDT",
  "side": "SELL",
  "type": "LIMIT",
  "quantity": 0.005,
  "price": 50000,
  "filled": 0,
  "remaining": 0.005,
  "status": "NEW",
  "createdAt": "2025-11-03T10:31:00Z",
  "updatedAt": "2025-11-03T10:31:00Z"
}
```

**What Happens**:
1. Validates you have sufficient BTC balance
2. Locks BTC quantity (0.005)
3. Creates pending order
4. Waits for matching by background service
5. Returns order in NEW status

---

### 3. Get Your Orders

**Endpoint**: `GET /api/trading/orders?page=1&pageSize=20`

**Optional Filters**:
- `symbol` - e.g., "BTC/USDT"
- `side` - "BUY" or "SELL"
- `type` - "MARKET" or "LIMIT"
- `status` - "NEW", "PARTIAL", "FILLED", "CANCELED"
- `fromDate` - ISO date string
- `toDate` - ISO date string

**Example**:
```
GET /api/trading/orders?status=NEW&side=SELL
```

**Response**:
```json
{
  "page": 1,
  "pageSize": 20,
  "totalItems": 1,
  "totalPages": 1,
  "data": [
    {
      "id": "2",
      "symbol": "BTC/USDT",
      "side": "SELL",
      "type": "LIMIT",
      "quantity": 0.005,
      "price": 50000,
      "filled": 0,
      "remaining": 0.005,
      "status": "NEW",
      "createdAt": "2025-11-03T10:31:00Z",
      "updatedAt": "2025-11-03T10:31:00Z"
    }
  ]
}
```

---

### 4. Get Order Details

**Endpoint**: `GET /api/trading/orders/{id}`

**Example**: `GET /api/trading/orders/1`

**Response**:
```json
{
  "id": "1",
  "symbol": "BTC/USDT",
  "side": "BUY",
  "type": "MARKET",
  "quantity": 0.01,
  "price": null,
  "filled": 0.01,
  "remaining": 0,
  "status": "FILLED",
  "createdAt": "2025-11-03T10:30:00Z",
  "updatedAt": "2025-11-03T10:30:01Z",
  "avgPrice": 45000.5,
  "totalFees": 4.50,
  "trades": [
    {
      "id": "1",
      "orderId": "1",
      "symbol": "BTC/USDT",
      "price": 45000.5,
      "quantity": 0.01,
      "fee": 4.50,
      "createdAt": "2025-11-03T10:30:01Z"
    }
  ]
}
```

---

### 5. Cancel Order

**Endpoint**: `DELETE /api/trading/orders/{id}`

**Example**: `DELETE /api/trading/orders/2`

**Response**:
```json
{
  "id": "2",
  "symbol": "BTC/USDT",
  "side": "SELL",
  "type": "LIMIT",
  "quantity": 0.005,
  "price": 50000,
  "filled": 0,
  "remaining": 0.005,
  "status": "CANCELED",
  "createdAt": "2025-11-03T10:31:00Z",
  "updatedAt": "2025-11-03T10:32:00Z"
}
```

**What Happens**:
1. Validates order belongs to you
2. Checks order is not already filled/canceled
3. Releases all locked balances
4. Updates status to CANCELED

---

### 6. Get Trade History

**Endpoint**: `GET /api/trading/trades?page=1&pageSize=50`

**Optional Filters**:
- `symbol` - e.g., "BTC/USDT"
- `orderId` - specific order ID
- `fromDate` - ISO date string
- `toDate` - ISO date string

**Example**:
```
GET /api/trading/trades?symbol=BTC/USDT&fromDate=2025-11-01
```

**Response**:
```json
{
  "page": 1,
  "pageSize": 50,
  "totalItems": 5,
  "totalPages": 1,
  "data": [
    {
      "id": "1",
      "orderId": "1",
      "symbol": "BTC/USDT",
      "price": 45000.5,
      "quantity": 0.01,
      "fee": 4.50,
      "createdAt": "2025-11-03T10:30:01Z"
    }
  ]
}
```

---

### 7. Get Order Book

**Endpoint**: `GET /api/trading/orderbook/{symbol}?depth=20`

**Example**: `GET /api/trading/orderbook/BTC/USDT?depth=10`

**Response**:
```json
{
  "symbol": "BTC/USDT",
  "currentPrice": 45000.50,
  "priceChange24h": 1200.30,
  "priceChangePercentage24h": 2.74,
  "asks": [
    {
      "price": 45100.00,
      "quantity": 0.5,
      "total": 22550.00,
      "orderCount": 3
    }
  ],
  "bids": [
    {
      "price": 44900.00,
      "quantity": 0.3,
      "total": 13470.00,
      "orderCount": 2
    }
  ],
  "lastUpdated": "2025-11-03T10:35:00Z"
}
```

**Notes**:
- `asks` = sell orders (sorted price ascending)
- `bids` = buy orders (sorted price descending)
- Shows aggregated price levels
- Includes current market price from CoinGecko

---

## 🧪 Testing Scenarios

### Scenario 1: Simple Market Buy
```bash
# 1. Buy 0.01 BTC at market price
POST /api/trading/orders
{
  "symbol": "BTC/USDT",
  "side": "BUY",
  "type": "MARKET",
  "quantity": 0.01
}

# 2. Check your balances
GET /api/trading/balances

# 3. View trade history
GET /api/trading/trades
```

### Scenario 2: Limit Order Matching
```bash
# User A: Place limit sell order
POST /api/trading/orders
{
  "symbol": "BTC/USDT",
  "side": "SELL",
  "type": "LIMIT",
  "quantity": 0.01,
  "price": 45000
}

# User B: Place limit buy order (matching price)
POST /api/trading/orders
{
  "symbol": "BTC/USDT",
  "side": "BUY",
  "type": "LIMIT",
  "quantity": 0.01,
  "price": 45000
}

# Wait 5-10 seconds for background service

# Check order status (should be FILLED)
GET /api/trading/orders/{orderId}
```

### Scenario 3: Cancel Pending Order
```bash
# 1. Place limit order
POST /api/trading/orders
{
  "symbol": "ETH/USDT",
  "side": "BUY",
  "type": "LIMIT",
  "quantity": 0.5,
  "price": 2500
}

# 2. Get order ID from response

# 3. Cancel before it fills
DELETE /api/trading/orders/{orderId}

# 4. Verify balance unlocked
GET /api/trading/balances
```

---

## 🔍 Checking Database Changes

### View Orders
```sql
SELECT Id, UserId, Side, Type, Status, QuantityCoin, FilledQty, PriceUsd, CreatedAt
FROM Orders
WHERE UserId = 1
ORDER BY CreatedAt DESC;
```

### View Trades
```sql
SELECT t.Id, t.OrderId, t.PriceUsd, t.QuantityCoin, t.FeeUsd, t.CreatedAt,
       o.Side, o.UserId
FROM Trades t
JOIN Orders o ON t.OrderId = o.Id
WHERE o.UserId = 1
ORDER BY t.CreatedAt DESC;
```

### View Balances
```sql
SELECT w.Id, w.AssetType, w.CurrencyCode, c.Symbol,
       SUM(wm.Amount) as TotalBalance,
       (SELECT SUM(oh.Amount) 
        FROM OrderHolds oh 
        WHERE oh.WalletId = w.Id AND oh.ReleasedAt IS NULL) as LockedBalance
FROM Wallets w
LEFT JOIN WalletMovements wm ON w.Id = wm.WalletId
LEFT JOIN Cryptocurrencies c ON w.CryptocurrencyId = c.Id
WHERE w.UserId = 1
GROUP BY w.Id;
```

### View Order Holds
```sql
SELECT oh.Id, oh.OrderId, oh.WalletId, oh.Amount, oh.CreatedAt, oh.ReleasedAt,
       o.Side, o.Type, o.Status
FROM OrderHolds oh
JOIN Orders o ON oh.OrderId = o.Id
WHERE o.UserId = 1
ORDER BY oh.CreatedAt DESC;
```

---

## 🐛 Troubleshooting

### "Insufficient balance" Error
**Solution**: Add funds to your wallet using SQL (see "Add Balance" section above)

### "Cryptocurrency not found" Error
**Solution**: Ensure the symbol exists in the Cryptocurrencies table:
```sql
SELECT * FROM Cryptocurrencies WHERE Symbol = 'BTC';
```

### "Unauthorized" Error
**Solution**: 
1. Login to get a fresh JWT token
2. Add `Authorization: Bearer {token}` header
3. Token expires after configured time

### Order Not Matching
**Possible Reasons**:
1. Background service not running (check logs)
2. Price mismatch (buy price < sell price)
3. Insufficient quantity
4. Orders from same user (can't match with yourself)

### Check Background Service Logs
```bash
# Look for logs like:
# "Order matching completed: {count} matches made"
# "OrderMatchingBackgroundService is starting"
```

---

## 📊 Monitoring

### Logs to Watch
- Order placement: `Order {OrderId} placed: {Side} {Quantity} {Symbol} @ {Price}`
- Order execution: `Market order {OrderId} executed: {Filled}/{Total}`
- Order matching: `Order matching completed: {Count} matches made`
- Cancellations: `Order {OrderId} canceled by user {UserId}`

### Health Check
```bash
GET http://localhost:5000/health
```

---

## 🎯 Success Indicators

✅ Order placed successfully  
✅ Balance locked correctly  
✅ Market order executed immediately  
✅ Trade record created  
✅ Wallets updated (debits/credits)  
✅ OrderHold released  
✅ Limit orders matched by background service  
✅ Order canceled and balance released  
✅ Trade history accurate  
✅ Order book shows real orders  

---

## 📚 Additional Resources

- **Full Documentation**: `docs/TRADING_SYSTEM_IMPLEMENTATION.md`
- **Implementation Summary**: `PHASE_1_2_COMPLETION_SUMMARY.md`
- **Swagger UI**: `http://localhost:5000/swagger`

---

## 🆘 Need Help?

1. Check the comprehensive documentation
2. Review Swagger API documentation
3. Check application logs
4. Verify database state
5. Contact the trading team

---

**Happy Trading!** 🚀📈

