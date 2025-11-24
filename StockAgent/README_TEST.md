# How to Test AI Recommendation

## Step 1: Start FastAPI Service

Open a new terminal/PowerShell window:

```powershell
cd D:\FPT\Intern\Project\StockAgent
uvicorn app:app --host 0.0.0.0 --port 8000 --reload
```

Service will start at: `http://localhost:8000`

## Step 2: Test the API

In another terminal:

```powershell
cd D:\FPT\Intern\Project\StockAgent
python test_ai_simple.py
```

Or test manually with curl:

```bash
# Health check
curl http://localhost:8000/health

# Get recommendation
curl -X POST http://localhost:8000/ai/recommendations \
  -H "Content-Type: application/json" \
  -d @test_request.json
```

## Step 3: Test from .NET Backend

1. Make sure FastAPI service is running (Step 1)
2. Start .NET backend: `dotnet run`
3. Call API: `POST /api/ai/recommendations` with:
```json
{
  "userId": 1,
  "botId": null
}
```

## Expected Response

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

