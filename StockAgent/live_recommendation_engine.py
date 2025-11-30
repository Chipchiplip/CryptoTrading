"""
Live Crypto Spot Trading Recommendation Engine
Production-ready AI recommendation system for spot trading.
DO NOT modify legacy database tables - only uses new auxiliary tables.
"""

import json
import uuid
from datetime import datetime
from typing import Dict, Optional, Any
import pymysql
import util
from log.custom_logger import log


class LiveRecommendationEngine:
    """
    Live trading recommendation engine.
    Takes trading plan + market snapshot → returns JSON recommendation.
    """
    
    def __init__(self):
        self.db_connection = None
    
    def get_db_connection(self):
        """Get database connection for auxiliary tables only."""
        if self.db_connection is None or not self.db_connection.open:
            try:
                ssl_config = {'ssl': {'ca': None}} if util.DB_SSL_MODE == "Required" else None
                self.db_connection = pymysql.connect(
                    host=util.DB_HOST,
                    port=util.DB_PORT,
                    user=util.DB_USER,
                    password=util.DB_PASSWORD,
                    database=util.DB_NAME,
                    ssl=ssl_config,
                    charset='utf8mb4',
                    cursorclass=pymysql.cursors.DictCursor
                )
            except Exception as e:
                log.logger.error(f"Error connecting to database: {e}")
                return None
        return self.db_connection
    
    def generate_recommendation(
        self,
        trading_plan: Dict[str, Any],
        market_snapshot: Dict[str, Any]
    ) -> Dict[str, Any]:
        """
        Generate trading recommendation based on plan and snapshot.
        
        Returns JSON:
        {
            "decision": "NO_TRADE" | "BUY" | "SELL",
            "symbol": "BTCUSD" | "ETHUSD",
            "amount_usdt": number,
            "reason": "string",
            "confidence": number,
            "time_horizon": "scalping" | "intraday" | "swing"
        }
        """
        # Extract plan constraints
        strategy_type = trading_plan.get("strategy_type", "trend following")
        risk_mode = trading_plan.get("risk_mode", "normal")
        max_capital_per_trade = trading_plan.get("max_capital_per_trade", 1000.0)
        max_daily_exposure = trading_plan.get("max_daily_exposure", 5000.0)
        # Support both "symbols" and "preferred_symbols" keys
        allowed_symbols = trading_plan.get("preferred_symbols") or trading_plan.get("symbols", ["BTCUSD", "ETHUSD"])
        time_horizon = trading_plan.get("time_horizon", "intraday")
        
        # Extract market data
        symbols_data = market_snapshot.get("symbols", {})
        holdings = market_snapshot.get("holdings", {})
        market_conditions = market_snapshot.get("conditions", {})
        
        log.logger.info(f"Trading plan: strategy={strategy_type}, symbols={allowed_symbols}, max_capital={max_capital_per_trade}")
        log.logger.info(f"Market snapshot: symbols_data keys={list(symbols_data.keys())}, holdings={holdings}")
        
        # Risk checks - return NO_TRADE if blocked
        if self._check_market_blockers(market_snapshot, trading_plan):
            log.logger.warning("Market conditions blocked by trading plan")
            return self._create_no_trade_response("Market conditions blocked by trading plan")
        
        # Analyze each allowed symbol
        best_recommendation = None
        best_confidence = 0.0
        
        for symbol in allowed_symbols:
            if symbol not in symbols_data:
                log.logger.warning(f"Symbol {symbol} not found in symbols_data")
                continue
            
            symbol_data = symbols_data[symbol]
            log.logger.info(f"Analyzing {symbol}: price={symbol_data.get('price')}, trend={symbol_data.get('trend')}, volume_change={symbol_data.get('volume_change_pct')}")
            
            recommendation = self._analyze_symbol(
                symbol,
                symbol_data,
                trading_plan,
                holdings.get(symbol, 0),
                max_capital_per_trade
            )
            
            if recommendation:
                log.logger.info(f"Recommendation for {symbol}: {recommendation['decision']} confidence={recommendation['confidence']:.2f}")
                if recommendation["confidence"] > best_confidence:
                    best_confidence = recommendation["confidence"]
                    best_recommendation = recommendation
            else:
                log.logger.info(f"No recommendation for {symbol} with current market conditions")
        
        # Return best recommendation or NO_TRADE
        min_confidence = trading_plan.get("min_confidence", 0.6)
        # Allow slightly lower confidence if we have a recommendation
        if best_recommendation and best_confidence >= max(0.5, min_confidence - 0.1):
            best_recommendation["time_horizon"] = time_horizon
            return best_recommendation
        
        # Log why no recommendation
        if best_recommendation:
            log.logger.info(f"Recommendation rejected: confidence {best_confidence:.2f} < threshold {min_confidence:.2f}")
        else:
            log.logger.info("No recommendation generated: no signals found for any symbol")
        
        return self._create_no_trade_response("No strong signals found")
    
    def _check_market_blockers(self, market_snapshot: Dict, trading_plan: Dict) -> bool:
        """Check if market conditions block trading."""
        conditions = market_snapshot.get("conditions", {})
        
        # High volatility check
        if conditions.get("volatility") == "extremely_high":
            forbidden_volatility = trading_plan.get("forbidden_conditions", {}).get("high_volatility", False)
            if forbidden_volatility:
                return True
        
        # Bad news check
        if conditions.get("news_alert") in ["hack", "fud", "depeg"]:
            return True
        
        # Time restriction check
        time_restrictions = trading_plan.get("forbidden_conditions", {}).get("time_restrictions", [])
        current_hour = datetime.utcnow().hour
        if current_hour in time_restrictions:
            return True
        
        return False
    
    def _analyze_symbol(
        self,
        symbol: str,
        symbol_data: Dict,
        trading_plan: Dict,
        current_holdings: float,
        max_capital: float
    ) -> Optional[Dict]:
        """Analyze a single symbol and return recommendation."""
        strategy_type = trading_plan.get("strategy_type", "trend following")
        risk_mode = trading_plan.get("risk_mode", "normal")
        
        price = symbol_data.get("price", 0)
        trend = symbol_data.get("trend", {})
        volume_change = symbol_data.get("volume_change_pct", 0)
        volatility = symbol_data.get("volatility", 0)
        support = symbol_data.get("support")
        resistance = symbol_data.get("resistance")
        
        # Strategy-specific analysis
        if strategy_type == "trend following":
            return self._trend_following_analysis(
                symbol, price, trend, volume_change, volatility,
                current_holdings, max_capital, risk_mode, trading_plan,
                support, resistance
            )
        elif strategy_type == "DCA pullback":
            return self._dca_pullback_analysis(
                symbol, price, symbol_data, current_holdings, max_capital, risk_mode, trading_plan
            )
        elif strategy_type == "breakout":
            return self._breakout_analysis(
                symbol, price, symbol_data, current_holdings, max_capital, risk_mode, trading_plan
            )
        elif strategy_type == "defensive":
            return self._defensive_analysis(
                symbol, price, trend, current_holdings, max_capital, risk_mode, trading_plan
            )
        
        return None
    
    def _trend_following_analysis(
        self, symbol, price, trend, volume_change, volatility,
        holdings, max_capital, risk_mode, plan, support=None, resistance=None
    ) -> Optional[Dict]:
        """Trend following strategy analysis."""
        trend_1h = trend.get("1h", "neutral")
        trend_4h = trend.get("4h", "neutral")
        
        # Strong uptrend signal - relaxed conditions (volume optional)
        if trend_1h == "uptrend" and trend_4h == "uptrend":
            # Accept if volume is positive OR if volume data is not available (0 or None)
            if volume_change > 5 or volume_change == 0 or volume_change is None:
                if holdings == 0:  # No position, can buy
                    amount = self._calculate_position_size(max_capital, price, risk_mode)
                    # Calculate confidence based on volume and trend strength
                    base_confidence = 0.65 if volume_change > 5 else 0.6  # Slightly lower if no volume data
                    volume_boost = min(0.2, (volume_change / 100) * 0.3) if volume_change and volume_change > 0 else 0
                    confidence = min(0.85, base_confidence + volume_boost)
                    reason = f"Strong uptrend on 1h/4h"
                    if volume_change and volume_change > 0:
                        reason += f", volume +{volume_change:.1f}%"
                    return {
                        "decision": "BUY",
                        "symbol": symbol,
                        "amount_usdt": amount,
                        "reason": reason,
                        "confidence": confidence
                    }
        
        # Moderate uptrend on at least one timeframe (volume optional)
        if trend_1h == "uptrend" or trend_4h == "uptrend":
            # Accept if volume is positive OR if no volume data
            if volume_change > 10 or (volume_change == 0 or volume_change is None):
                if holdings == 0:
                    amount = self._calculate_position_size(max_capital * 0.5, price, risk_mode)  # Smaller position
                    confidence = 0.55 if volume_change == 0 or volume_change is None else 0.6  # Lower if no volume
                    reason = f"Moderate uptrend on {'1h' if trend_1h == 'uptrend' else '4h'}"
                    if volume_change and volume_change > 0:
                        reason += f", volume +{volume_change:.1f}%"
                    return {
                        "decision": "BUY",
                        "symbol": symbol,
                        "amount_usdt": amount,
                        "reason": reason,
                        "confidence": confidence
                    }
        
        # Strong downtrend - sell if holding
        if trend_1h == "downtrend" and trend_4h == "downtrend" and holdings > 0:
            confidence = 0.75
            return {
                "decision": "SELL",
                "symbol": symbol,
                "amount_usdt": holdings * price * 0.5,  # Sell 50% of holdings
                "reason": "Strong downtrend on 1h/4h, reducing exposure",
                "confidence": confidence
            }
        
        # Moderate downtrend - partial sell
        if (trend_1h == "downtrend" or trend_4h == "downtrend") and holdings > 0:
            confidence = 0.6
            return {
                "decision": "SELL",
                "symbol": symbol,
                "amount_usdt": holdings * price * 0.3,  # Sell 30% of holdings
                "reason": "Moderate downtrend detected, reducing exposure",
                "confidence": confidence
            }
        
        # Fallback: If trend is neutral but price is reasonable and no holdings, suggest small buy
        if trend_1h == "neutral" and trend_4h == "neutral" and holdings == 0:
            # Only suggest if volatility is reasonable and we have capital
            if volatility < 0.1 and max_capital > 100:  # Low volatility, reasonable capital
                amount = self._calculate_position_size(max_capital * 0.3, price, risk_mode)  # Very small position
                confidence = 0.5  # Low confidence
                return {
                    "decision": "BUY",
                    "symbol": symbol,
                    "amount_usdt": amount,
                    "reason": "Neutral market, small position entry",
                    "confidence": confidence
                }
        
        # Neutral trend with holdings: suggest DCA buy if conditions are favorable
        if trend_1h == "neutral" and trend_4h == "neutral" and holdings > 0:
            # If price is near support and we have capital, suggest small DCA buy
            if support and support > 0 and price <= support * 1.02 and max_capital > 100:
                amount = self._calculate_position_size(max_capital * 0.2, price, risk_mode)  # Small DCA amount
                confidence = 0.52
                return {
                    "decision": "BUY",
                    "symbol": symbol,
                    "amount_usdt": amount,
                    "reason": f"Neutral trend, DCA near support {support:.0f}",
                    "confidence": confidence
                }
            
            # If price is reasonable and volatility is low, suggest small DCA buy
            if volatility < 0.05 and max_capital > 100:
                amount = self._calculate_position_size(max_capital * 0.15, price, risk_mode)  # Very small DCA
                confidence = 0.51
                return {
                    "decision": "BUY",
                    "symbol": symbol,
                    "amount_usdt": amount,
                    "reason": "Neutral trend, low volatility, small DCA entry",
                    "confidence": confidence
                }
            
            # Fallback: If we have holdings and neutral trend, suggest small DCA buy anyway (very conservative)
            if max_capital > 100:
                amount = self._calculate_position_size(max_capital * 0.1, price, risk_mode)  # Very small DCA
                confidence = 0.5  # Minimum confidence
                return {
                    "decision": "BUY",
                    "symbol": symbol,
                    "amount_usdt": amount,
                    "reason": "Neutral trend with existing position, small DCA entry",
                    "confidence": confidence
                }
        
        return None
    
    def _dca_pullback_analysis(
        self, symbol, price, symbol_data, holdings, max_capital, risk_mode, plan
    ) -> Optional[Dict]:
        """DCA pullback strategy analysis."""
        support = symbol_data.get("support", 0)
        resistance = symbol_data.get("resistance", 0)
        
        # Buy on pullback to support
        if support > 0 and price <= support * 1.02:  # Within 2% of support
            if holdings == 0 or holdings < max_capital / price * 0.5:  # Can add more
                amount = self._calculate_position_size(max_capital * 0.3, price, risk_mode)  # Smaller DCA size
                confidence = 0.7
                return {
                    "decision": "BUY",
                    "symbol": symbol,
                    "amount_usdt": amount,
                    "reason": f"Price near support level {support:.2f}, DCA entry",
                    "confidence": confidence
                }
        
        return None
    
    def _breakout_analysis(
        self, symbol, price, symbol_data, holdings, max_capital, risk_mode, plan
    ) -> Optional[Dict]:
        """Breakout strategy analysis."""
        resistance = symbol_data.get("resistance", 0)
        volume_change = symbol_data.get("volume_change_pct", 0)
        
        # Breakout above resistance with volume
        if resistance > 0 and price > resistance * 0.998 and volume_change > 15:
            if holdings == 0:
                amount = self._calculate_position_size(max_capital, price, risk_mode)
                confidence = 0.75
                return {
                    "decision": "BUY",
                    "symbol": symbol,
                    "amount_usdt": amount,
                    "reason": f"Breakout above resistance {resistance:.2f} with volume surge",
                    "confidence": confidence
                }
        
        return None
    
    def _defensive_analysis(
        self, symbol, price, trend, holdings, max_capital, risk_mode, plan
    ) -> Optional[Dict]:
        """Defensive strategy - prioritize capital preservation."""
        trend_1h = trend.get("1h", "neutral")
        
        # Only sell if strong downtrend
        if trend_1h == "downtrend" and holdings > 0:
            confidence = 0.8
            return {
                "decision": "SELL",
                "symbol": symbol,
                "amount_usdt": holdings * price,  # Sell all
                "reason": "Defensive mode: exiting on downtrend",
                "confidence": confidence
            }
        
        # Very conservative - rarely buy
        return None
    
    def _calculate_position_size(self, max_capital: float, price: float, risk_mode: str) -> float:
        """Calculate position size based on risk mode."""
        if risk_mode == "low":
            return max_capital * 0.3
        elif risk_mode == "high":
            return max_capital * 0.9
        else:  # normal
            return max_capital * 0.6
    
    def _create_no_trade_response(self, reason: str) -> Dict[str, Any]:
        """Create NO_TRADE response."""
        return {
            "decision": "NO_TRADE",
            "symbol": "BTCUSD",
            "amount_usdt": 0,
            "reason": reason,
            "confidence": 0.0,
            "time_horizon": "intraday"
        }
    
    def save_recommendation_to_db(
        self,
        recommendation: Dict[str, Any],
        trading_plan_id: Optional[str] = None,
        market_snapshot: Optional[Dict] = None,
        user_id: Optional[int] = None,
        bot_id: Optional[int] = None
    ) -> Optional[str]:
        """
        Save recommendation to auxiliary database table.
        Returns recommendation_id if successful.
        """
        conn = self.get_db_connection()
        if not conn:
            return None
        
        try:
            recommendation_id = f"REC_{uuid.uuid4().hex[:16]}"
            
            with conn.cursor() as cursor:
                # Check if table has UserId/BotId columns (for future extension)
                # For now, store in MarketSnapshotJson metadata
                snapshot_with_metadata = market_snapshot.copy() if market_snapshot else {}
                if user_id is not None:
                    snapshot_with_metadata["_metadata"] = snapshot_with_metadata.get("_metadata", {})
                    snapshot_with_metadata["_metadata"]["user_id"] = user_id
                if bot_id is not None:
                    snapshot_with_metadata["_metadata"] = snapshot_with_metadata.get("_metadata", {})
                    snapshot_with_metadata["_metadata"]["bot_id"] = bot_id
                
                sql = """
                    INSERT INTO ai_trading_recommendations 
                    (RecommendationId, TradingPlanId, MarketSnapshotJson,
                     Decision, Symbol, AmountUsdt, Reason, Confidence, TimeHorizon, Status)
                    VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
                """
                cursor.execute(sql, (
                    recommendation_id,
                    trading_plan_id,
                    json.dumps(snapshot_with_metadata) if snapshot_with_metadata else None,
                    recommendation["decision"],
                    recommendation["symbol"],
                    recommendation["amount_usdt"],
                    recommendation["reason"],
                    recommendation["confidence"],
                    recommendation.get("time_horizon", "intraday"),
                    "pending"
                ))
                conn.commit()
                log.logger.info(f"Saved recommendation {recommendation_id} to database")
                return recommendation_id
        except Exception as e:
            log.logger.error(f"Error saving recommendation to database: {e}")
            conn.rollback()
            return None
    
    def close(self):
        """Close database connection."""
        if self.db_connection and self.db_connection.open:
            self.db_connection.close()


def generate_live_recommendation(trading_plan: Dict, market_snapshot: Dict) -> Dict[str, Any]:
    """
    Standalone function to generate recommendation.
    Returns JSON recommendation only - no database writes.
    """
    engine = LiveRecommendationEngine()
    try:
        recommendation = engine.generate_recommendation(trading_plan, market_snapshot)
        return recommendation
    finally:
        engine.close()

