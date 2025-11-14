# 🚀 Setup Guide - Trading Platform Improvements

## Quick Start

### 1. Install Required Packages

The improvements use standard .NET packages that should already be in your project. If not, add:

```bash
# Polly for resilience
dotnet add package Polly

# For future: Redis support (optional for now)
# dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
# dotnet add package Microsoft.AspNetCore.SignalR.StackExchangeRedis
```

### 2. Run Database Migration

**Option A: Using Entity Framework**
```bash
# Create migration
dotnet ef migrations add AddAuditAndConfiguration

# Apply to database
dotnet ef database update
```

**Option B: Using SQL Script Directly**
```bash
# Run the provided SQL script
mysql -u your_user -p your_database < database/mysql/003_AuditAndConfiguration_MySQL.sql
```

### 3. Verify Setup

Run the application and check:

```bash
dotnet run
```

**Check 1: Correlation ID**
```bash
curl -i http://localhost:5186/api/market/cryptocurrencies
# Look for X-Correlation-ID header in response
```

**Check 2: Configuration**
```sql
SELECT * FROM TradingConfigurations;
SELECT * FROM BotRiskConfigurations;
```

**Check 3: Audit Trail**
- Place a test order
- Check `AuditEvents` table:
```sql
SELECT * FROM AuditEvents ORDER BY CreatedAt DESC LIMIT 5;
```

**Check 4: Reconciliation**
- Wait 1-5 minutes for first reconciliation run
- Check results:
```sql
SELECT * FROM ReconciliationResults ORDER BY ReconciliationTime DESC LIMIT 1;
```

## Configuration

### Environment Variables

```bash
# Optional: Auto-apply migrations on startup
export AUTO_APPLY_MIGRATIONS=true

# Environment name (affects which configuration is loaded)
export ASPNETCORE_ENVIRONMENT=Development  # or Sandbox, Production
```

### appsettings.json

Add trading settings section:

```json
{
  "TradingSettings": {
    "FeeRate": "0.001",
    "MarketPriceBuffer": "0.05"
  }
}
```

### Database Configuration

The system uses a three-tier configuration:
1. **Database** (highest priority) - `TradingConfigurations` table
2. **appsettings.json** (fallback)
3. **Hardcoded defaults** (last resort)

To update configuration without redeployment:

```sql
UPDATE TradingConfigurations 
SET ConfigValue = '0.002', UpdatedAt = NOW() 
WHERE ConfigKey = 'FeeRate' AND Environment = 'Production';
```

## Testing

### Run Unit Tests

```bash
cd Tests
dotnet test --logger "console;verbosity=detailed"
```

### Expected Output

```
Test Run Successful.
Total tests: 19
     Passed: 19
```

### Test Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Monitoring

### 1. Check Audit Trail

```sql
-- Recent operations
SELECT EventType, UserId, EntityType, EntityId, CreatedAt, CorrelationId
FROM AuditEvents 
ORDER BY CreatedAt DESC 
LIMIT 10;

-- Operations for specific user
SELECT * FROM AuditEvents 
WHERE UserId = 1 
ORDER BY CreatedAt DESC;

-- Operations with same correlation ID (trace entire request)
SELECT * FROM AuditEvents 
WHERE CorrelationId = 'your-correlation-id';
```

### 2. Check Reconciliation Results

```sql
-- Latest reconciliation
SELECT * FROM ReconciliationResults 
ORDER BY ReconciliationTime DESC 
LIMIT 1;

-- Any mismatches found
SELECT * FROM ReconciliationResults 
WHERE MismatchCount > 0 
ORDER BY ReconciliationTime DESC;

-- View mismatch details
SELECT 
    EntityType,
    ReconciliationTime,
    MismatchCount,
    JSON_PRETTY(Mismatches) as MismatchDetails
FROM ReconciliationResults 
WHERE MismatchCount > 0;
```

### 3. Check Risk Manager Status

```sql
-- Bot risk configurations
SELECT * FROM BotRiskConfigurations;

-- Recent bot orders with PnL
SELECT 
    b.DisplayName as BotName,
    o.Status,
    o.PnL,
    o.PlacedAt
FROM TradingBotOrders o
JOIN TradingBots b ON o.BotId = b.Id
ORDER BY o.PlacedAt DESC
LIMIT 10;
```

### 4. Check Fee Ledger

