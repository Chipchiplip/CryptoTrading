# AI Trading Recommendation API

FastAPI microservice for generating crypto spot trading recommendations.

## Quick Start

### 1. Install Dependencies

```bash
cd StockAgent
pip install -r requirements.txt
```

### 2. Start the Service

```bash
uvicorn app:app --host 0.0.0.0 --port 8000 --reload
```

Service will be available at: `http://localhost:8000`

### 3. Test the Service

```bash
# Health check
curl http://localhost:8000/health

# Get recommendation
python test_api.py
```

## API Endpoints

### GET /health
Health check endpoint.

**Response:**
```json
{
  "status": "ok",
  "service": "ai-recommendation-engine"
}
```

### POST /ai/recommendations
Generate AI trading recommendation.

**Request Body:**
```json
{
  "user_id": 1,
  "bot_id": null,
  "trading_plan_id": "PLAN_001",
  "trading_plan": {
    "preferred_symbols": ["BTCUSDT", "ETHUSDT"],
    "strategy_type": "trend following",
    "risk_mode": "normal",
    "max_capital_per_trade": 1000.0,
    "max_daily_exposure": 5000.0,
    "time_horizon": "intraday",
    "min_confidence": 0.6
  },
  "market_snapshot": {
    "symbol": "BTCUSDT",
    "price": 96500.0,
    "trend_1h": "uptrend",
    "trend_4h": "uptrend",
    "volume_vs_ma": 15.5,
    "volatility": 0.04,
    "support": 95000.0,
    "resistance": 98000.0,
    "usdt_balance": 5000.0,
    "btc_holding": 0.0,
    "eth_holding": 0.0,
    "has_bad_news": false
  }
}
```

**Response:**
```json
{
  "id": "REC_abc123...",
  "decision": "BUY",
  "symbol": "BTCUSDT",
  "amount_usdt": 600.0,
  "reason": "Strong uptrend on 1h/4h, volume +15.5%",
  "confidence": 0.631,
  "time_horizon": "intraday"
}
```

## Integration with .NET Backend

1. Register HttpClient in `Program.cs`:
```csharp
builder.Services.AddHttpClient("AiRecommendationService", client =>
{
    client.BaseAddress = new Uri("http://localhost:8000");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

2. Use `AiRecommendationsController` to call the service.

3. See `Program.cs.AiServiceRegistration` for full setup.

## Docker Deployment

```dockerfile
FROM python:3.11-slim

WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8000"]
```

## Production Notes

- Configure CORS properly (currently allows all origins)
- Use environment variables for database connection
- Disable `reload=True` in production
- Add authentication/authorization if needed
- Set up proper logging and monitoring

