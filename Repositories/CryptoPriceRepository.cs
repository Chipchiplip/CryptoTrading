using CryptoTrading.Data;
using CryptoTrading.Interfaces;
using CryptoTrading.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoTrading.Repositories
{
    public interface ICryptoPriceRepository : IRepository<CryptoPrice>
    {
        Task<List<CryptoPrice>> GetLatestPricesAsync();
        Task<List<CryptoPrice>> GetPriceHistoryAsync(int cryptocurrencyId, int days = 7);
        Task<CryptoPrice?> GetLatestPriceAsync(int cryptocurrencyId);
    }

    public class CryptoPriceRepository : Repository<CryptoPrice>, ICryptoPriceRepository
    {
        public CryptoPriceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<CryptoPrice>> GetLatestPricesAsync()
        {
            // Get the latest price for each cryptocurrency
            return await _context.CryptoPrices
                .Include(cp => cp.Cryptocurrency)
                .GroupBy(cp => cp.CryptocurrencyId)
                .Select(g => g.OrderByDescending(cp => cp.CollectedAtUtc).FirstOrDefault()!)
                .Where(cp => cp != null)
                .ToListAsync();
        }

        public async Task<List<CryptoPrice>> GetPriceHistoryAsync(int cryptocurrencyId, int days = 7)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            return await _context.CryptoPrices
                .Where(cp => cp.CryptocurrencyId == cryptocurrencyId && cp.CollectedAtUtc >= startDate)
                .OrderBy(cp => cp.CollectedAtUtc)
                .ToListAsync();
        }

        public async Task<CryptoPrice?> GetLatestPriceAsync(int cryptocurrencyId)
        {
            return await _context.CryptoPrices
                .Where(cp => cp.CryptocurrencyId == cryptocurrencyId)
                .OrderByDescending(cp => cp.CollectedAtUtc)
                .FirstOrDefaultAsync();
        }
    }
}

