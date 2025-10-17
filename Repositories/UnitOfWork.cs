using Microsoft.EntityFrameworkCore.Storage;
using CryptoTrading.Data;
using CryptoTrading.Interfaces;
using CryptoTrading.Models;

namespace CryptoTrading.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    // Repositories
    private IRepository<User>? _users;
    private IRepository<RefreshToken>? _refreshTokens;
    private IRepository<Cryptocurrency>? _cryptocurrencies;
    private IRepository<Order>? _orders;
    private IRepository<UserWatchlist>? _userWatchlists;
    private IRepository<WatchlistItem>? _watchlistItems;
    private IRepository<Position>? _positions;
    private IRepository<Balance>? _balances;
    private IRepository<Bot>? _bots;
    private IRepository<CryptoPrice>? _cryptoPrices;
    private IRepository<Trade>? _trades;
    private IRepository<Subscription>? _subscriptions;
    private IRepository<PaymentHistory>? _paymentHistories;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<RefreshToken> RefreshTokens => _refreshTokens ??= new Repository<RefreshToken>(_context);
    public IRepository<Cryptocurrency> Cryptocurrencies => _cryptocurrencies ??= new Repository<Cryptocurrency>(_context);
    public IRepository<Order> Orders => _orders ??= new Repository<Order>(_context);
    public IRepository<UserWatchlist> UserWatchlists => _userWatchlists ??= new Repository<UserWatchlist>(_context);
    public IRepository<WatchlistItem> WatchlistItems => _watchlistItems ??= new Repository<WatchlistItem>(_context);
    public IRepository<Position> Positions => _positions ??= new Repository<Position>(_context);
    public IRepository<Balance> Balances => _balances ??= new Repository<Balance>(_context);
    public IRepository<Bot> Bots => _bots ??= new Repository<Bot>(_context);
    public IRepository<CryptoPrice> CryptoPrices => _cryptoPrices ??= new Repository<CryptoPrice>(_context);
    public IRepository<Trade> Trades => _trades ??= new Repository<Trade>(_context);
    public IRepository<Subscription> Subscriptions => _subscriptions ??= new Repository<Subscription>(_context);
    public IRepository<PaymentHistory> PaymentHistories => _paymentHistories ??= new Repository<PaymentHistory>(_context);

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
