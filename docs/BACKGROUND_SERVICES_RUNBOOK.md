# Background Services Runbook

Purpose: How to operate, monitor, and debug the recurring background services in the CryptoTrading monolith.

## Services Overview
| Service | Purpose | Interval | Key Dependencies |
|---------|---------|----------|-------------------|
| CryptoSyncBackgroundService | Sync market data (prices/stats) from CoinGecko into cache/DB | Scheduled inside service (loop) | CoinGecko API, MemoryCache, DbContext |
| RealtimeBroadcastService | Refresh market data cache and push real-time updates via SignalR | ~5s loop with backoff/jitter | CoinGeckoService, SignalR hub context |
| OrderMatchingBackgroundService | Match limit orders (buy/sell) and generate trades | 5s loop | TradingService, Orders/Trades tables |
| BotExecutionHostedService | Execute bot strategies, create orders | Interval per bot (default ~60s, scheduled inside service) | StrategyRegistry, TradingService, Bot tables |
| BotMonitorHostedService | Detect stalled bots, update status/heartbeat | Loop with configured delay | Bot tables, logging, SignalR dispatcher |
| SubscriptionExpirationBackgroundService | Expire subscriptions past `CurrentPeriodEndUtc` | Loop with delay | Subscriptions table, DbContext |

## Execution & Scheduling
- All services run as `IHostedService/BackgroundService` inside the monolith.
- Intervals are hard-coded (5s for matcher/broadcast) or strategy-driven (bots). Review each service for delay/backoff logic before changing.
- In single-instance mode, they run concurrently but independently; there is no leader election.

## Failure Symptoms & Checks
- CryptoSyncBackgroundService
  - Symptoms: stale market data, null/old prices, CoinGecko errors in logs.
  - Checks: CoinGecko API health, network, log warnings/errors.
- RealtimeBroadcastService
  - Symptoms: SignalR clients stop receiving price/stats updates; logs show backoff.
  - Checks: SignalR connectivity, CoinGecko latency, app logs for backoff growth.
- OrderMatchingBackgroundService
  - Symptoms: Limit orders stay NEW forever; no trades generated.
  - Checks: DB connectivity, locks on Orders/Trades tables, application logs.
- BotExecutionHostedService
  - Symptoms: Bots show stale `lastExecutionAt`, `totalOrders` not increasing.
  - Checks: Bot status, Bot logs, TradingService exceptions, Strategy registry injection.
- BotMonitorHostedService
  - Symptoms: Stale bots not detected; status never flips to Degraded/Error.
  - Checks: Bot runtime snapshots, monitor logs, heartbeat logic.
- SubscriptionExpirationBackgroundService
  - Symptoms: Expired subscriptions remain active.
  - Checks: DB clock skew, query filtering on `CurrentPeriodEndUtc`, log warnings.

## Observability
- Logging: Each service logs errors and key lifecycle messages. Raise log level to Debug when diagnosing.
- Metrics (recommended to add):
  - Run duration, success/failure counts
  - Items processed (orders matched, bots executed, subscriptions expired)
  - External API latency/errors (CoinGecko)
- Traceability: Add correlation IDs per run (e.g., `runId`) and per entity (orderId/botId).

## Debug Playbooks
- Any service:
  1) Check logs around the run window.
  2) Validate DB connectivity and pending migrations.
  3) If external API is involved, test reachability manually.
  4) Restart service/app if stuck due to transient errors.
- OrderMatching:
  - Inspect Orders where `Status=NEW` and `CreatedAt` is old.
  - Ensure matching loop is running (logs every interval). Manually invoke matching via TradingService if needed.
  - Verify no DB deadlocks; check isolation levels.
- BotExecution/BotMonitor:
  - Check bot runtime snapshots for `nextRunAt`, `lastExecutionAt`, `heartbeatAt`.
  - Validate strategy parameters are present; bad params can abort execution.
  - Verify SignalR dispatch succeeds (exceptions in dispatcher).
- RealtimeBroadcast/CryptoSync:
  - Verify CoinGecko key/base URL; check throttle/backoff.
  - Inspect MemoryCache TTL logic.
- SubscriptionExpiration:
  - Query subscriptions past `CurrentPeriodEndUtc` with `Status=active` to find misses.

## Multi-Instance Guidance
- Current state: No distributed locks/backplane. Running multiple instances risks:
  - Duplicate matching runs → double-settlement.
  - Duplicate bot executions → duplicate orders.
  - Duplicate subscription expirations.
- Mitigations (to implement before scale-out):
  - Distributed lock (e.g., Redis) around:
    - OrderMatching loop
    - BotExecution loop (per bot or per batch)
    - SubscriptionExpiration sweep
  - SignalR Redis backplane so broadcasts reach all clients.
  - Idempotent writes (unique keys) when feasible.

## Deployment Best Practices
- Single-instance until distributed locks/backplane are in place.
- Configure timeouts for external calls (CoinGecko, etc.).
- Keep `AUTO_APPLY_MIGRATIONS` off in production; apply migrations manually.
- Monitor:
  - Error rates per service
  - Latency per loop
  - Backoff growth (RealtimeBroadcast)
  - Queue depth equivalents (count of NEW orders, number of stale bots)
- Rollback plan: ability to disable a service if it misbehaves (feature flags or config switches).
