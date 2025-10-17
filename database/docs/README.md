# Database Documentation

## Overview

This directory contains all database-related scripts and documentation for the Crypto Trading Web platform.

## Schema Design

The database is organized into 6 schemas:

### 1. **auth** - Authentication & User Management
- `Users` - User accounts with subscription tiers
- `RefreshTokens` - JWT refresh token management
- `UserActivity` - User action tracking
- `AuditLogs` - Security audit trail

### 2. **market** - Cryptocurrency Market Data
- `Cryptocurrencies` - Supported cryptocurrencies
- `CryptoPrices` - Price history (time-series)
- `MarketStats` - Overall market statistics

### 3. **portfolio** - User Portfolio Management
- `UserWatchlists` - User-created watchlists
- `WatchlistItems` - Coins in watchlists
- `Positions` - User crypto holdings

### 4. **trading** - Trading Operations
- `Balances` - User asset balances
- `Bots` - Trading bot configurations
- `Orders` - Trade orders (with temporal history)
- `Trades` - Order fills
- `BinanceApiKeys` - User API keys (encrypted)

### 5. **billing** - Payments & Subscriptions
- `Subscriptions` - User subscriptions (with temporal history)
- `PaymentHistory` - Payment transactions

### 6. **ops** - Operational Tables
- `Idempotency` - Idempotency key tracking
- `Outbox` - Event outbox pattern
- `Inbox` - Webhook deduplication

## Running Migrations

### Initial Setup

1. Start Docker infrastructure:
```bash
docker-compose up -d
```

2. Run initial schema:
```bash
sqlcmd -S localhost,1433 -U sa -P YourStrong@Password123 -i database/migrations/001_InitialSchema.sql
```

3. Seed data:
```bash
sqlcmd -S localhost,1433 -U sa -P YourStrong@Password123 -i database/migrations/002_SeedData.sql
```

### Using SQL Server Management Studio (SSMS)

1. Connect to `localhost,1433`
2. Login: `sa` / `YourStrong@Password123`
3. Open and execute migration files in order

## Key Features

### Temporal Tables
Orders and Subscriptions use SQL Server Temporal Tables for automatic history tracking:
```sql
-- Query order history
SELECT * FROM trading.Orders 
FOR SYSTEM_TIME ALL 
WHERE Id = @OrderId
ORDER BY ValidFrom;
```

### Optimistic Concurrency
All tables use `ROWVERSION` for optimistic concurrency control:
```csharp
// EF Core will automatically handle concurrency conflicts
context.SaveChanges(); // Throws DbUpdateConcurrencyException if row changed
```

### Idempotency
POST/PUT requests use idempotency keys stored in `ops.Idempotency` to prevent duplicate operations.

### Outbox/Inbox Pattern
- **Outbox**: Ensures events are published exactly once
- **Inbox**: Prevents duplicate webhook processing

## Connection String

```
Server=localhost,1433;Database=CtwDb;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True;
```

## Security Notes

1. **Production**: Change `sa` password immediately
2. **API Keys**: Encrypted using AES-256 before storage
3. **Audit Logs**: All critical operations are logged
4. **Row-Level Security**: Consider implementing for multi-tenant scenarios

## Performance Tuning

### Hot Indexes
- Orders: Composite index on `(UserId, CreatedAtUtc DESC)` with covering columns
- Prices: Composite index on `(CryptocurrencyId, CollectedAtUtc DESC)`
- Activity: Index on `(UserId, CreatedAtUtc DESC)`

### Partitioning (Future)
`market.CryptoPrices` should be partitioned by month once data grows beyond 100M rows.

## Backup Strategy

1. **Full Backup**: Daily at 2 AM UTC
2. **Differential**: Every 6 hours
3. **Transaction Log**: Every 15 minutes
4. **Retention**: 30 days

## Monitoring

Monitor these metrics:
- Deadlock count
- Average query duration
- Index fragmentation
- Database size growth

## Support

For database issues, contact the DevOps team or check the runbook in `/docs/runbooks/`.

