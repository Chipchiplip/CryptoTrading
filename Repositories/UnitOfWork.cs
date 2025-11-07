using CryptoTrading.Data;
using CryptoTrading.Interfaces;
using CryptoTrading.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace CryptoTrading.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            // Auth
            Users = new Repository<User>(_context);
            LoginActivities = new Repository<LoginActivity>(_context);
            
            // Market
            Cryptocurrencies = new CryptocurrencyRepository(_context);
            CryptoPrices = new CryptoPriceRepository(_context);
            MarketStats = new Repository<MarketStat>(_context);
        }

        // Auth
        public IRepository<User> Users { get; private set; }
        public IRepository<LoginActivity> LoginActivities { get; private set; }
        
        // Market
        public ICryptocurrencyRepository Cryptocurrencies { get; private set; }
        public ICryptoPriceRepository CryptoPrices { get; private set; }
        public IRepository<MarketStat> MarketStats { get; private set; }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
