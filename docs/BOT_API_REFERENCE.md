# Bot API Reference

## Response Structure: TradingBotDetailDto

### Full Response Example

```json
{
  "id": "543a31df-7142-46d6-9877-947b4d332a62",
  "userId": 1,
  "name": "Aggressive ETHUSD Trend Following",
  "status": "Running",
  "riskProfile": "AGGRESSIVE",
  "baseAsset": "ETH",
  "quoteAsset": "USD",
  "strategy": {
    "id": "e1090fdd-1237-4825-b41d-b0026894869f",
    "key": "aggressive-forex",
    "version": "1.0.0",
    "displayName": "Aggressive Forex Style"
  },
  "parameters": {
    "emaPeriod": 200,
    "initialLot": 0.1,
    "capitalAllocation": 100000,
    "cutLossUSD": 10000,
    "pyramidEnabled": true,
    "martingaleEnabled": true
  },
  "positionSizing": {
    "maxDailyExposure": 40000,
    "maxCapitalPerTrade": 5000
  },
  "executionIntervalSeconds": 60,
  "nextRunAt": "2025-11-30T12:00:00.000Z",
  "lastStatusReason": null,
  "runtime": {
    "nextRunAt": "2025-11-30T12:00:00.000Z",
    "openPositions": 2,
    "unrealizedPnl": 125.50,
    "realizedPnl": 350.25,
    "totalFees": 12.50,
    "totalOrders": 15,
    "filledOrders": 12,
    "lastSignal": "BUY ETH @ $3155.23",
    "lastExecutionAt": "2025-11-30T11:59:00.000Z",
    "heartbeatAt": "2025-11-30T11:59:30.000Z"
  },
  "createdAt": "2025-11-30T11:18:44.543739Z",
  "updatedAt": "2025-11-30T11:59:30.000Z"
}
```

## Field Descriptions

### Top Level Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | Guid | Unique bot identifier |
| `userId` | int | Owner user ID |
| `name` | string | Bot display name |
| `status` | string | Bot status: "Draft", "Starting", "Running", "Stopped", "Error", "Degraded" |
| `riskProfile` | string | Risk level: "LOW", "MODERATE", "AGGRESSIVE", "SAFE" |
| `baseAsset` | string | Base cryptocurrency (e.g., "ETH", "BTC") |
| `quoteAsset` | string | Quote currency (should be "USD", not "USDT") |
| `executionIntervalSeconds` | int | How often bot runs (in seconds) |
| `nextRunAt` | DateTime? | Next scheduled execution time |
| `lastStatusReason` | string? | Reason for last status change (error message, etc.) |
| `createdAt` | DateTime | Bot creation timestamp |
| `updatedAt` | DateTime | Last update timestamp |

### Strategy Object

| Field | Type | Description |
|-------|------|-------------|
| `id` | Guid | Strategy definition ID |
| `key` | string | Strategy key (e.g., "aggressive-forex", "grid-basic") |
| `version` | string | Strategy version |
| `displayName` | string | Human-readable strategy name |

### Parameters Object

Strategy-specific parameters. Varies by strategy type.

**For Aggressive Forex Strategy:**
```json
{
  "emaPeriod": 200,
  "emaShift": 5,
  "initialLot": 0.1,
  "capitalAllocation": 100000,
  "pyramidEnabled": true,
  "pyramidTriggerPercent": 3.0,
  "martingaleEnabled": true,
  "martingaleDistancePercent": 4.0,
  "rsiPeriod": 5,
  "rsiOverbought": 80,
  "rsiOversold": 20,
  "cutLossUSD": 10000,
  "trailingEnabled": true,
  "executionIntervalSeconds": 60
}
```

**For Grid Trading Strategy:**
```json
{
  "gridLevels": 10,
  "lowerBound": 3000,
  "upperBound": 4000,
  "orderSize": 0.01,
  "capitalAllocation": 10000,
  "rebalanceMode": "balanced"
}
```

### PositionSizing Object

| Field | Type | Description |
|-------|------|-------------|
| `maxCapitalPerTrade` | decimal | Maximum capital per single trade |
| `maxDailyExposure` | decimal | Maximum total exposure per day |

### Runtime Object

| Field | Type | Description |
|-------|------|-------------|
| `nextRunAt` | DateTime? | Next execution time |
| `openPositions` | int | Number of open positions |
| `unrealizedPnl` | decimal | Unrealized profit/loss |
| `realizedPnl` | decimal | Realized profit/loss |
| `totalFees` | decimal | Total fees paid |
| `totalOrders` | int | Total orders placed |
| `filledOrders` | int | Number of filled orders |
| `lastSignal` | string? | Last trading signal (e.g., "BUY ETH @ $3155.23") |
| `lastExecutionAt` | DateTime? | Last execution timestamp |
| `heartbeatAt` | DateTime? | Last heartbeat (bot alive signal) |

## Bot Status Values

- **Draft**: Bot created but not started
- **Starting**: Bot is initializing
- **Running**: Bot is active and trading
- **Stopped**: Bot was manually stopped
- **Error**: Bot encountered an error (check `lastStatusReason`)
- **Degraded**: Bot is running but with issues

## Common Operations

### Create Bot

```http
POST /api/bots
Content-Type: application/json

{
  "name": "My Trading Bot",
  "strategyDefinitionId": "e1090fdd-1237-4825-b41d-b0026894869f",
  "baseAsset": "ETH",
  "quoteAsset": "USD",
  "riskProfile": "AGGRESSIVE",
  "parameters": {
    "initialLot": 0.1,
    "capitalAllocation": 100000,
    "cutLossUSD": 10000
  },
  "executionIntervalSeconds": 60
}
```

### Get Bot Details

```http
GET /api/bots/{botId}
```

### Start Bot

```http
POST /api/bots/{botId}/start
Content-Type: application/json

{
  "mode": "live"
}
```

### Stop Bot

```http
POST /api/bots/{botId}/stop
Content-Type: application/json

{
  "reason": "user"
}
```

### Get Bot Logs

```http
GET /api/bots/{botId}/logs?page=1&pageSize=50&level=Error
```

### Get Bot Orders

```http
GET /api/bots/{botId}/orders?page=1&pageSize=20
```

### Nudge Bot (Force Immediate Execution)

```http
POST /api/bots/{botId}/nudge
```

## Notes

1. **QuoteAsset**: Always use "USD", not "USDT". The system automatically normalizes USDT to USD.

2. **Status Monitoring**: Check `runtime.totalOrders` to see if bot is actually trading. If it's 0, check logs for errors.

3. **Error Handling**: If `status` is "Error", check `lastStatusReason` for details.

4. **Runtime Metrics**: `runtime` object is calculated from actual orders in database, so it reflects real trading activity.

