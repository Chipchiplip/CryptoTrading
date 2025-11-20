"""
FastAPI REST API for LiveRecommendationEngine
Microservice for AI trading recommendations
"""

from fastapi import FastAPI, HTTPException, status
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field
from typing import Optional, Dict, Any, List
import uvicorn
from live_recommendation_engine import LiveRecommendationEngine
from log.custom_logger import log

# Initialize FastAPI app
app = FastAPI(
    title="AI Trading Recommendation Service",
    description="Microservice for generating crypto spot trading recommendations",
    version="1.0.0"
)

# CORS middleware for .NET backend
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Configure properly in production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Initialize engine (singleton)
engine = LiveRecommendationEngine()


# ============================================
# Pydantic Models
# ============================================

class TradingPlan(BaseModel):
    """Trading plan configuration"""
    preferred_symbols: List[str] = Field(default=["BTCUSDT", "ETHUSDT"], description="Allowed trading symbols")
    strategy_type: str = Field(..., description="Strategy: trend following, DCA pullback, breakout, defensive")
    risk_mode: str = Field(default="normal", description="Risk mode: low, normal, high")
    max_capital_per_trade: float = Field(..., gt=0, description="Maximum capital per trade in USDT")
    max_daily_exposure: float = Field(default=5000.0, gt=0, description="Maximum daily exposure in USDT")
    time_horizon: str = Field(default="intraday", description="Time horizon: scalping, intraday, swing")
    entry_conditions: Optional[Dict[str, Any]] = Field(default=None, description="Entry filter conditions")
    exit_conditions: Optional[Dict[str, Any]] = Field(default=None, description="Exit rules")
    restrictions: Optional[Dict[str, Any]] = Field(default=None, description="Forbidden conditions")
    additional_preferences: Optional[Dict[str, Any]] = Field(default=None, description="Additional preferences")
    min_confidence: float = Field(default=0.6, ge=0.0, le=1.0, description="Minimum confidence threshold")


class MarketSnapshot(BaseModel):
    """Market snapshot data"""
    symbol: str = Field(..., description="Primary symbol to analyze")
    price: float = Field(..., gt=0, description="Current price")
    trend_1h: Optional[str] = Field(default=None, description="1h trend: uptrend, downtrend, neutral")
    trend_4h: Optional[str] = Field(default=None, description="4h trend: uptrend, downtrend, neutral")
    ma20_vs_ma50: Optional[str] = Field(default=None, description="MA20 vs MA50 comparison")
    volume_vs_ma: Optional[float] = Field(default=None, description="Volume change vs moving average (%)")
    volatility: Optional[float] = Field(default=None, ge=0, description="Current volatility")
    support: Optional[float] = Field(default=None, gt=0, description="Support level")
    resistance: Optional[float] = Field(default=None, gt=0, description="Resistance level")
    usdt_balance: Optional[float] = Field(default=0.0, ge=0, description="Available USDT balance")
    btc_holding: Optional[float] = Field(default=0.0, ge=0, description="BTC holdings")
    eth_holding: Optional[float] = Field(default=0.0, ge=0, description="ETH holdings")
    has_bad_news: Optional[bool] = Field(default=False, description="Bad news flag")
    meta: Optional[Dict[str, Any]] = Field(default=None, description="Additional metadata")


class RecommendationRequest(BaseModel):
    """Request for AI recommendation"""
    user_id: int = Field(..., description="User ID")
    bot_id: Optional[int] = Field(default=None, description="Bot ID if applicable")
    trading_plan_id: str = Field(..., description="Trading plan identifier")
    trading_plan: TradingPlan = Field(..., description="Trading plan configuration")
    market_snapshot: MarketSnapshot = Field(..., description="Current market snapshot")


class RecommendationResponse(BaseModel):
    """AI recommendation response"""
    id: str = Field(..., description="Recommendation ID")
    decision: str = Field(..., description="Decision: NO_TRADE, BUY, SELL")
    symbol: str = Field(..., description="Trading symbol")
    amount_usdt: float = Field(..., ge=0, description="Amount in USDT")
    reason: str = Field(..., description="Reason for recommendation")
    confidence: float = Field(..., ge=0.0, le=1.0, description="Confidence score 0-1")
    time_horizon: str = Field(..., description="Time horizon: scalping, intraday, swing")


class HealthResponse(BaseModel):
    """Health check response"""
    status: str = "ok"
    service: str = "ai-recommendation-engine"


# ============================================
# Helper Functions
# ============================================

