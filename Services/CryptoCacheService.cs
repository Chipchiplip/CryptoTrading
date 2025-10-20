using CryptoTrading.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoTrading.Services
{
    public interface ICryptoCacheService
    {
        bool TryGetCryptoData(out List<Crypto>? data);
        void SetCryptoData(List<Crypto> data);
        void ClearAllCache();
        bool TryGetMarketStats(out MarketStats? stats);
        void SetMarketStats(MarketStats stats);
    }

    public class CryptoCacheService : ICryptoCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CryptoCacheService> _logger;
        private const string CRYPTO_DATA_KEY = "crypto_data";
        private const string MARKET_STATS_KEY = "market_stats";
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        public CryptoCacheService(IMemoryCache cache, ILogger<CryptoCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public bool TryGetCryptoData(out List<Crypto>? data)
        {
            if (_cache.TryGetValue(CRYPTO_DATA_KEY, out List<Crypto>? cachedData))
            {
                data = cachedData;
                _logger.LogDebug("Retrieved crypto data from cache");
                return true;
            }
            
            data = null;
            return false;
        }

        public void SetCryptoData(List<Crypto> data)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration,
                Priority = CacheItemPriority.High
            };
            
            _cache.Set(CRYPTO_DATA_KEY, data, cacheOptions);
            _logger.LogInformation("Cached {Count} crypto coins", data.Count);
        }

        public bool TryGetMarketStats(out MarketStats? stats)
        {
            if (_cache.TryGetValue(MARKET_STATS_KEY, out MarketStats? cachedStats))
            {
                stats = cachedStats;
                _logger.LogDebug("Retrieved market stats from cache");
                return true;
            }
            
            stats = null;
            return false;
        }

        public void SetMarketStats(MarketStats stats)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration,
                Priority = CacheItemPriority.High
            };
            
            _cache.Set(MARKET_STATS_KEY, stats, cacheOptions);
            _logger.LogInformation("Cached market stats");
        }

        public void ClearAllCache()
        {
            _cache.Remove(CRYPTO_DATA_KEY);
            _cache.Remove(MARKET_STATS_KEY);
            _logger.LogInformation("Cleared all crypto cache");
        }
    }
}
