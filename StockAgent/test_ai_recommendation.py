"""
Quick test script for AI Recommendation API
"""

import requests
import json
import time

BASE_URL = "http://localhost:8000"

def test_health():
    """Test health endpoint"""
    print("=" * 60)
    print("1. Testing Health Check")
    print("=" * 60)
    try:
        response = requests.get(f"{BASE_URL}/health", timeout=5)
        print(f"Status: {response.status_code}")
        print(f"Response: {json.dumps(response.json(), indent=2)}")
        return response.status_code == 200
    except requests.exceptions.ConnectionError:
        print("ERROR: Cannot connect to FastAPI service.")
        print("Please start the service first:")
        print("  cd StockAgent")
        print("  uvicorn app:app --host 0.0.0.0 --port 8000")
        return False
    except Exception as e:
            print(f"Error: {e}")
        return False

def test_recommendation_with_bot():
    """Test recommendation with bot context"""
    print("\n" + "=" * 60)
    print("2. Testing AI Recommendation (with bot context)")
    print("=" * 60)
    
    payload = {
        "user_id": 1,
        "bot_id": None,  # No bot, use user defaults
        "trading_plan_id": "TEST_USER_001",
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
    
    try:
        print("Sending request...")
        response = requests.post(
            f"{BASE_URL}/ai/recommendations",
            json=payload,
            headers={"Content-Type": "application/json"},
            timeout=30
        )
        
        print(f"\nStatus: {response.status_code}")
        
        if response.status_code == 200:
            result = response.json()
            print(f"\n✅ Recommendation received:")
            print(json.dumps(result, indent=2))
            print(f"\n📊 Summary:")
            print(f"   Decision: {result['decision']}")
            print(f"   Symbol: {result['symbol']}")
            print(f"   Amount: ${result['amount_usdt']:.2f}")
            print(f"   Confidence: {result['confidence']:.1%}")
            print(f"   Reason: {result['reason']}")
            return result
        else:
            print(f"❌ Error: {response.text}")
            return None
    except Exception as e:
            print(f"Error: {e}")
        return None

def test_recommendation_no_trade():
    """Test recommendation that should return NO_TRADE"""
    print("\n" + "=" * 60)
    print("3. Testing AI Recommendation (NO_TRADE scenario)")
    print("=" * 60)
    
    payload = {
        "user_id": 1,
        "bot_id": None,
        "trading_plan_id": "TEST_NO_TRADE",
        "trading_plan": {
            "preferred_symbols": ["BTCUSDT"],
            "strategy_type": "trend following",
            "risk_mode": "normal",
            "max_capital_per_trade": 1000.0,
            "restrictions": {
                "high_volatility": True
            }
        },
        "market_snapshot": {
            "symbol": "BTCUSDT",
            "price": 96500.0,
            "trend_1h": "neutral",
            "trend_4h": "neutral",
            "volume_vs_ma": 5.0,
            "volatility": 0.15,  # High volatility
            "usdt_balance": 5000.0,
            "btc_holding": 0.0,
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
            print(f"SUCCESS - Recommendation: {result['decision']}")
            print(f"   Reason: {result['reason']}")
            return result['decision'] == "NO_TRADE"
        else:
            print(f"❌ Error: {response.text}")
            return False
    except Exception as e:
            print(f"Error: {e}")
        return False

def main():
    print("\n" + "=" * 60)
    print("AI Recommendation API Test Suite")
    print("=" * 60)
    print()
    
    # Test 1: Health check
    if not test_health():
        return
    
    # Test 2: Get recommendation
    recommendation = test_recommendation_with_bot()
    
    # Test 3: NO_TRADE scenario
    test_recommendation_no_trade()
    
    print("\n" + "=" * 60)
    print("Test completed!")
    print("=" * 60)
    
    if recommendation:
        print(f"\n💡 Next steps:")
        print(f"   1. Recommendation ID: {recommendation.get('id', 'N/A')}")
        print(f"   2. Test .NET backend: POST /api/ai/recommendations")
        print(f"   3. Apply to bot: POST /api/ai/recommendations/{recommendation.get('id')}/apply-to-bot")
        print(f"   4. Place order: POST /api/ai/recommendations/{recommendation.get('id')}/place-order")

if __name__ == "__main__":
    main()

