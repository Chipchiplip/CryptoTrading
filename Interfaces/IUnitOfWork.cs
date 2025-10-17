using CryptoTrading.Models;

namespace CryptoTrading.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<RefreshToken> RefreshTokens { get; }
    IRepository<Cryptocurrency> Cryptocurrencies { get; }
    IRepository<Order> Orders { get; }
    IRepository<UserWatchlist> UserWatchlists { get; }
    IRepository<WatchlistItem> WatchlistItems { get; }
    IRepository<Position> Positions { get; }
    IRepository<Balance> Balances { get; }
    IRepository<Bot> Bots { get; }
    IRepository<CryptoPrice> CryptoPrices { get; }
    IRepository<Trade> Trades { get; }
    IRepository<Subscription> Subscriptions { get; }
    IRepository<PaymentHistory> PaymentHistories { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
