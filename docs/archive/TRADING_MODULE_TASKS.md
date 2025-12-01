# Trading Roadmap (Current)

## Stability & Safety (P0/P1)
- Concurrency/locking: add rowversion on Orders/Wallets or per-wallet locks to prevent double-spend.
- Idempotency: keys for order placement; guard matcher against duplicate fills.
- Validation: tighter price/quantity bounds; consistent error codes (`TRADING_*`).
- Logging/metrics: trades matched per run, matcher duration, rejected orders.

## Matcher Improvements
- Backoff/jitter tuning; detect long-running cycles.
- Partial fill handling review; ensure holds release correctly.
- Caching of order book snapshots for read endpoints (if needed).

## Wallet/Hold Lifecycle
- Ensure holds released on cancel/reject/timeout.
- Settlement audit: wallet movement consistency checks.

## Bot Integration
- Ensure bot-created orders follow same validation/idempotency.
- Protect against duplicate bot execution (consider distributed lock plan).
- Expose bot→order linkage in queries for debugging/UI.

## PnL & Reporting
- Add realized/unrealized PnL per bot/user; surface via API.
- Add dashboard metrics (volume, fills, fees).

## Testing Matrix
- Unit: order validation, fee calc, hold math, partial fills.
- Integration: place order → match → settle; cancel path; concurrent placements.
- Bot flow: strategy creates orders, matcher fills, wallet updates.
- Payments interaction: deposits → orders consuming balance; subscription unaffected.

## Error Handling
- Align with `ERROR_HANDLING_CONTRACT.md` codes/messages.
- Map matcher errors to actionable responses/logs.

## Scale-Out Readiness
- Design distributed lock for matcher; ensure idempotent writes.
- Plan Redis backplane if matcher/SignalR scale is needed.
