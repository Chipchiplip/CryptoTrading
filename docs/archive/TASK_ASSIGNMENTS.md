# Task Assignments (Current)

Task board aligned to the current monolith (ASP.NET Core 9, MySQL, React/Vite).

## Backend
- Auth (P1)  
  - Harden 2FA storage and token rotation.  
  - Add rate limiting for login/2FA endpoints.  
  - Dependency: Error model alignment.
- Market Data (P1)  
  - CoinGecko backoff/Polly policies.  
  - Cache TTL tuning; optional Redis toggle.  
  - Dependency: Observability.
- Trading Engine (P0)  
  - Add rowversion or per-wallet lock to prevent double-spend.  
  - Idempotency for order placement.  
  - Matcher robustness and metrics.  
  - Dependency: Concurrency/locking design.
- Bots & Strategies (P1)  
  - Parameter validation hardening.  
  - Add strategy health metrics/logs.  
  - New strategy onboarding checklist.  
  - Dependency: Bot execution/monitor runbook.
- AI Chat & Recommendations (P2)  
  - Timeouts/retries for AI calls.  
  - Token/PII redaction in prompts/logs.  
  - Dependency: Observability, security review.
- Payments (Stripe/VNPay) (P0)  
  - Persist webhook event IDs/idempotency keys.  
  - Signature validation tests.  
  - Unique constraints on external transaction IDs.  
  - Dependency: Payment callbacks doc.
- Realtime Hubs (P2)  
  - Add `[Authorize]` to MarketHub (if needed) and ownership checks in BotHub.  
  - Plan Redis backplane for scale-out.  
  - Dependency: SignalR data flow.
- Background Services (P1)  
  - Add metrics (run duration, processed counts).  
  - Optional distributed lock design.  
  - Dependency: Background services runbook.
- Portfolio Module (P2)  
  - Performance profiling of portfolio queries.  
  - Add unit tests for performance calculations.  
  - Dependency: Portfolio docs.

## Frontend (React/Vite)
- Trading UI: order placement/error handling aligned to error contract (P1).
- Bots UI: runtime/pnl/logs streaming via SignalR (P1).
- Payments UI: Stripe/VNPay status handling, retry UX (P1).
- Portfolio UI: performance/holdings accuracy checks (P2).

## Infra
- Cloudflare R2 (P2): verify avatar upload/policy; key rotation.
- SMTP (P2): rotate creds, add health check.
- Hosting (P1): deployment scripts; consider Redis for backplane/locks if scaling.

## AI/Prompt Engineering
- Prompt hardening for bot generation/recommendations (P2).
- Add safety filters and truncation for long user input (P2).

## Testing Roadmap
- Unit: TradingService (validation, fee calc), Bot strategies (signals), Payment services (signature, mapping).
- Integration: Order lifecycle, Matcher loop, Bot execution → order → match flow, Stripe/VNPay callbacks.
- E2E smoke: Auth, Place order, Bot run + SignalR, Payment deposit/return, Portfolio overview.

## DevOps
- Logging/OTel (P1): structured logs with `code`, `traceId`, `userId`; enable traces.  
  - Dependency: Error handling contract.  
- Migrations (P1): manual apply in prod; gating for AUTO_APPLY_MIGRATIONS.  
- Security (P0): purge committed secrets; enforce env-based config; rate limits; webhook signature enforcement.  
