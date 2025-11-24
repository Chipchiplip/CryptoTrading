# MySQL Configuration Guidelines

## 🎯 Overview

This document provides MySQL-specific configuration guidelines for the CryptoTrading platform, optimized for Aiven Cloud MySQL deployment.

## 🔧 Database Configuration

### Engine & Charset Standards
```sql
-- All tables use InnoDB engine with utf8mb4_0900_ai_ci collation
ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
```

### Connection String Format
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-mysql-host;Port=20158;Database=crypto_trading;User ID=avnadmin;Password=your-password;SslMode=Required;"
  }
}
```

## 📊 Performance Optimization

### Composite Indexes
The following composite indexes are implemented for optimal query performance:

#### Orders Table
```sql
-- Primary performance indexes
KEY `IX_Orders_CryptocurrencyId_Status` (`CryptocurrencyId`,`Status`)
KEY `IX_Orders_UserId_CreatedAt` (`UserId`,`CreatedAt`)

-- Additional recommended indexes
ALTER TABLE `Orders` ADD INDEX `IX_Orders_UserId_Status_CreatedAt` (`UserId`, `Status`, `CreatedAt`);
ALTER TABLE `Orders` ADD INDEX `IX_Orders_CryptocurrencyId_CreatedAt` (`CryptocurrencyId`, `CreatedAt`);
```

#### Trades Table
```sql
-- Current indexes
KEY `IX_Trades_CryptocurrencyId` (`CryptocurrencyId`)
KEY `IX_Trades_OrderId_CreatedAt` (`OrderId`,`CreatedAt`)

-- Additional recommended indexes
ALTER TABLE `Trades` ADD INDEX `IX_Trades_UserId_CreatedAt` (`UserId`, `CreatedAt`);
ALTER TABLE `Trades` ADD INDEX `IX_Trades_CryptocurrencyId_CreatedAt` (`CryptocurrencyId`, `CreatedAt`);
```

#### WalletMovements Table
```sql
-- Current indexes
KEY `IX_WalletMovements_WalletId_CreatedAt` (`WalletId`,`CreatedAt`)

-- Additional recommended indexes
ALTER TABLE `WalletMovements` ADD INDEX `IX_WalletMovements_RefType_RefId` (`RefType`, `RefId`);
```

#### CryptoPrices Table
```sql
-- Optimized for time-series queries
KEY `IX_CryptoPrices_CryptocurrencyId_CollectedAtUtc` (`CryptocurrencyId`,`CollectedAtUtc`)
```

## ⚙️ MySQL Server Configuration

### Recommended my.cnf Settings for Aiven
```ini
[mysqld]
# Connection & Timeout Settings
innodb_lock_wait_timeout = 50
innodb_rollback_on_timeout = ON
max_connections = 200
wait_timeout = 28800

# Performance Settings
innodb_buffer_pool_size = 1G
innodb_log_file_size = 256M
innodb_flush_log_at_trx_commit = 1

# Character Set
character_set_server = utf8mb4
collation_server = utf8mb4_0900_ai_ci
```

## 🔄 Transaction Management

### Repository Retry Logic
Implement retry logic for handling lock timeouts:

```csharp
public class BaseRepository
{
    protected async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try 
            { 
                return await operation(); 
            }
            catch (MySqlException ex) when (ex.Number == 1205) // Lock wait timeout
            {
                if (i == maxRetries - 1) throw;
                
                // Exponential backoff
                var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, i));
                await Task.Delay(delay);
            }
        }
        
        throw new InvalidOperationException("Should not reach here");
    }
}
```

### Transaction Isolation Levels
```csharp
// For read-heavy operations
using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

// For critical financial operations
using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
```

## 💾 Backup & Restore Strategy

### Aiven Cloud Backup
```bash
#!/bin/bash
# Automated backup script for Aiven MySQL

