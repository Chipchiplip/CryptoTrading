using Microsoft.EntityFrameworkCore;
using CryptoTrading.Models;

namespace CryptoTrading.Services.Trading.Extensions
{
    /// <summary>
    /// Extensions for DbContext to support pessimistic locking (SELECT ... FOR UPDATE)
    /// </summary>
    public static class DbContextExtensions
    {
        /// <summary>
        /// Locks a wallet for update within a transaction (SELECT ... FOR UPDATE)
        /// </summary>
        public static async Task<Wallet?> LockWalletForUpdateAsync(
            this DbContext context,
            ulong walletId,
            CancellationToken cancellationToken = default)
        {
            // MySQL-specific SELECT ... FOR UPDATE
            var wallet = await context.Set<Wallet>()
                .FromSqlRaw("SELECT * FROM Wallets WHERE Id = {0} FOR UPDATE", walletId)
                .FirstOrDefaultAsync(cancellationToken);

            return wallet;
        }

        /// <summary>
        /// Locks multiple wallets for update within a transaction
        /// </summary>
        public static async Task<List<Wallet>> LockWalletsForUpdateAsync(
            this DbContext context,
            IEnumerable<ulong> walletIds,
            CancellationToken cancellationToken = default)
        {
            var ids = string.Join(",", walletIds);
            var wallets = await context.Set<Wallet>()
                .FromSqlRaw($"SELECT * FROM Wallets WHERE Id IN ({ids}) FOR UPDATE")
                .ToListAsync(cancellationToken);

            return wallets;
        }

        /// <summary>
        /// Locks an order for update within a transaction
        /// </summary>
        public static async Task<Order?> LockOrderForUpdateAsync(
            this DbContext context,
            ulong orderId,
            CancellationToken cancellationToken = default)
        {
            var order = await context.Set<Order>()
                .FromSqlRaw("SELECT * FROM Orders WHERE Id = {0} FOR UPDATE", orderId)
                .Include(o => o.Cryptocurrency)
                .FirstOrDefaultAsync(cancellationToken);

            return order;
        }

        /// <summary>
        /// Locks orders for a specific cryptocurrency for matching (SELECT ... FOR UPDATE)
        /// </summary>
        public static async Task<List<Order>> LockOrdersForMatchingAsync(
            this DbContext context,
            int cryptoId,
            string side,
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            // Lock orders for matching - this prevents race conditions in the matching engine
            var orders = await context.Set<Order>()
                .FromSqlRaw(@"
                    SELECT * FROM Orders 
                    WHERE CryptocurrencyId = {0} 
                    AND Side = {1} 
                    AND Type = 'LIMIT' 
                    AND (Status = 'NEW' OR Status = 'PARTIAL')
                    ORDER BY 
                        CASE WHEN {1} = 'BUY' THEN PriceUsd END DESC,
                        CASE WHEN {1} = 'SELL' THEN PriceUsd END ASC,
                        CreatedAt ASC
                    LIMIT {2}
                    FOR UPDATE",
                    cryptoId, side, limit)
                .ToListAsync(cancellationToken);

            return orders;
        }

        /// <summary>
        /// Gets the calculated balance for a wallet with a read lock
        /// </summary>
        public static async Task<decimal> GetLockedWalletBalanceAsync(
            this DbContext context,
            ulong walletId,
            CancellationToken cancellationToken = default)
        {
            // Calculate total balance from movements
            var totalBalance = await context.Set<WalletMovement>()
                .Where(m => m.WalletId == walletId)
                .SumAsync(m => m.Amount, cancellationToken);

            // Calculate locked balance from active holds
            var lockedBalance = await context.Set<OrderHold>()
                .Where(h => h.WalletId == walletId && h.ReleasedAt == null)
                .SumAsync(h => h.Amount, cancellationToken);

            return totalBalance - lockedBalance;
        }

        /// <summary>
        /// Check if client order ID already exists (for idempotency)
        /// </summary>
        public static async Task<ulong?> GetOrderIdByClientOrderIdAsync(
            this DbContext context,
            string clientOrderId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var idempotency = await context.Set<ClientOrderIdempotency>()
                .FirstOrDefaultAsync(i => i.ClientOrderId == clientOrderId && i.UserId == userId, 
                    cancellationToken);

            return idempotency?.OrderId;
        }

        /// <summary>
        /// Records client order ID for idempotency
        /// </summary>
        public static async Task RecordClientOrderIdAsync(
            this DbContext context,
            string clientOrderId,
            int userId,
            ulong orderId,
            CancellationToken cancellationToken = default)
        {
            var idempotency = new ClientOrderIdempotency
            {
                ClientOrderId = clientOrderId,
                UserId = userId,
                OrderId = orderId,
                CreatedAt = DateTime.UtcNow
            };

            context.Set<ClientOrderIdempotency>().Add(idempotency);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}

