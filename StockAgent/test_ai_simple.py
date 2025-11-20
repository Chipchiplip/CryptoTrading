"""
Simple test for AI Recommendation API
Run this after starting FastAPI service: uvicorn app:app --host 0.0.0.0 --port 8000
"""

import requests
import json

BASE_URL = "http://localhost:8000"

print("Testing AI Recommendation API...")
print("=" * 60)

# Test 1: Health check
try:
    response = requests.get(f"{BASE_URL}/health", timeout=5)
    print(f"Health Check: {response.status_code} - {response.json()}")
except Exception as e:
    print(f"ERROR: Service not running. Please start with:")
    print("  cd StockAgent")
    print("  uvicorn app:app --host 0.0.0.0 --port 8000")
    exit(1)

# Test 2: Get recommendation
print("\nRequesting AI recommendation...")
payload = {
    "user_id": 1,
    "bot_id": None,
    "trading_plan_id": "TEST_001",
    "trading_plan": {
        "preferred_symbols": ["BTCUSDT"],
        "strategy_type": "trend following",
        "risk_mode": "normal",
        "max_capital_per_trade": 1000.0,
        "max_daily_exposure": 5000.0,
        "time_horizon": "intraday"
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
        "has_bad_news": False
    }
}

try:
    response = requests.post(
        f"{BASE_URL}/ai/recommendations",
        json=payload,
        headers={"Content-Type": "application/json"},
        timeout=30
    )
    
    if response.status_code == 200:
        result = response.json()
        print("\nSUCCESS - Recommendation received:")
        print(json.dumps(result, indent=2))
        print(f"\nDecision: {result['decision']}")
        print(f"Symbol: {result['symbol']}")
        print(f"Amount: ${result['amount_usdt']:.2f}")
        print(f"Confidence: {result['confidence']:.1%}")
    else:
        print(f"ERROR: {response.status_code} - {response.text}")
except Exception as e:
    print(f"ERROR: {e}")

print("\n" + "=" * 60)

