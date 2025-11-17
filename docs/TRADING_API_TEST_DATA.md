# Trading API Test Data

Collection of test requests for all Trading API endpoints.

## Base URL
```
http://localhost:5299/api/trading
```

## Authentication
All endpoints require JWT token in `Authorization: Bearer <token>` header.

---

## 1. Place Order (POST /api/trading/orders)

### Test Case 1: BUY LIMIT Order - BTC/USDT
```json
{
  "symbol": "BTC/USDT",
  "side": "BUY",
  "type": "LIMIT",
  "quantity": 0.001,
  "price": 50000.00
}
```

### Test Case 2: SELL LIMIT Order - BTC/USDT
```json
{
  "symbol": "BTC/USDT",
  "side": "SELL",
  "type": "LIMIT",
  "quantity": 0.0005,
  "price": 52000.00
}
```

### Test Case 3: BUY MARKET Order - ETH/USDT
```json
{
  "symbol": "ETH/USDT",
  "side": "BUY",
  "type": "MARKET",
  "quantity": 0.01
}
```

### Test Case 4: SELL MARKET Order - ETH/USDT
```json
{
  "symbol": "ETH/USDT",
  "side": "SELL",
  "type": "MARKET",
  "quantity": 0.005
}
```

### Test Case 5: Small Quantity LIMIT Order
```json
{
  "symbol": "BTC/USDT",
  "side": "BUY",
  "type": "LIMIT",
  "quantity": 0.00000001,
  "price": 0.01
}
```

### Test Case 6: High Price LIMIT Order
```json
{
  "symbol": "BTC/USDT",
  "side": "BUY",
  "type": "LIMIT",
  "quantity": 1.0,
  "price": 100000.00
}
```

### Test Case 7: Multiple Trading Pairs
```json
// BNB/USDT
{
  "symbol": "BNB/USDT",
  "side": "BUY",
  "type": "LIMIT",
  "quantity": 0.1,
  "price": 300.00
}

// SOL/USDT
{
  "symbol": "SOL/USDT",
  "side": "SELL",
  "type": "LIMIT",
  "quantity": 0.5,
  "price": 150.00
}

// ADA/USDT
{
  "symbol": "ADA/USDT",
  "side": "BUY",
  "type": "MARKET",
  "quantity": 100.0
}
```

---

## 2. Get Orders (GET /api/trading/orders)

### Test Case 1: Get All Orders
```
GET /api/trading/orders
```

### Test Case 2: Filter by Symbol
```
GET /api/trading/orders?symbol=BTC/USDT
```

### Test Case 3: Filter by Side
```
GET /api/trading/orders?side=BUY
GET /api/trading/orders?side=SELL
```

### Test Case 4: Filter by Type
```
GET /api/trading/orders?type=LIMIT
GET /api/trading/orders?type=MARKET
```

### Test Case 5: Filter by Status
```
GET /api/trading/orders?status=NEW
GET /api/trading/orders?status=NEW&status=PARTIAL
GET /api/trading/orders?status=FILLED
GET /api/trading/orders?status=CANCELED
```

### Test Case 6: Date Range Filter
```
GET /api/trading/orders?fromDate=2025-01-01&toDate=2025-01-31
```

### Test Case 7: Pagination
```
GET /api/trading/orders?page=1&pageSize=10
GET /api/trading/orders?page=2&pageSize=20
```

### Test Case 8: Combined Filters
```
GET /api/trading/orders?symbol=BTC/USDT&side=BUY&status=NEW&page=1&pageSize=10
```

---

## 3. Get Order Detail (GET /api/trading/orders/{id})

### Test Case 1: Get Order by ID
```
GET /api/trading/orders/1234567890
```

Replace `1234567890` with actual order ID from previous place order response.

---

## 4. Cancel Order (DELETE /api/trading/orders/{id})

### Test Case 1: Cancel Order
```
DELETE /api/trading/orders/1234567890
```

---

## 5. Get Trades (GET /api/trading/trades)

### Test Case 1: Get All Trades
```
GET /api/trading/trades
```

### Test Case 2: Filter by Symbol
```
GET /api/trading/trades?symbol=BTC/USDT
```

### Test Case 3: Filter by Order ID
```
GET /api/trading/trades?orderId=1234567890
```

### Test Case 4: Date Range
```
GET /api/trading/trades?fromDate=2025-01-01&toDate=2025-01-31
```

### Test Case 5: Pagination
```
GET /api/trading/trades?page=1&pageSize=20
```

### Test Case 6: Combined
```
GET /api/trading/trades?symbol=BTC/USDT&fromDate=2025-01-01&page=1&pageSize=10
```

---

## 6. Get Balances (GET /api/trading/balances)

### Test Case 1: Get All Balances
```
GET /api/trading/balances
```

No parameters required.

---

## 7. Get Holdings (GET /api/trading/holdings)

### Test Case 1: Get Portfolio Holdings
```
GET /api/trading/holdings
```

---

## 8. Get Order Book (GET /api/trading/orderbook)

### Test Case 1: BTC/USDT Order Book
```
GET /api/trading/orderbook?symbolQuery=BTC/USDT
```

### Test Case 2: ETH/USDT Order Book
```
GET /api/trading/orderbook?symbolQuery=ETH/USDT
```

### Test Case 3: With Depth Parameter
```
GET /api/trading/orderbook?symbolQuery=BTC/USDT&depth=50
```

---

## 9. Get Dashboard (GET /api/trading/dashboard)

### Test Case 1: Full Dashboard
```
GET /api/trading/dashboard
```

---

## 10. Get Dashboard Summary (GET /api/trading/dashboard/summary)

