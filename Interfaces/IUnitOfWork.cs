using CryptoTrading.Models;

namespace CryptoTrading.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<User> Users { get; }
        // Thêm các repository khác khi cần
        // IRepository<Portfolio> Portfolios { get; }
        // IRepository<Order> Orders { get; }
        
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
