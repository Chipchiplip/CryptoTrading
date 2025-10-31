# Crypto Trading Dashboard API

Node.js/Express API server for dashboard endpoints.

## Setup

1. Install dependencies:
```bash
npm install
```

2. Copy `.env.example` to `.env` and configure:
```bash
cp .env.example .env
```

Edit `.env`:
```
DB_HOST=localhost
DB_PORT=3306
DB_USER=root
DB_PASS=your_password
DB_NAME=crypto_trading
PORT=4000
```

3. Run the server:
```bash
# Production
npm start

# Development (with auto-reload)
npm run dev
```

## Endpoints

### GET /dashboard/summary?userId=8
Returns dashboard summary:
```json
{
  "navTotal": 32500.00,
  "todayPnl": 156.25,
  "availableBalance": 8104.69,
  "openOrdersCount": 2
}
```

### GET /dashboard/nav?userId=8&days=30
Returns NAV history:
```json
[
  { "t": "2025-10-02", "v": 31000.00 },
  { "t": "2025-10-03", "v": 31200.00 },
  ...
]
```

### GET /dashboard/pnl?userId=8&granularity=hourly&date=today
Returns PnL history by hour:
```json
[
  { "h": "00", "v": 0 },
  { "h": "01", "v": 0 },
  ...
  { "h": "14", "v": 156.25 }
]
```

### GET /health
Health check endpoint.

## Notes

- Uses MySQL connection pooling for performance
- All numeric values return 0 if null
- Date format: YYYY-MM-DD
- Hour format: HH (00-23)

