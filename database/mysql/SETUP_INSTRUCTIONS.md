# MySQL Setup Instructions

## Overview
Your project has been successfully configured to use MySQL instead of SQL Server. The connection string has been updated to point to your Aiven MySQL instance.

## Current Configuration

### Connection String (appsettings.json)


### Project Changes Made
1. ✅ **EF Core Provider**: Changed from `Microsoft.EntityFrameworkCore.SqlServer` to `Pomelo.EntityFrameworkCore.MySql` (v9.0.0)
2. ✅ **Program.cs**: Updated to use `UseMySql()` instead of `UseSqlServer()`
3. ✅ **ApplicationDbContext**: Changed default timestamp from `GETUTCDATE()` to `UTC_TIMESTAMP()`
4. ✅ **Migrations**: Created new MySQL-compatible migration
5. ✅ **Design-time Factory**: Added for EF Core tooling support

## Database Setup Steps

### Option 1: Manual SQL Execution (Recommended)
Since the IP access might be restricted, run these SQL files manually:

1. **Connect to your MySQL database using your preferred client:**
   ```bash
   mysql -h cryptotrading-01-phantrunghieu0000-ad84.g.aivencloud.com \
         -P 20158 \
         -u avnadmin \
         -pAVNS_crRx8I7160cmzHy-vge \
         --ssl-mode=REQUIRED \
         --ssl-ca="path/to/your/ca.pem"
   ```

2. **Execute the schema file:**
   ```sql
   source database/mysql/001_InitialSchema_MySQL.sql
   ```

3. **Execute the seed data file:**
   ```sql
   source database/mysql/002_SeedData_MySQL.sql
   ```

### Option 2: EF Core Migrations (If IP access is allowed)
```bash
# Apply migrations
dotnet ef database update

# If you need to recreate migrations:
dotnet ef migrations remove --force
dotnet ef migrations add InitialMySqlMigration
dotnet ef database update
```

## Database Schema Structure

The MySQL schema uses table prefixes instead of schemas:
- `auth_*` tables (users, tokens, activity, audit logs)
- `market_*` tables (cryptocurrencies, prices, market stats)
- `portfolio_*` tables (watchlists, positions)
- `trading_*` tables (balances, bots, orders, trades, API keys)
- `billing_*` tables (subscriptions, payment history)
- `ops_*` tables (idempotency, outbox, inbox)

## Key MySQL Adaptations

### Data Type Changes
- `UNIQUEIDENTIFIER` → `CHAR(36)` (for UUIDs)
- `NVARCHAR` → `VARCHAR`
- `VARBINARY(MAX)` → `LONGBLOB`
- `DATETIME2(3)` → `DATETIME(3)`
- `BIT` → `BOOLEAN`
- `IDENTITY` → `AUTO_INCREMENT`

### Function Changes
- `SYSUTCDATETIME()` → `CURRENT_TIMESTAMP(3)`
- `NEWSEQUENTIALID()` → `UUID()`
- `HASHBYTES('SHA2_256', ...)` → `SHA2(..., 256)`

## Demo Users
After running the seed data, you'll have these test users (password: `Admin@123`):
- admin@cryptotrading.dev (Pro tier)
- nhat.an@cryptotrading.dev (Plus tier)
- huu.triet@cryptotrading.dev (Plus tier)
- trung.hieu@cryptotrading.dev (Plus tier)
- vu.hoang@cryptotrading.dev (Pro tier)
- dang.khoa@cryptotrading.dev (Plus tier)

## Testing the Setup

1. **Run the application:**
   ```bash
   dotnet run
   ```

2. **Check the API endpoints:**
   - Health check: `http://localhost:5000/health`
   - Swagger UI: `http://localhost:5000/swagger`
   - API root: `http://localhost:5000/`

## Troubleshooting

### Connection Issues
- Ensure your IP is whitelisted in Aiven console
- Verify SSL certificate path if using certificate-based auth
- Check firewall settings for port 20158

### Migration Issues
- Use the design-time factory if EF tools can't connect
- Run SQL scripts manually if migrations fail
- Verify database name matches connection string

## Security Notes
⚠️ **Important**: 
- Change default passwords before production
- Use proper password hashing in production (current seed uses simple SHA2)
- Secure your connection string in production environments
- Consider using Azure Key Vault or similar for secrets management

## Next Steps
1. Execute the SQL scripts on your MySQL database
2. Test the application connection
3. Update demo user passwords
4. Configure proper authentication/authorization
5. Set up production-ready logging and monitoring