```sql
-- Total fees by user
SELECT 
    UserId,
    FeeCurrency,
    SUM(FeeAmount) as TotalFees,
    COUNT(*) as FeeCount
FROM FeeLedger
GROUP BY UserId, FeeCurrency;

-- Recent fees
SELECT * FROM FeeLedger 
ORDER BY CreatedAt DESC 
LIMIT 10;
```

## Troubleshooting

### Issue: Migration Fails

**Symptom**: `dotnet ef database update` fails

**Solutions**:
1. Check database connection string
2. Ensure user has CREATE TABLE permissions
3. Try running SQL script directly
4. Check for existing tables with same names

```sql
-- Check existing tables
SHOW TABLES LIKE 'Audit%';
```

### Issue: Reconciliation Not Running

**Symptom**: No entries in `ReconciliationResults` table

**Solutions**:
1. Check application logs for errors
2. Verify background service is registered:
```csharp
// Should be in Program.cs
builder.Services.AddHostedService<ReconciliationBackgroundService>();
```
3. Wait at least 5 minutes (default interval)
4. Check for exceptions in logs

### Issue: Correlation IDs Missing

**Symptom**: No `X-Correlation-ID` in response headers

**Solutions**:
1. Verify middleware is registered:
```csharp
// Should be in Program.cs
app.UseMiddleware<CorrelationIdMiddleware>();
```
2. Check middleware order (should be early in pipeline)
3. Restart application

### Issue: Audit Events Not Created

**Symptom**: `AuditEvents` table is empty

**Solutions**:
1. Verify service is registered:
```csharp
builder.Services.AddScoped<IAuditService, AuditService>();
```
2. Check for exceptions in logs
3. Verify HttpContextAccessor is registered
4. Check database permissions

### Issue: Tests Failing

**Symptom**: Unit tests fail to run

**Solutions**:
1. Restore packages:
```bash
dotnet restore
```
2. Check test project references:
```bash
cd Tests
dotnet list reference
```
3. Clean and rebuild:
```bash
dotnet clean
dotnet build
dotnet test
```

## Performance Tuning

### Reconciliation Interval

To change reconciliation frequency, modify in `ReconciliationBackgroundService`:

```csharp
private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Change to 1, 10, etc.
```

### Audit Retention

To prevent `AuditEvents` from growing too large, set up periodic cleanup:

```sql
-- Delete audit events older than 90 days
DELETE FROM AuditEvents 
WHERE CreatedAt < DATE_SUB(NOW(), INTERVAL 90 DAY);

-- Or archive to separate table
CREATE TABLE AuditEvents_Archive LIKE AuditEvents;
INSERT INTO AuditEvents_Archive 
SELECT * FROM AuditEvents 
WHERE CreatedAt < DATE_SUB(NOW(), INTERVAL 90 DAY);
```

### Configuration Cache

The configuration service caches values for 5 minutes. To adjust:

```csharp
private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5); // Change as needed
```

## Next Steps

### Recommended Enhancements (Optional)

1. **Redis Cache** (for distributed caching):
   ```bash
   dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
   ```

2. **Serilog** (for structured logging):
   ```bash
   dotnet add package Serilog.AspNetCore
   dotnet add package Serilog.Sinks.Console
   dotnet add package Serilog.Sinks.File
   ```

3. **OpenTelemetry** (for distributed tracing):
   ```bash
   dotnet add package OpenTelemetry.Extensions.Hosting
   dotnet add package OpenTelemetry.Instrumentation.AspNetCore
   dotnet add package OpenTelemetry.Exporter.Console
   ```

### Production Deployment Checklist

- [ ] Run database migration on production
- [ ] Verify all configuration values in `TradingConfigurations`
- [ ] Set up monitoring for `ReconciliationResults`
- [ ] Configure alert rules for mismatches
- [ ] Set up log aggregation for `AuditEvents`
- [ ] Test idempotency with duplicate requests
- [ ] Verify correlation IDs in distributed traces
- [ ] Monitor kill switch activations
- [ ] Set up periodic cleanup for old audit data
- [ ] Document emergency rollback procedure

## Support

- **Documentation**: See `docs/IMPROVEMENTS_IMPLEMENTED.md`
- **Database Schema**: See `database/mysql/003_AuditAndConfiguration_MySQL.sql`
- **Tests**: See `Tests/` directory

For questions or issues, check:
1. Application logs (with correlation IDs)
2. `AuditEvents` table for operation history
3. `ReconciliationResults` for data integrity
4. Unit tests for expected behavior

