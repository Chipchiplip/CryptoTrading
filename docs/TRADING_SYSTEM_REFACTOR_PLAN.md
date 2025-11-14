# Trading System Refactor & Fix Implementation Plan

## Executive Summary

This document outlines the comprehensive refactoring plan to fix all logic flaws, stabilize runtime behavior, and align backtest with realtime execution for the CryptoTrading system.

## Current State Analysis

### Existing Components
- ✅ Grid Trading Strategy - Fully functional
- ✅ Momentum Scalping Strategy - 95% complete, needs real order placement
- ✅ Aggressive Forex Strategy - Fully functional
- ❌ DCA Strategy - Missing
- ✅ TradingService - Fully implemented with order matching
- ⚠️ MarketDataProvider - Uses CoinGecko (not exchange data)
- ⚠️ RiskManager - Basic limits, no kill switch
- ⚠️ PortfolioService - Basic implementation
- ✅ BackgroundService - Worker pool architecture

### Critical Issues Identified

1. **Market Data Issues**
   - Uses CoinGecko aggregate prices instead of exchange-specific data
   - No bid/ask spread information
   - No order book depth
   - Potential for stale/delayed data

2. **Risk Management Gaps**
   - Hard-coded $100k capital limit
   - No kill switch implementation
   - No consecutive loss detection
   - No drawdown monitoring
   - No circuit breaker

3. **Missing Features**
   - DCA strategy not implemented
   - No rate limiting for Grid bot
   - No trailing TP/SL
   - Incomplete order cancellation on bot stop

4. **Backtest Alignment**
   - Needs realistic order lifecycle simulation
   - Must include fees, slippage, latency
   - Should not assume instant fills

