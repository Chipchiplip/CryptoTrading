# Architecture

## Overview
Single ASP.NET Core 9 API with layered services and EF Core (MySQL). Frontend is React/Vite. Optional side services: dashboard-api (Node) and StockAgent (Python).

## Layering
- Controllers → Services → Repositories/UoW → EF Core (ApplicationDbContext) → MySQL
- Cross-cutting: Middleware (error), Attributes (RequireProOrPremium), SignalR hubs, MemoryCache.

## Key Components
- Auth/AuthZ: JWT + OAuth + 2FA, CurrentUser service, Email service.
- Market: CoinGeckoService, CryptoCacheService, CryptoDataSyncService.
- Trading: TradingService, OrderMatchingBackgroundService, wallets/holds/trades.
- Portfolio: PortfolioService, WatchlistService.
- Bots: StrategyRegistry + strategies (grid, aggressive-forex, momentum), BotExecutionHostedService, BotMonitorHostedService, BotSignalRDispatcher.
- AI: AiTradingChatService, GeminiService, AiRecommendationService.
- Payments: VnPayService, SubscriptionService, PaymentHistory/DepositTransaction handling.
- Background services: CryptoSyncBackgroundService, RealtimeBroadcastService, OrderMatchingBackgroundService, BotExecutionHostedService, BotMonitorHostedService, SubscriptionExpirationBackgroundService.
- Realtime: SignalR hubs `/marketHub`, `/botHub`.

## Data Model (major tables)
Users, LoginActivity, Cryptocurrencies, CryptoPrices, MarketStats, Wallets, WalletMovements, Orders, OrderHolds, Trades, Levels, Roles, Subscriptions, PaymentHistories, DepositTransactions, Notifications, BotStrategyDefinitions, TradingBots, TradingBotOrders, TradingBotLogs, TradingBotRuntimeSnapshots, AiGeneratedBotProfiles.

## External Integrations
- CoinGecko (HTTP client, 10s timeout)
- Stripe + VNPay (payments)
- Cloudflare R2 (avatar uploads)
- SMTP email
- Gemini/OpenAI (AI chat/reco)

## Concurrency & Consistency
- Transactions around trading operations; no rowversion tokens yet (risk).
- Background loops run singly per instance; add distributed locks if multi-instance.
- Cache: MemoryCache only; use Redis for scale-out + SignalR backplane.

## Logging/Observability
- ILogger-based; add Serilog + OpenTelemetry for prod.

## Deployment Notes
- Configure connection strings and secrets via env vars.
- Use AUTO_APPLY_MIGRATIONS=true cautiously; otherwise run migrations manually.
- For multi-instance: add Redis (cache/backplane) and locking strategy.
