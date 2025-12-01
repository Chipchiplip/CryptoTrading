# Database Overview

Single MySQL schema managed by EF Core.

## Core Tables
- Auth: Users, LoginActivity
- Market: Cryptocurrencies, CryptoPrices, MarketStats
- Portfolio/Wallet: Wallets, WalletMovements, UserWatchlist
- Trading: Orders, OrderHolds, Trades
- Reference: Roles, Levels
- Payments: Subscriptions, PaymentHistories, DepositTransactions
- Notifications: Notifications
- Bots: BotStrategyDefinitions, TradingBots, TradingBotOrders, TradingBotLogs, TradingBotRuntimeSnapshots, AiGeneratedBotProfiles

## Migrations
- EF migrations live in `/Migrations`. Apply with `dotnet ef database update`.
- `AUTO_APPLY_MIGRATIONS=true` can auto-apply at startup (off by default).

## Indexing
Defined in `ApplicationDbContext` (examples: Orders on UserId/CreatedAt and CryptocurrencyId/Status; CryptoPrices on CryptocurrencyId/CollectedAt; Notifications on UserId/CreatedAtUtc). Consider adding rowversion/concurrency tokens for wallets/orders.

## Seeds
- `Program.cs` seeds default Roles, Levels, and built-in BotStrategyDefinitions.
- Additional seed SQL dumps live in `/database`.

## Connection
Set `ConnectionStrings:DefaultConnection` in appsettings.* for MySQL 8 (Pomelo provider).