## Target Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    BotExecutionHostedService                │
│                   (Orchestrator + Workers)                  │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                    ITradingStrategy                         │
│         (Grid / DCA / Trend / Momentum / Forex)            │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                      IRiskManager                           │
│  - Kill Switch (PnL + Consecutive Loss)                    │
│  - Position Sizing (Real Balance)                          │
│  - Daily Loss Limits                                        │
│  - Cooldown Management                                      │
│  - Rate Limiting                                            │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                    ITradingService                          │
│  - Order Placement (Idempotency)                           │
│  - Order Cancellation                                       │
│  - Balance Management                                       │
│  - Audit Trail                                              │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                  IMarketDataProvider                        │
│  - Exchange Mark Price                                      │
│  - Order Book Mid Price                                     │
│  - Price Validation                                         │
│  - Stale Data Detection                                     │
│  - Volume Data                                              │
└─────────────────────────────────────────────────────────────┘
```

### New Components to Create

1. **IPriceValidator** - Validates price data quality
2. **IExchangeDataProvider** - Real exchange API integration
3. **IKillSwitchService** - Monitors and triggers circuit breakers
4. **IPositionTracker** - Tracks realtime PnL and positions
5. **DCAStrategy** - Implements Dollar Cost Averaging
6. **ImprovedBacktestEngine** - Realistic simulation

## Implementation Plan

### Phase 1: Core Infrastructure (Priority: CRITICAL)

#### 1.1 Create IPriceValidator Service
**File**: `Services/Market/PriceValidator.cs`

**Features**:
- Detect price deviation > configurable threshold (e.g., 5%)
- Filter outlier candles
- Stale tick detection (timestamp check)
- Volume validation

**Dependencies**: None (standalone utility)

#### 1.2 Create IExchangeDataProvider Interface
**File**: `Services/Market/IExchangeDataProvider.cs`

**Methods**:
- `GetMarkPriceAsync(string symbol)`
- `GetOrderBookMidPriceAsync(string symbol)`
- `GetOrderBookAsync(string symbol, int depth)`
- `GetTickerAsync(string symbol)`

**Implementations**:
- `BinanceDataProvider` - For production
- `CoinGeckoDataProvider` - Fallback/development
- `MockExchangeDataProvider` - For testing

#### 1.3 Refactor MarketDataProvider
**File**: `Services/Bot/MarketDataProvider.cs`

**Changes**:
- Inject `IExchangeDataProvider` instead of direct `ICoinGeckoService`
- Use `GetOrderBookMidPriceAsync()` as primary price source
- Add `IPriceValidator` validation before returning prices
- Implement retry logic with exponential backoff
- Add latency monitoring

### Phase 2: Risk Management Overhaul (Priority: CRITICAL)

#### 2.1 Create IKillSwitchService
**File**: `Services/Risk/KillSwitchService.cs`

**Features**:
- Monitor realtime PnL from `Trades` table
- Detect consecutive losses (configurable threshold, e.g., 5 losses)
- Daily loss limit enforcement
- Total drawdown limit
- Automatic bot pause + alert on trigger

**Database**:
- Add `KillSwitchEvents` table to track triggers
- Add `BotRiskState` table for per-bot risk metrics

#### 2.2 Create IPositionTracker Service
**File**: `Services/Risk/PositionTracker.cs`

**Features**:
- Track open positions per bot
- Calculate realtime unrealized PnL
- Calculate realized PnL from trades
- Aggregate exposure across bots per user

#### 2.3 Enhance RiskManager
**File**: `Services/Bot/RiskManager.cs`

**Changes**:
- Remove hard-coded capital limits
- Read limits from `BotRiskConfigurations` table
- Read user balance from `IPortfolioService`
- Inject `IKillSwitchService` and call in `CheckLimitsAsync`
- Add `CheckCooldownAsync` method
- Add `CheckRateLimitAsync` method for Grid bot

**New Methods**:
```csharp
Task<bool> CheckKillSwitchAsync(int botId);
Task<bool> CheckDailyLossLimitAsync(int botId);
Task<bool> CheckCooldownAsync(int botId, TimeSpan minCooldown);
Task<bool> CheckRateLimitAsync(int botId, int maxOrdersPerCycle);
```

### Phase 3: Strategy Fixes (Priority: HIGH)

#### 3.1 Fix Grid Trading Strategy
**File**: `Services/Bot/Strategies/GridTradingStrategy.cs`

**Changes**:
- Add rate limiting: min 30s cooldown between same-side orders
- Add max 5 orders per execution cycle
- Implement trailing TP/SL for held inventory
- Replace `GetMidPriceAsync` with exchange mark price
- Add validation before order placement
- Respect `takeProfitPercent` and `stopLossPercent` parameters

#### 3.2 Fix Momentum Scalping Strategy
**File**: `Services/Bot/Strategies/MomentumScalpingStrategy.cs`

**Changes**:
- Replace simulation with real `TradingService.PlaceOrderAsync` calls
- Implement actual exit order placement
- Add trailing stop loss
- Remove TODO comments
- Add position size validation

#### 3.3 Enhance Aggressive Forex Strategy
**File**: `Services/Bot/Strategies/AggressiveForexStrategy.cs`

**Changes**:
- Add kill switch integration
- Validate Martingale multiplier (prevent overexposure)
- Add trailing TP/SL
- Improve pyramid logic

#### 3.4 Create DCA Strategy
**File**: `Services/Bot/Strategies/DCAStrategy.cs`

**Features**:
- Periodic buy orders (e.g., every 1 hour, 4 hours, daily)
- Fixed amount or percentage-based
- Optional sell target (e.g., +10% profit)
- Support for both accumulation and taking profit modes
- Configurable buy intervals and amounts

**Parameters**:
```json
{
  "symbol": "BTCUSDT",
  "buyAmountUSDT": 100,
  "intervalMinutes": 60,
  "takeProfitPercent": 10,
  "maxTotalInvestment": 5000,
  "mode": "accumulate" // or "accumulate-and-sell"
}
```

### Phase 4: Execution Flow Enhancements (Priority: HIGH)

#### 4.1 Enhance BotExecutionHostedService
**File**: `Services/Bot/BotExecutionHostedService.cs`

**Changes**:
- Remove hard-coded `AllowedCapital = 100000m`
- Get capital from `IRiskManager.GetBotCapitalLimitAsync(userId, botId)`
- Add pre-execution guardrails:
  ```csharp
  if (!await _riskManager.CheckKillSwitchAsync(botId)) return;
  if (!await _riskManager.CheckDailyLossLimitAsync(botId)) return;
  if (!await _riskManager.CheckCooldownAsync(botId, minCooldown)) return;
  ```
- Add comprehensive logging (price, qty, latency, position state)
- Add exception handling with alerting

#### 4.2 Enhance BotMonitorHostedService
**File**: `Services/Bot/BotMonitorHostedService.cs`

**Changes**:
- Complete TODO: Cancel pending orders when stopping
  ```csharp
  var pendingOrders = await _context.TradingBotOrders
      .Where(o => o.BotId == bot.Id && o.Status == "Pending")
      .ToListAsync();

  foreach (var order in pendingOrders)
  {
      await _tradingService.CancelAsync(order.OrderId);
  }
  ```
- Add kill switch monitoring
- Alert on degraded state

#### 4.3 Add StartAsync Validation
**File**: `Services/Application/BotApplicationService.cs`

**Changes**:
- In `StartAsync`, add strategy validation:
  ```csharp
  var strategy = _strategyRegistry.GetStrategy(bot.StrategyKey);
  var validation = await strategy.ValidateAsync(parameters);
  if (!validation.IsValid) throw new ValidationException(validation.Errors);
  ```
- Add risk limit check:
  ```csharp
  var limitsOk = await _riskManager.CheckLimitsAsync(userId, botId, capital);
  if (!limitsOk) throw new RiskLimitExceededException();
  ```
- Add cooldown check (if bot was recently stopped)

### Phase 5: Backtest Engine Improvements (Priority: MEDIUM)

#### 5.1 Create Realistic BacktestEngine
**File**: `Services/Backtest/RealisticBacktestEngine.cs`

**Features**:
1. **Order Lifecycle Simulation**
   - Placed → (wait for price match) → Partially Filled → Filled
   - Simulate order book matching for limit orders
   - Market orders fill at current candle's price ± slippage

2. **Fee Simulation**
   - Maker fee: 0.1% (limit orders)
   - Taker fee: 0.1% (market orders)
   - Configurable per exchange

3. **Slippage Simulation**
   - Market orders: 0.05% - 0.2% depending on volume
   - Large orders: proportional slippage

4. **Latency Simulation**
   - Order placement delay: 50-200ms
   - Market data delay: 100-500ms
   - Orders placed mid-candle don't fill until next candle

5. **Realistic Fills**
   - Limit buy fills only if `low <= limitPrice`
   - Limit sell fills only if `high >= limitPrice`
   - No instant fills at candle close

#### 5.2 Update Strategy Backtest Methods
**Files**: All strategy classes

**Changes**:
- Use `RealisticBacktestEngine` base class
- Remove assumptions like "fill at candle close"
- Simulate order placement with timestamps
- Track pending orders across candles

### Phase 6: Data Integrity & Monitoring (Priority: MEDIUM)

#### 6.1 Add Volume Data to MarketDataProvider
**File**: `Services/Bot/MarketDataProvider.cs`

**Changes**:
- Return volume with OHLCV data
- Validate volume > 0 for valid ticks
- Filter candles with anomalous volume (> 10x average)

#### 6.2 Add Stale Tick Detection
**File**: `Services/Market/PriceValidator.cs`

**Logic**:
```csharp
if (DateTime.UtcNow - lastUpdate > TimeSpan.FromMinutes(5))
{
    _logger.LogWarning("Stale price data detected");
    return ValidationResult.Fail("Stale data");
}
```

#### 6.3 Add API Error Detection
**File**: `Services/Market/ExchangeDataProvider.cs`

**Features**:
- Detect HTTP errors (429, 500, 503)
- Exponential backoff retry (100ms, 200ms, 400ms, 800ms)
- Circuit breaker pattern (fail-fast after 5 consecutive errors)
- Fallback to cached data

### Phase 7: Advanced Features (Priority: LOW)

#### 7.1 Implement Trailing TP/SL
**File**: `Services/Trading/TrailingStopService.cs`

**Logic**:
- Track highest price since entry for long positions
- Track lowest price since entry for short positions
- Update stop loss dynamically
- Trigger when price retraces by X% from peak

#### 7.2 Add Position Pyramiding
**Existing**: Already in AggressiveForexStrategy

**Enhancements**:
- Add to Momentum strategy
- Configurable pyramid levels
- Risk-adjusted position sizing

## Database Schema Changes

### New Tables

```sql
-- Kill switch event tracking
CREATE TABLE KillSwitchEvents (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    BotId INT NOT NULL,
    TriggerReason VARCHAR(255) NOT NULL,
    TriggerTime DATETIME(6) NOT NULL,
    TotalLoss DECIMAL(18,8),
    ConsecutiveLosses INT,
    FOREIGN KEY (BotId) REFERENCES TradingBots(Id)
);