def convert_market_snapshot_to_engine_format(snapshot: MarketSnapshot) -> Dict[str, Any]:
    """Convert MarketSnapshot model to engine format"""
    # Build symbols data
    symbols_data = {
        snapshot.symbol: {
            "price": snapshot.price,
            "trend": {
                "1h": snapshot.trend_1h or "neutral",
                "4h": snapshot.trend_4h or "neutral"
            },
            "volume_change_pct": snapshot.volume_vs_ma or 0.0,
            "volatility": snapshot.volatility or 0.0,
            "support": snapshot.support,
            "resistance": snapshot.resistance
        }
    }
    
    # Add other symbols if mentioned
    if snapshot.symbol == "BTCUSDT" and snapshot.eth_holding is not None:
        # Could add ETHUSDT data if available
        pass
    
    # Build holdings
    holdings = {}
    if snapshot.symbol == "BTCUSDT":
        holdings["BTCUSDT"] = snapshot.btc_holding or 0.0
        if snapshot.eth_holding is not None:
            holdings["ETHUSDT"] = snapshot.eth_holding
    elif snapshot.symbol == "ETHUSDT":
        holdings["ETHUSDT"] = snapshot.eth_holding or 0.0
        if snapshot.btc_holding is not None:
            holdings["BTCUSDT"] = snapshot.btc_holding
    
    # Build conditions
    conditions = {
        "volatility": "extremely_high" if snapshot.volatility and snapshot.volatility > 0.1 else "normal",
        "news_alert": "fud" if snapshot.has_bad_news else None
    }
    
    return {
        "symbols": symbols_data,
        "holdings": holdings,
        "conditions": conditions,
        "meta": snapshot.meta or {}
    }


# ============================================
# API Endpoints
# ============================================

@app.get("/health", response_model=HealthResponse)
async def health_check():
    """Health check endpoint"""
    return HealthResponse(status="ok", service="ai-recommendation-engine")


@app.post("/ai/recommendations", response_model=RecommendationResponse, status_code=status.HTTP_200_OK)
async def create_recommendation(request: RecommendationRequest):
    """
    Generate AI trading recommendation based on trading plan and market snapshot.
    
    Returns a JSON recommendation with decision, symbol, amount, reason, confidence.
    """
    try:
        log.logger.info(f"Received recommendation request: user_id={request.user_id}, bot_id={request.bot_id}, plan_id={request.trading_plan_id}")
        
        # Convert trading plan to engine format
        trading_plan_dict = request.trading_plan.model_dump()
        
        # Convert market snapshot to engine format
        market_snapshot_dict = convert_market_snapshot_to_engine_format(request.market_snapshot)
        
        # Generate recommendation
        recommendation = engine.generate_recommendation(
            trading_plan=trading_plan_dict,
            market_snapshot=market_snapshot_dict
        )
        
        log.logger.info(f"Generated recommendation: {recommendation['decision']} {recommendation['symbol']} ${recommendation['amount_usdt']:.2f} (confidence: {recommendation['confidence']:.2%})")
        
        # Save to database
        rec_id = engine.save_recommendation_to_db(
            recommendation=recommendation,
            trading_plan_id=request.trading_plan_id,
            market_snapshot=market_snapshot_dict,
            user_id=request.user_id,
            bot_id=request.bot_id
        )
        
        if not rec_id:
            log.logger.warning("Failed to save recommendation to database, but continuing with response")
            rec_id = "TEMP_" + recommendation.get("symbol", "UNKNOWN")
        
        # Add ID to response
        recommendation["id"] = rec_id
        
        return RecommendationResponse(**recommendation)
        
    except Exception as e:
        log.logger.error(f"Error generating recommendation: {e}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to generate recommendation: {str(e)}"
        )


@app.get("/")
async def root():
    """Root endpoint"""
    return {
        "service": "AI Trading Recommendation Service",
        "version": "1.0.0",
        "endpoints": {
            "health": "/health",
            "recommendations": "/ai/recommendations"
        }
    }


# ============================================
# Startup/Shutdown
# ============================================

@app.on_event("startup")
async def startup_event():
    """Initialize on startup"""
    log.logger.info("AI Recommendation Service starting up...")


@app.on_event("shutdown")
async def shutdown_event():
    """Cleanup on shutdown"""
    log.logger.info("AI Recommendation Service shutting down...")
    engine.close()


# ============================================
# Main Entry Point
# ============================================

if __name__ == "__main__":
    import os
    # Only watch specific directories to avoid reloading on test file changes
    reload_dirs = [os.path.dirname(os.path.abspath(__file__))]
    
    uvicorn.run(
        "app:app",
        host="0.0.0.0",
        port=8000,
        reload=True,  # Disable in production
        reload_dirs=reload_dirs,  # Only watch current directory
        log_level="info"
    )