MYSQL_HOST="your-host.aivencloud.com"
MYSQL_PORT="20158"
MYSQL_USER="avnadmin"
MYSQL_PASSWORD="your-password"
DATABASE="crypto_trading"
BACKUP_DIR="/backups"
DATE=$(date +%Y%m%d_%H%M%S)

# Create backup with SSL
mysqldump \
  --host=$MYSQL_HOST \
  --port=$MYSQL_PORT \
  --user=$MYSQL_USER \
  --password=$MYSQL_PASSWORD \
  --single-transaction \
  --routines \
  --triggers \
  --ssl-mode=REQUIRED \
  --result-file="$BACKUP_DIR/crypto_trading_$DATE.sql" \
  $DATABASE

# Compress backup
gzip "$BACKUP_DIR/crypto_trading_$DATE.sql"

# Clean old backups (keep last 30 days)
find $BACKUP_DIR -name "crypto_trading_*.sql.gz" -mtime +30 -delete
```

### Restore Process
```bash
# Restore from backup
mysql \
  --host=$MYSQL_HOST \
  --port=$MYSQL_PORT \
  --user=$MYSQL_USER \
  --password=$MYSQL_PASSWORD \
  --ssl-mode=REQUIRED \
  $DATABASE < backup_file.sql
```

## 🔍 Monitoring & Maintenance

### Key Metrics to Monitor
```sql
-- Connection usage
SHOW STATUS LIKE 'Threads_connected';
SHOW STATUS LIKE 'Max_used_connections';

-- Lock information
SHOW ENGINE INNODB STATUS;

-- Slow queries
SHOW STATUS LIKE 'Slow_queries';

-- Buffer pool efficiency
SHOW STATUS LIKE 'Innodb_buffer_pool_read_requests';
SHOW STATUS LIKE 'Innodb_buffer_pool_reads';
```

### Regular Maintenance Tasks
```sql
-- Analyze table statistics (weekly)
ANALYZE TABLE Orders, Trades, WalletMovements, CryptoPrices;

-- Check table integrity (monthly)
CHECK TABLE Orders, Trades, WalletMovements, CryptoPrices;

-- Optimize tables if needed (quarterly)
OPTIMIZE TABLE Orders, Trades, WalletMovements, CryptoPrices;
```

## 🚨 Common Issues & Solutions

### Issue: Lock Wait Timeout
```
Error: Lock wait timeout exceeded; try restarting transaction
```
**Solution**: Implement retry logic and optimize queries to reduce lock duration.

### Issue: Connection Pool Exhaustion
```
Error: Too many connections
```
**Solution**: Configure connection pooling in Entity Framework:
```csharp
services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, serverVersion, mysqlOptions =>
    {
        mysqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    });
}, ServiceLifetime.Scoped);
```

### Issue: Charset/Collation Mismatch
**Solution**: Ensure all tables and columns use utf8mb4_0900_ai_ci:
```sql
ALTER TABLE table_name CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
```

## 📈 Performance Best Practices

1. **Use Composite Indexes**: Always create indexes that match your query patterns
2. **Limit Result Sets**: Use LIMIT and proper WHERE clauses
3. **Avoid SELECT ***: Only select needed columns
4. **Use Connection Pooling**: Configure appropriate pool sizes
5. **Monitor Slow Queries**: Enable and regularly review slow query log
6. **Regular Statistics Updates**: Keep table statistics current with ANALYZE TABLE

## 🔐 Security Considerations

1. **SSL/TLS**: Always use SSL connections in production
2. **Least Privilege**: Grant minimal required permissions
3. **Regular Updates**: Keep MySQL version updated
4. **Audit Logging**: Enable audit logs for sensitive operations
5. **Backup Encryption**: Encrypt backup files

## 📚 Additional Resources

- [MySQL 8.0 Reference Manual](https://dev.mysql.com/doc/refman/8.0/en/)
- [Pomelo Entity Framework Core Provider](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)
- [Aiven MySQL Documentation](https://docs.aiven.io/docs/products/mysql)