-- Bot risk state tracking
CREATE TABLE BotRiskState (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    BotId INT NOT NULL UNIQUE,
    ConsecutiveLosses INT DEFAULT 0,
    DailyLoss DECIMAL(18,8) DEFAULT 0,
    DailyLossResetAt DATETIME(6) NOT NULL,
    TotalDrawdown DECIMAL(18,8) DEFAULT 0,
    LastOrderAt DATETIME(6),
    OrderCountThisCycle INT DEFAULT 0,
    UpdatedAt DATETIME(6) NOT NULL,
    FOREIGN KEY (BotId) REFERENCES TradingBots(Id)
);

-- User capital limits (replace hard-coded values)
CREATE TABLE UserCapitalLimits (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    MaxTotalExposure DECIMAL(18,8) NOT NULL DEFAULT 10000,
    MaxCapitalPerBot DECIMAL(18,8) NOT NULL DEFAULT 5000,
    MaxBotsAllowed INT NOT NULL DEFAULT 5,
    MaxDailyLoss DECIMAL(18,8) NOT NULL DEFAULT 500,
    UpdatedAt DATETIME(6) NOT NULL,
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
);
```

### Updated Tables

```sql
-- Add to BotRiskConfigurations
ALTER TABLE BotRiskConfigurations ADD COLUMN ConsecutiveLossLimit INT DEFAULT 5;
ALTER TABLE BotRiskConfigurations ADD COLUMN DailyLossLimit DECIMAL(18,8) DEFAULT 1000;
ALTER TABLE BotRiskConfigurations ADD COLUMN MaxDrawdownPercent DECIMAL(5,2) DEFAULT 20.00;
ALTER TABLE BotRiskConfigurations ADD COLUMN MinOrderCooldownSeconds INT DEFAULT 30;
ALTER TABLE BotRiskConfigurations ADD COLUMN MaxOrdersPerCycle INT DEFAULT 5;
```

## Configuration Changes

### appsettings.json

```json
{
  "Trading": {
    "DefaultExchange": "Binance",
    "FallbackToCoinGecko": true,
    "PriceValidation": {
      "MaxDeviationPercent": 5.0,
      "StaleDataThresholdMinutes": 5,
      "MinVolume": 1000
    },
    "KillSwitch": {
      "Enabled": true,
      "DefaultConsecutiveLossLimit": 5,
      "DefaultDailyLossLimit": 1000,
      "DefaultMaxDrawdownPercent": 20.0
    },
    "Backtest": {
      "SimulateLatency": true,
      "LatencyMinMs": 50,
      "LatencyMaxMs": 200,
      "SlippagePercent": 0.1,
      "MakerFeePercent": 0.1,
      "TakerFeePercent": 0.1
    }
  }
}
```

## Testing Strategy

### Unit Tests

1. **PriceValidator Tests**
   - Test deviation detection
   - Test stale data detection
   - Test outlier filtering

2. **KillSwitch Tests**
   - Test consecutive loss detection
   - Test daily loss limit
   - Test drawdown calculation

3. **RiskManager Tests**
   - Test capital limit enforcement
   - Test cooldown logic
   - Test rate limiting

### Integration Tests

1. **Grid Strategy Tests**
   - Test rate limiting prevents spam
   - Test order placement with validation
   - Test TP/SL triggers

2. **Momentum Strategy Tests**
   - Test real order placement
   - Test position sizing
   - Test exit logic

3. **DCA Strategy Tests**
   - Test periodic execution
   - Test accumulation mode
   - Test take profit mode

### QA Scenarios (As Specified)

#### Scenario 1: Low Volatility Sideways (±0.5%)
**Expected**:
- Grid bot places 2-3 orders max per hour
- Momentum bot stays flat (no positions)
- Kill switch not triggered
- All orders succeed despite random delays

**Test Data**: 1000 candles, price oscillates 50000 ± 250

#### Scenario 2: Strong Trend + Breakout
**Expected**:
- Trend bot pyramids correctly (3-4 levels)
- Grid cancels counter-trend orders
- TP triggers at target
- Trailing stop follows price up

**Test Data**: 500 candles, price trends from 50000 to 60000, then breakout to 65000

#### Scenario 3: Flash Crash (-15%) then Rebound (+10%)
**Expected**:
- API timeout during crash (simulate)
- Risk manager blocks new entries
- Stop losses trigger (or kill switch)
- No duplicate orders on retry
- Bot recovers gracefully after rebound

**Test Data**: 200 candles, crash from 50000 to 42500, then recover to 46750

## Implementation Timeline

### Week 1: Core Infrastructure
- Day 1-2: PriceValidator, ExchangeDataProvider interfaces
- Day 3-4: MarketDataProvider refactor
- Day 5: KillSwitchService, PositionTracker

### Week 2: Risk Management
- Day 1-2: RiskManager enhancements
- Day 3: Database migrations
- Day 4-5: Integration and testing

### Week 3: Strategy Fixes
- Day 1: Grid strategy fixes
- Day 2: Momentum strategy fixes
- Day 3: DCA strategy implementation
- Day 4-5: Integration and testing

### Week 4: Backtest & QA
- Day 1-2: RealisticBacktestEngine
- Day 3: QA scenario implementation
- Day 4-5: Full system testing and bug fixes

## Success Criteria

### Pre-Deploy Checklist

- [ ] All strategies (Grid/DCA/Trend/Momentum) have working implementations
- [ ] `StartAsync` performs validation + risk-limits + cooldown
- [ ] Market data provides correct mark/mid price with latency monitor
- [ ] Position sizing uses real balance + fee + slippage
- [ ] Kill switch & consecutive loss logic works correctly
- [ ] Entry/exit logic uses actual TradingService order placement
- [ ] Backtest replicates fees, slippage, latency, order lifecycle
- [ ] Bot restart does NOT send duplicate orders
- [ ] All QA scenarios pass
- [ ] Logging & alerting tested for timeout / order failure / guardrail triggers

### Performance Targets

- Order placement latency: < 500ms (p99)
- Market data refresh: < 1s
- Risk check latency: < 100ms
- Backtest performance: > 1000 candles/second
- Memory usage: < 500MB per worker
- CPU usage: < 50% average

## Rollout Plan

### Phase 1: Development Environment
- Deploy all changes to dev
- Run QA scenarios
- Monitor for 48 hours

### Phase 2: Staging Environment
- Deploy to staging
- Run with real API (testnet)
- Monitor for 1 week
- Performance testing

### Phase 3: Production Rollout
- Feature flag: `EnableEnhancedRiskManagement`
- Gradual rollout: 10% → 50% → 100%
- Monitor kill switch triggers
- Monitor performance metrics

## Monitoring & Alerts

### Key Metrics to Monitor

1. **Kill Switch Triggers**
   - Alert: Any trigger
   - Dashboard: Count per day

2. **Order Failure Rate**
   - Alert: > 5% failure rate
   - Dashboard: Success rate per strategy

3. **Price Data Staleness**
   - Alert: > 5 minutes stale
   - Dashboard: Last update timestamp

4. **Bot Performance**
   - Dashboard: Win rate, avg PnL, drawdown per bot
   - Alert: Any bot with > 20% drawdown

5. **System Health**
   - Dashboard: Worker pool utilization
   - Alert: Worker queue > 100

## Risk Mitigation

### Known Risks

1. **Exchange API Rate Limits**
   - Mitigation: Implement request queuing and rate limiting
   - Fallback: Use CoinGecko if rate limited

2. **Market Data Latency**
   - Mitigation: Use WebSocket connections for real-time data
   - Fallback: Increase candle intervals

3. **Database Performance**
   - Mitigation: Add indexes on frequently queried columns
   - Fallback: Redis caching for hot data

4. **Bot Execution Lag**
   - Mitigation: Increase worker pool size
   - Fallback: Queue non-critical bots

## Conclusion

This refactor will transform the trading system from a prototype to a production-ready platform with:

- **Reliable risk management** to protect user capital
- **Accurate market data** for informed trading decisions
- **Realistic backtesting** to prevent overfitting
- **Complete strategy suite** including DCA
- **Robust execution** with proper guardrails

All changes will be implemented incrementally with comprehensive testing at each stage.

---

**Document Version**: 1.0
**Last Updated**: 2025-11-14
**Author**: Trading System Refactor Team
