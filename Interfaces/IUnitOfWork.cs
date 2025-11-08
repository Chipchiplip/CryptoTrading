using CryptoTrading.Models;
using CryptoTrading.Repositories;

namespace CryptoTrading.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Auth
        IRepository<User> Users { get; }
        
        // Market
        ICryptocurrencyRepository Cryptocurrencies { get; }
        ICryptoPriceRepository CryptoPrices { get; }
        IRepository<MarketStat> MarketStats { get; }

        IRepository<LoginActivity> LoginActivities { get; }
        
        // Future repositories
        // IRepository<Portfolio> Portfolios { get; }
        // IRepository<Order> Orders { get; }
        
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
