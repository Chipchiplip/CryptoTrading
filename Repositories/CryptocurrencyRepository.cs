using CryptoTrading.Data;
using CryptoTrading.Interfaces;
using CryptoTrading.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoTrading.Repositories
{
    public interface ICryptocurrencyRepository : IRepository<Cryptocurrency>
    {
        Task<Cryptocurrency?> GetBySymbolAsync(string symbol);
        Task<Cryptocurrency?> GetByCoinGeckoIdAsync(string coinGeckoId);
        Task<List<Cryptocurrency>> GetActiveAsync();
        Task<bool> ExistsBySymbolAsync(string symbol);
    }

    public class CryptocurrencyRepository : Repository<Cryptocurrency>, ICryptocurrencyRepository
    {
        public CryptocurrencyRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Cryptocurrency?> GetBySymbolAsync(string symbol)
        {
            return await _context.Cryptocurrencies
                .FirstOrDefaultAsync(c => c.Symbol.ToLower() == symbol.ToLower());
        }

        public async Task<Cryptocurrency?> GetByCoinGeckoIdAsync(string coinGeckoId)
        {
            return await _context.Cryptocurrencies
                .FirstOrDefaultAsync(c => c.CoinGeckoId == coinGeckoId);
        }

        public async Task<List<Cryptocurrency>> GetActiveAsync()
        {
            return await _context.Cryptocurrencies
                .Where(c => c.IsActive)
                .OrderBy(c => c.MarketCapRank)
                .ToListAsync();
        }

        public async Task<bool> ExistsBySymbolAsync(string symbol)
        {
            return await _context.Cryptocurrencies
                .AnyAsync(c => c.Symbol.ToLower() == symbol.ToLower());
        }
    }
}

