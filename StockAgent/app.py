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
    price: Optional[float] = Field(default=None, ge=0, description="Current price if available")
    has_price: bool = Field(default=True, description="Flag indicating whether price is available")
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


class PortfolioPayload(BaseModel):
    """Lightweight portfolio snapshot passed from .NET backend"""
    total_value: Optional[float] = None
    total_cost: Optional[float] = None
    unrealized_pnl: Optional[float] = None
    unrealized_pnl_percent: Optional[float] = None
    available_usdt: Optional[float] = None
    holdings: Optional[List[Dict[str, Any]]] = None
    nav_history: Optional[List[Dict[str, Any]]] = None
    bot_positions: Optional[List[Dict[str, Any]]] = None

    class Config:
        extra = "allow"


class AiTradeSuggestion(BaseModel):
    decision: str = "NO_TRADE"
    symbol: str = ""
    amount_usdt: float = 0
    expected_return_pct: Optional[float] = None
    confidence: float = 0.0
    time_horizon: str = "intraday"


class AiChatRequest(BaseModel):
    user_id: int
    bot_id: Optional[str] = None
    trading_plan_id: str
    user_message: str = Field(..., min_length=1, max_length=2000)
    trading_plan: TradingPlan
    market_snapshot: MarketSnapshot
    portfolio: PortfolioPayload
    mode: str = Field(default="chat")
    context_summary: Optional[str] = None
    intent: str = Field(default="chat")
    market_highlights: Optional[List[Dict[str, Any]]] = None


class BotProposal(BaseModel):
    name: str
    symbols: List[str]
    strategy_type: str
    risk_mode: str
    max_capital_per_trade: float
    max_daily_exposure: float
    time_horizon: str
    expected_return_pct: Optional[float] = None
    risk_note: Optional[str] = None


class AiChatResponse(BaseModel):
    reply: str
    tradeSuggestion: Optional[AiTradeSuggestion] = None
    bots: List[BotProposal] = Field(default_factory=list)


# ============================================
# Helper Functions
# ============================================