### Test Case 1: Dashboard Summary
```
GET /api/trading/dashboard/summary
```

---

## 11. Get NAV History (GET /api/trading/dashboard/nav)

### Test Case 1: Default (Last 30 days)
```
GET /api/trading/dashboard/nav
```

### Test Case 2: Custom Date Range
```
GET /api/trading/dashboard/nav?from=2025-01-01
```

---

## 12. Get PnL History (GET /api/trading/dashboard/pnl)

### Test Case 1: Hourly (Today)
```
GET /api/trading/dashboard/pnl?granularity=hourly
```

### Test Case 2: Daily (This Week)
```
GET /api/trading/dashboard/pnl?granularity=daily
```

### Test Case 3: Weekly (Last 4 Weeks)
```
GET /api/trading/dashboard/pnl?granularity=weekly
```

### Test Case 4: Custom Date
```
GET /api/trading/dashboard/pnl?granularity=hourly&date=2025-01-15
```

---

## Test Scenarios

### Scenario 1: Complete Trading Flow
1. **Check Balance**
   ```
   GET /api/trading/balances
   ```

2. **Place BUY LIMIT Order**
   ```json
   POST /api/trading/orders
   {
     "symbol": "BTC/USDT",
     "side": "BUY",
     "type": "LIMIT",
     "quantity": 0.001,
     "price": 50000.00
   }
   ```

3. **Check Order Status**
   ```
   GET /api/trading/orders/{orderId}
   ```

4. **Get Order Book** (to see your order)
   ```
   GET /api/trading/orderbook?symbolQuery=BTC/USDT
   ```

5. **Cancel Order** (if needed)
   ```
   DELETE /api/trading/orders/{orderId}
   ```

6. **Check Trades** (if order filled)
   ```
   GET /api/trading/trades?orderId={orderId}
   ```

7. **Check Updated Balance**
   ```
   GET /api/trading/balances
   ```

### Scenario 2: Market Order Test
1. **Place MARKET BUY**
   ```json
   POST /api/trading/orders
   {
     "symbol": "ETH/USDT",
     "side": "BUY",
     "type": "MARKET",
     "quantity": 0.01
   }
   ```

2. **Check Immediate Fill**
   ```
   GET /api/trading/orders/{orderId}
   ```

3. **Verify Trade Created**
   ```
   GET /api/trading/trades?orderId={orderId}
   ```

### Scenario 3: Portfolio Analysis
1. **Get Holdings**
   ```
   GET /api/trading/holdings
   ```

2. **Get Dashboard**
   ```
   GET /api/trading/dashboard
   ```

3. **Get NAV History**
   ```
   GET /api/trading/dashboard/nav
   ```

4. **Get PnL History**
   ```
   GET /api/trading/dashboard/pnl?granularity=hourly
   ```

---

## Common Trading Pairs

Based on system configuration, common pairs include:
- `BTC/USDT`
- `ETH/USDT`
- `BNB/USDT`
- `SOL/USDT`
- `ADA/USDT`
- `DOT/USDT`
- `MATIC/USDT`
- `AVAX/USDT`

---

## Error Test Cases

### Invalid Symbol
```json
{
  "symbol": "INVALID/PAIR",
  "side": "BUY",
  "type": "LIMIT",
  "quantity": 0.001,
  "price": 50000.00
}
```

### Invalid Side
```json
{
  "symbol": "BTC/USDT",
  "side": "INVALID",
  "type": "LIMIT",
  "quantity": 0.001,
  "price": 50000.00
}
```

### Invalid Type
```json
{
  "symbol": "BTC/USDT",
  "side": "BUY",
  "type": "INVALID",
  "quantity": 0.001,
  "price": 50000.00
}
```

### Missing Price for LIMIT
```json
{
  "symbol": "BTC/USDT",
  "side": "BUY",
  "type": "LIMIT",
  "quantity": 0.001
}
```

### Zero Quantity
```json
{
  "symbol": "BTC/USDT",
  "side": "BUY",
  "type": "LIMIT",
  "quantity": 0,
  "price": 50000.00
}
```

### Negative Price
```json
{
  "symbol": "BTC/USDT",
  "side": "BUY",
  "type": "LIMIT",
  "quantity": 0.001,
  "price": -100.00
}
```

---

## cURL Examples

### Place Order
```bash
curl -X POST http://localhost:5299/api/trading/orders \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTC/USDT",
    "side": "BUY",
    "type": "LIMIT",
    "quantity": 0.001,
    "price": 50000.00
  }'
```

### Get Orders
```bash
curl -X GET "http://localhost:5299/api/trading/orders?symbol=BTC/USDT&side=BUY" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Get Balances
```bash
curl -X GET http://localhost:5299/api/trading/balances \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Cancel Order
```bash
curl -X DELETE http://localhost:5299/api/trading/orders/1234567890 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## Postman Collection

You can import these into Postman:

### Environment Variables
- `baseUrl`: `http://localhost:5299`
- `token`: Your JWT token

### Collection Structure
```
Trading API
├── Orders
│   ├── Place Order (BUY LIMIT)
│   ├── Place Order (SELL LIMIT)
│   ├── Place Order (BUY MARKET)
│   ├── Place Order (SELL MARKET)
│   ├── Get Orders
│   ├── Get Order by ID
│   └── Cancel Order
├── Trades
│   ├── Get Trades
│   └── Get Trades (Filtered)
├── Balances
│   └── Get Balances
├── Holdings
│   └── Get Holdings
├── Order Book
│   └── Get Order Book
└── Dashboard
    ├── Get Dashboard
    ├── Get Summary
    ├── Get NAV History
    └── Get PnL History
```

