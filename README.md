# CryptoTrading

ASP.NET Core 9 monolith for a crypto trading sandbox with human + AI-driven orders/bots, subscriptions, and real-time UI.

## Features
- Auth: JWT + OAuth (Google/GitHub), email verification, 2FA.
- Market: CoinGecko-backed prices/stats with memory cache and scheduled sync.
- Trading: Order placement (market/limit), matching loop, balances via wallets/holds, trades history.
- Portfolio: Watchlists, overview/performance, notifications.
- Bots: Built-in strategies (grid, momentum, aggressive forex), bot runtime snapshots, logs, SignalR updates.
- AI: Gemini/OpenAI chat + recommendations to create bots/place orders.
- Payments: Stripe + VNPay deposits/subscriptions.
- Realtime: SignalR hubs `/marketHub`, `/botHub`.
- Background services: CryptoSync, RealtimeBroadcast, OrderMatching, BotExecution, BotMonitor, SubscriptionExpiration.

## Tech Stack
- Backend: ASP.NET Core 9, EF Core (Pomelo MySQL), MemoryCache, SignalR.
- DB: MySQL 8.
- Frontend: React + Vite + TypeScript.
- Extras: Cloudflare R2 uploads, SMTP email.

## Run Backend
1. Prereqs: .NET 9 SDK, MySQL 8 (provide connection), optional Redis for cache/backplane.
2. Copy `appsettings.Development.json` and replace secrets/connection strings.
3. Apply migrations: `dotnet ef database update`.
4. Run: `dotnet run` (HTTP 5186, HTTPS 7269). Swagger at `/swagger`, health at `/health`.

## Run Frontend
```bash
cd frontend
npm install
npm run dev
```
Default origin: http://localhost:5173 (CORS allowed in Program.cs).

## Repo Layout
- Controllers/, Services/, Models/, Data/ — API, business logic, EF models
- Interfaces/, Repositories/ — Contracts + UnitOfWork
- Middleware/, Hubs/, Attributes/ — Pipeline and realtime
- docs/ — Project docs
- frontend/ — React dashboard
- dashboard-api/ — Optional Node dashboard service
- StockAgent/ — Optional Python LLM trading simulator
- database/, Migrations/ — SQL dumps and EF migrations
- Program.cs — Composition root

## Key Endpoints
- Auth: `/api/auth/*`
- Market: `/api/market/*`
- Portfolio: `/api/portfolio/*`
- Trading: `/api/trading/*`
- Bots: `/api/bots/*`, `/api/bot-strategies/*`
- AI: `/api/ai/chat`, `/api/ai/recommendations`
- Payments: `/api/payment/*`
- SignalR: `/marketHub`, `/botHub`

## Background Services
CryptoSync, RealtimeBroadcast, OrderMatching, BotExecution, BotMonitor, SubscriptionExpiration.

## Security Notes
Do not commit secrets; rotate the example keys; enforce HTTPS in production; add rate limiting and idempotency where needed.

