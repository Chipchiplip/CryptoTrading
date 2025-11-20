"""
Test script for LiveRecommendationEngine with database integration.
"""

import json
from live_recommendation_engine import LiveRecommendationEngine
from log.custom_logger import log
import pymysql
import util

def test_recommendation_engine():
    """Test the recommendation engine and database saving."""
    
    print("=" * 60)
    print("Testing LiveRecommendationEngine + Database")
    print("=" * 60)
    
    # Create engine instance
    engine = LiveRecommendationEngine()
    
    # Test Case 1: Trend Following - BUY signal
    print("\n[Test 1] Trend Following - Strong Uptrend")
    print("-" * 60)
    
    trading_plan_1 = {
        "strategy_type": "trend following",
        "risk_mode": "normal",
        "max_capital_per_trade": 1000.0,
        "max_daily_exposure": 5000.0,
        "symbols": ["BTCUSDT", "ETHUSDT"],
        "time_horizon": "intraday",
        "min_confidence": 0.6,
        "forbidden_conditions": {
            "high_volatility": True,
            "time_restrictions": []
        }
    }
    
    market_snapshot_1 = {
        "symbols": {
            "BTCUSDT": {
                "price": 96500.0,
                "trend": {"1h": "uptrend", "4h": "uptrend"},
                "volume_change_pct": 15.5,
                "volatility": 0.04,
                "support": 95000,
                "resistance": 98000
            },
            "ETHUSDT": {
                "price": 3480.0,
                "trend": {"1h": "neutral", "4h": "uptrend"},
                "volume_change_pct": 8.0,
                "volatility": 0.05
            }
        },
        "holdings": {
            "BTCUSDT": 0.0,
            "ETHUSDT": 0.0
        },
        "conditions": {
            "volatility": "normal",
            "news_alert": None
        }
    }
    
    recommendation_1 = engine.generate_recommendation(trading_plan_1, market_snapshot_1)
    print(f"Recommendation: {json.dumps(recommendation_1, indent=2)}")
    
    # Save to database
    rec_id_1 = engine.save_recommendation_to_db(
        recommendation_1,
        trading_plan_id="TEST_PLAN_001",
        market_snapshot=market_snapshot_1
    )
    print(f"Saved to DB with ID: {rec_id_1}")
    
    # Test Case 2: DCA Pullback - BUY near support
    print("\n[Test 2] DCA Pullback - Price Near Support")
    print("-" * 60)
    
    trading_plan_2 = {
        "strategy_type": "DCA pullback",
        "risk_mode": "low",
        "max_capital_per_trade": 500.0,
        "symbols": ["BTCUSDT"],
        "time_horizon": "swing"
    }
    
    market_snapshot_2 = {
        "symbols": {
            "BTCUSDT": {
                "price": 95200.0,  # Near support
                "trend": {"1h": "downtrend", "4h": "neutral"},
                "volume_change_pct": 5.0,
                "volatility": 0.03,
                "support": 95000,
                "resistance": 98000
            }
        },
        "holdings": {"BTCUSDT": 0.0},
        "conditions": {}
    }
    
    recommendation_2 = engine.generate_recommendation(trading_plan_2, market_snapshot_2)
    print(f"Recommendation: {json.dumps(recommendation_2, indent=2)}")
    
    rec_id_2 = engine.save_recommendation_to_db(
        recommendation_2,
        trading_plan_id="TEST_PLAN_002",
        market_snapshot=market_snapshot_2
    )
    print(f"Saved to DB with ID: {rec_id_2}")
    
    # Test Case 3: Market Blocker - NO_TRADE
    print("\n[Test 3] Market Blocker - High Volatility")
    print("-" * 60)
    
    trading_plan_3 = {
        "strategy_type": "trend following",
        "risk_mode": "normal",
        "max_capital_per_trade": 1000.0,
        "symbols": ["BTCUSDT"],
        "forbidden_conditions": {
            "high_volatility": True
        }
    }
    
    market_snapshot_3 = {
        "symbols": {
            "BTCUSDT": {
                "price": 96500.0,
                "trend": {"1h": "uptrend", "4h": "uptrend"},
                "volume_change_pct": 20.0,
                "volatility": 0.15
            }
        },
        "holdings": {"BTCUSDT": 0.0},
        "conditions": {
            "volatility": "extremely_high"
        }
    }
    
    recommendation_3 = engine.generate_recommendation(trading_plan_3, market_snapshot_3)
    print(f"Recommendation: {json.dumps(recommendation_3, indent=2)}")
    
    rec_id_3 = engine.save_recommendation_to_db(
        recommendation_3,
        trading_plan_id="TEST_PLAN_003",
        market_snapshot=market_snapshot_3
    )
    print(f"Saved to DB with ID: {rec_id_3}")
    
    # Test Case 4: Defensive - SELL on downtrend
    print("\n[Test 4] Defensive Strategy - SELL on Downtrend")
    print("-" * 60)
    
    trading_plan_4 = {
        "strategy_type": "defensive",
        "risk_mode": "low",
        "max_capital_per_trade": 1000.0,
        "symbols": ["BTCUSDT"]
    }
    
    market_snapshot_4 = {
        "symbols": {
            "BTCUSDT": {
                "price": 94000.0,
                "trend": {"1h": "downtrend", "4h": "downtrend"},
                "volume_change_pct": -10.0,
                "volatility": 0.05
            }
        },
        "holdings": {"BTCUSDT": 0.5},  # Has holdings
        "conditions": {}
    }
    
    recommendation_4 = engine.generate_recommendation(trading_plan_4, market_snapshot_4)
    print(f"Recommendation: {json.dumps(recommendation_4, indent=2)}")
    
    rec_id_4 = engine.save_recommendation_to_db(
        recommendation_4,
        trading_plan_id="TEST_PLAN_004",
        market_snapshot=market_snapshot_4
    )
    print(f"Saved to DB with ID: {rec_id_4}")
    
    # Verify data in database
    print("\n[Verification] Checking database records...")
    print("-" * 60)
    
    try:
        ssl_config = {'ssl': {'ca': None}} if util.DB_SSL_MODE == "Required" else None
        conn = pymysql.connect(
            host=util.DB_HOST,
            port=util.DB_PORT,
            user=util.DB_USER,
            password=util.DB_PASSWORD,
            database=util.DB_NAME,
            ssl=ssl_config,
            charset='utf8mb4',
            cursorclass=pymysql.cursors.DictCursor
        )
        
        with conn.cursor() as cursor:
            # Get all test recommendations
            cursor.execute("""
                SELECT RecommendationId, Decision, Symbol, AmountUsdt, Confidence, Status, CreatedAtUtc
                FROM ai_trading_recommendations
                WHERE TradingPlanId LIKE 'TEST_PLAN_%'
                ORDER BY CreatedAtUtc DESC
            """)
            records = cursor.fetchall()
            
            print(f"\nFound {len(records)} recommendations in database:")
            for i, rec in enumerate(records, 1):
                print(f"\n{i}. ID: {rec['RecommendationId']}")
                print(f"   Decision: {rec['Decision']}")
                print(f"   Symbol: {rec['Symbol']}")
                print(f"   Amount: ${rec['AmountUsdt']:.2f}")
                print(f"   Confidence: {rec['Confidence']:.2%}")
                print(f"   Status: {rec['Status']}")
                print(f"   Created: {rec['CreatedAtUtc']}")
        
        conn.close()
        
    except Exception as e:
        print(f"Error verifying database: {e}")
    
    # Close engine
    engine.close()
    
    print("\n" + "=" * 60)
    print("Test completed successfully!")
    print("=" * 60)

if __name__ == "__main__":
    test_recommendation_engine()