def convert_market_snapshot_to_engine_format(snapshot: MarketSnapshot) -> Dict[str, Any]:
    """Convert MarketSnapshot model to engine format"""
    # Build symbols data
    price_value = snapshot.price or 0.0
    symbols_data = {
        snapshot.symbol: {
            "price": price_value,
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


def generate_bot_proposals(plan: TradingPlan) -> List[BotProposal]:
    symbols = plan.preferred_symbols or ["BTCUSDT"]
    proposals: List[BotProposal] = []

    for idx, symbol in enumerate(symbols[:3]):
        risk = (plan.risk_mode or "balanced").lower()
        if risk == "aggressive":
            strategy = "trend_following" if idx == 0 else "breakout"
            scale = 1.0
            note = "Rủi ro cao, có thể chịu drawdown lớn."
        elif risk == "safe":
            strategy = "dca"
            scale = 0.4
            note = "Ưu tiên bảo toàn vốn, vào lệnh nhỏ."
        else:
            strategy = "trend_following"
            scale = 0.7
            note = None

        proposals.append(
            BotProposal(
                name=f"{risk.title()} {symbol} {strategy.replace('_', ' ').title()}",
                symbols=[symbol],
                strategy_type=strategy,
                risk_mode=risk.upper(),
                max_capital_per_trade=plan.max_capital_per_trade * scale,
                max_daily_exposure=plan.max_daily_exposure * scale,
                time_horizon=plan.time_horizon,
                expected_return_pct=0.08 if strategy != "dca" else 0.04,
                risk_note=note,
            )
        )

    return proposals


def format_price(price: Optional[float]) -> str:
    if price is None or price <= 0:
        return ""
    return f"{price:,.2f}".replace(",", " ").replace(".", ",").replace(" ", ".")


def describe_price(snapshot: MarketSnapshot) -> str:
    if snapshot.has_price is False or not snapshot.price:
        return "hiện mình chưa có giá realtime chính xác"
    return f"khoảng {format_price(snapshot.price)} USDT"


def build_reply_text(
    plan: TradingPlan,
    bots: List[BotProposal],
    trade_suggestion: Optional[AiTradeSuggestion],
    user_message: str,
    snapshot: MarketSnapshot,
    mode: str,
    context_summary: Optional[str] = None,
    intent: Optional[str] = None,
    market_highlights: Optional[List[Dict[str, Any]]] = None
) -> str:
    intent = (intent or "chat").lower()
    if mode == "create_bot":
        return build_bot_reply_text(plan, bots, context_summary)
    if intent == "smalltalk":
        return build_smalltalk_reply(user_message)
    if intent == "direct_advice":
        return build_direct_intent_reply(plan, trade_suggestion, snapshot, user_message, market_highlights)
    return build_planning_reply(plan, trade_suggestion, snapshot, market_highlights)


def build_smalltalk_reply(user_message: str) -> str:
    cleaned = (user_message or "").strip()
    if not cleaned:
        return "Mình đang ở đây nè. Bạn muốn bàn về coin nào hay chia sẻ kế hoạch trading không?"
    return (
        f'Mình nghe bạn nói "{cleaned}". Mình luôn ở đây để trò chuyện về thị trường hoặc giúp bạn setup kế hoạch, '
        "cứ thoải mái nhé!"
    )


def build_direct_intent_reply(
    plan: TradingPlan,
    trade_suggestion: Optional[AiTradeSuggestion],
    snapshot: MarketSnapshot,
    user_message: str,
    market_highlights: Optional[List[Dict[str, Any]]]
) -> str:
    symbol = snapshot.symbol.upper()
    if trade_suggestion and trade_suggestion.decision != "NO_TRADE":
        action = "mua" if trade_suggestion.decision == "BUY" else "bán"
        return (
            f"Mình đang nghiêng về việc {action} {trade_suggestion.symbol} "
            f"khoảng {trade_suggestion.amount_usdt:.0f} USDT ({trade_suggestion.time_horizon}), "
            f"confidence ~{trade_suggestion.confidence:.0%}. Bạn có thể cân nhắc chia lệnh theo khẩu vị rủi ro của mình."
        )

    hints = build_highlight_text(user_message, market_highlights, symbol)
    price_phrase = describe_price(snapshot)
    trade_style = "lướt nhanh" if plan.time_horizon == "scalping" else "giữ swing ngắn"
    base_line = (
        f"{symbol} {price_phrase}. Nếu muốn {trade_style}, bạn có thể chia nhỏ vốn và theo dõi breakout/điểm hồi "
        "thay vì all-in một lần."
    )
    if hints:
        return base_line + "\n" + hints
    return base_line


def build_planning_reply(
    plan: TradingPlan,
    trade_suggestion: Optional[AiTradeSuggestion],
    snapshot: MarketSnapshot,
    market_highlights: Optional[List[Dict[str, Any]]]
) -> str:
    symbol = snapshot.symbol.upper()
    price_phrase = describe_price(snapshot)
    lines: List[str] = [
        f"{symbol} {price_phrase}. Mình sẽ tiếp tục quan sát để báo lại khi tín hiệu rõ hơn."
    ]

    if trade_suggestion and trade_suggestion.decision != "NO_TRADE":
        lines.append(
            f"Tín hiệu hiện tại: {trade_suggestion.decision} {trade_suggestion.symbol} "
            f"~{trade_suggestion.amount_usdt:.0f} USDT (confidence {trade_suggestion.confidence:.0%})."
        )
    else:
        lines.append("Chưa có lệnh nào thật sự chắc chắn, mình sẽ ping ngay khi điều kiện đẹp hơn.")

    hints = build_highlight_text("", market_highlights, symbol)
    if hints:
        lines.append(hints)

    return "\n".join(lines)


def build_bot_reply_text(
    plan: TradingPlan,
    bots: List[BotProposal],
    context_summary: Optional[str]
) -> str:
    risk_label = (plan.risk_mode or "balanced").lower()
    risk_map = {
        "aggressive": "mạo hiểm",
        "balanced": "cân bằng",
        "safe": "an toàn",
        "defensive": "phòng thủ"
    }
    friendly_risk = risk_map.get(risk_label, risk_label)

    lines: List[str] = []
    if context_summary:
        lines.append(context_summary)
    else:
        lines.append(
            "Mình sẽ dựng bot theo những gì bạn đã mô tả. Nếu muốn tinh chỉnh thêm các tham số, cứ nói nhé!"
        )

    if not bots:
        lines.append("Chưa đủ dữ liệu để tạo bot cụ thể, bạn cho mình biết thêm vốn, risk hoặc timeframe nhé.")
        return "\n".join(lines)

    lines.append(
        f"Cấu hình {friendly_risk} với hạn mức ~{plan.max_capital_per_trade:.0f} USDT/lệnh, "
        f"mình đề xuất các bot sau:"
    )

    for idx, bot in enumerate(bots, start=1):
        bot_line = (
            f"- Bot {idx}: {bot.name} ({', '.join(bot.symbols)}) • {bot.strategy_type.replace('_', ' ')} • "
            f"{bot.max_capital_per_trade:.0f} USDT/lệnh, tối đa ngày ~{bot.max_daily_exposure:.0f}."
        )
        lines.append(bot_line)
        if bot.risk_note:
            lines.append(f"  Ghi chú: {bot.risk_note}")

    lines.append("Nếu muốn mình tinh chỉnh thêm hoặc tạo bot khác cứ nói nhé!")
    return "\n".join(lines)


def build_highlight_text(user_message: str, highlights: Optional[List[Dict[str, Any]]], primary_symbol: str) -> str:
    if not highlights:
        return ""

    msg_lower = (user_message or "").lower()
    trigger_words = ["ngoai", "khac", "co coin", "co cai", "co dong", "coin nao", "mua nao"]
    ask_for_alt = any(word in msg_lower for word in trigger_words)

    if not ask_for_alt:
        return ""

    if "ngoai" in msg_lower or ("coin" in msg_lower and "khac" in msg_lower):
        secondary = [h for h in highlights if h.get("symbol", "").upper() != primary_symbol.upper()]
    else:
        secondary = highlights

    if not secondary:
        secondary = highlights

    parts = []
    seen = set()
    for item in secondary:
        symbol = item.get("symbol", "")
        if symbol in seen:
            continue
        seen.add(symbol)
        change = item.get("change_24h")
        if change is not None:
            parts.append(f"{symbol} (~{change:+.1f}%/24h)")
        else:
            parts.append(symbol)
        if len(parts) == 2:
            break

    if not parts:
        return ""

    return f"Ngoài ra bạn có thể theo dõi thêm: {', '.join(parts)}."


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


@app.post("/ai/chat", response_model=AiChatResponse, status_code=status.HTTP_200_OK)
async def ai_chat(request: AiChatRequest):
    """
    Chat-style endpoint used by .NET backend.
    It reuses the recommendation engine but packages the response with a natural language reply
    and JSON trade suggestion.
    """
    try:
        log.logger.info(
            "AI chat request from user_id=%s bot_id=%s message=%s",
            request.user_id,
            request.bot_id,
            request.user_message[:200]
        )

        trading_plan_dict = request.trading_plan.model_dump()
        market_snapshot_dict = convert_market_snapshot_to_engine_format(request.market_snapshot)

        recommendation = engine.generate_recommendation(
            trading_plan=trading_plan_dict,
            market_snapshot=market_snapshot_dict
        )

        decision = recommendation.get("decision", "NO_TRADE")
        symbol = recommendation.get("symbol", request.market_snapshot.symbol)
        amount_usdt = float(recommendation.get("amount_usdt", 0))
        confidence = float(recommendation.get("confidence", 0))
        time_horizon = recommendation.get("time_horizon", request.trading_plan.time_horizon)
        expected_return = recommendation.get("expected_return_pct")

        trade_suggestion = None
        if decision != "NO_TRADE":
            trade_suggestion = AiTradeSuggestion(
                decision=decision,
                symbol=symbol,
                amount_usdt=amount_usdt,
                expected_return_pct=expected_return,
                confidence=confidence,
                time_horizon=time_horizon
            )

        bots = generate_bot_proposals(request.trading_plan) if request.mode == "create_bot" else []
        reply = build_reply_text(
            request.trading_plan,
            bots,
            trade_suggestion,
            request.user_message,
            request.market_snapshot,
            mode=request.mode,
            context_summary=request.context_summary,
            intent=request.intent,
            market_highlights=request.market_highlights
        )

        return AiChatResponse(reply=reply, tradeSuggestion=trade_suggestion, bots=bots)

    except Exception as exc:
        log.logger.error("AI chat failed: %s", exc, exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="AI chat service failed to process the request."
        )


@app.get("/")
async def root():
    """Root endpoint"""
    return {
        "service": "AI Trading Recommendation Service",
        "version": "1.0.0",
        "endpoints": {
            "health": "/health",
            "recommendations": "/ai/recommendations",
            "chat": "/ai/chat"
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

