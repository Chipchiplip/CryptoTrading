"""
Test script for FastAPI service
"""

import requests
import json

BASE_URL = "http://localhost:8000"

def test_health():
    """Test health endpoint"""
    print("Testing /health endpoint...")
    response = requests.get(f"{BASE_URL}/health")
    print(f"Status: {response.status_code}")
    print(f"Response: {json.dumps(response.json(), indent=2)}")
    print()

def test_recommendation():
    """Test recommendation endpoint"""
    print("Testing /ai/recommendations endpoint...")
    
    payload = {
        "user_id": 1,
        "bot_id": None,
        "trading_plan_id": "TEST_PLAN_API",
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
            "has_bad_news": False
        }
    }
    
    response = requests.post(
        f"{BASE_URL}/ai/recommendations",
        json=payload,
        headers={"Content-Type": "application/json"}
    )
    
    print(f"Status: {response.status_code}")
    if response.status_code == 200:
        print(f"Response: {json.dumps(response.json(), indent=2)}")
    else:
        print(f"Error: {response.text}")
    print()

if __name__ == "__main__":
    print("=" * 60)
    print("Testing FastAPI Service")
    print("=" * 60)
    print()
    
    try:
        test_health()
        test_recommendation()
        print("=" * 60)
        print("Tests completed!")
        print("=" * 60)
    except requests.exceptions.ConnectionError:
        print("ERROR: Cannot connect to FastAPI service.")
        print("Please start the service first:")
        print("  cd StockAgent")
        print("  uvicorn app:app --host 0.0.0.0 --port 8000")
    except Exception as e:
        print(f"Error: {e}")

