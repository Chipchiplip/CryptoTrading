# Bot Troubleshooting Guide

Use this guide to debug bots and grid strategies in the current ASP.NET Core monolith.

## Quick checks (in order)
- Status must be `Running`; if not, start and watch `lastExecutionAt`.
- Logs: `GET /api/bots/{id}/logs?page=1&pageSize=50` (Execution + Trading). Look for price fetch, signal, and order placement.
- Orders: `GET /api/bots/{id}/orders?page=1&pageSize=20`. If none, the bot never placed; if unfilled, matcher has not filled yet.
- Runtime snapshot: review `totalOrders`, `filledOrders`, `lastSignal`, `nextRunAt`, `heartbeat`.
- Nudge: `POST /api/bots/{id}/nudge` to force a run now.
- Balance: ensure quote asset balance exists; holds reduce available balance.
- Market data: CoinGecko reachable; verify prices in `/api/market/cryptocurrencies`.

## Common causes and fixes
- No market data / price fetch errors: CoinGecko down or API key missing → retry, verify `CoinGecko` config; timeout is 10s with backoff.
- Sideways trend: strategy waits for signal; reduce `emaPeriod`/thresholds or let history accumulate.
- Insufficient balance: wallet has zero available or is fully locked in holds → deposit or release holds.
- Symbol mismatch: ensure base/quote assets (e.g., `ETH`/`USD`) exist in `Cryptocurrencies`.
- Bot not executing: `lastExecutionAt` stale → nudge; confirm `BotExecutionHostedService` running (app logs).
- Order placement errors: inspect Trading logs; price validation may reject orders outside allowed bounds.

## Grid bot & order-count issues (merged fixes)
- “Only 1-2 orders” or “no orders”:
  - Check `gridLevels`, `lowerBound`, `upperBound`, and `orderSize` > 0.
  - Ensure capital/inventory covers planned grid; low balance caps number of placed orders.
  - Price must be within [lowerBound, upperBound]; outside range → no placements.
- Missing parameters:
  - Required: bounds, levels, order size, capital allocation; verify JSON matches strategy schema.
- Matching behavior:
  - Grid uses limit orders; fills depend on OrderMatchingBackgroundService (5s loop). If prices never cross grid lines, orders remain open.
  - Maker price execution; partial fills expected when liquidity is thin.
- Strategy differences:
  - Aggressive Forex vs Grid: aggressive may pyramid/martingale; grid is range-bound. Pick by market regime.

## Monitoring and health (rolled up)
- Logs: Execution (price fetch, signals), Trading (orders, errors).
- Runtime metrics: `totalOrders`, `filledOrders`, `unrealizedPnl`, `realizedPnl`, `lastSignal`, `lastExecutionAt`, `heartbeatAt`.
- Health: `GET /health`; verify background services in application logs.

## Price fetch troubleshooting
- Symptoms: “Unable to get current price”, `null` prices, repeated backoff.
- Actions: verify CoinGecko base URL/key, outbound network, and that `GetMarketDataAsync(forceRefresh)` succeeds; restart if cache is stale.

## Quick commands
- Force run: `POST /api/bots/{id}/nudge`
- Recent logs: `GET /api/bots/{id}/logs?page=1&pageSize=50&category=Execution`
- Orders: `GET /api/bots/{id}/orders?page=1&pageSize=20`

## Debug checklist
- [ ] Status = Running
- [ ] `lastExecutionAt` recent
- [ ] Logs show price + signal + placed order
- [ ] `totalOrders > 0`
- [ ] Balance available (not fully locked)
- [ ] CoinGecko reachable
- [ ] No blocking errors in Trading/BotExecution services

Expected: after a few runs you should see price logs, signals, placed orders, and non-null `lastSignal`. If not, re-check logs and parameters.
