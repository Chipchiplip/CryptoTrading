using CryptoTrading.Data;
using CryptoTrading.Interfaces;
using CryptoTrading.Models;
using CryptoTrading.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CryptoTrading.Services
{
    public class WatchlistService : IWatchlistService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICoinGeckoService _coinGeckoService;

        public WatchlistService(ApplicationDbContext context, ICoinGeckoService coinGeckoService)
        {
            _context = context;
            _coinGeckoService = coinGeckoService;
        }

        public async Task<WatchlistDto> CreateWatchlistAsync(int userId, CreateWatchlistDto dto)
        {
            // Ensure user doesn't exceed quota
            if (!await CanCreateMoreWatchlistsAsync(userId))
            {
                throw new InvalidOperationException("Watchlist quota exceeded");
            }

            // If this is set as default, unset other defaults
            if (dto.IsDefault)
            {
                await UnsetDefaultWatchlistsAsync(userId);
            }

            var watchlist = new Watchlist
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = dto.Name,
                IsDefault = dto.IsDefault,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Watchlists.Add(watchlist);
            await _context.SaveChangesAsync();

            return await GetWatchlistAsync(userId, watchlist.Id);
        }

        public async Task<List<WatchlistSummaryDto>> GetAllWatchlistsAsync(int userId)
        {
            await EnsureDefaultWatchlistExistsAsync(userId);

            var watchlists = await _context.Watchlists
                .Where(w => w.UserId == userId)
                .Include(w => w.Items)
                .OrderByDescending(w => w.IsDefault)
                .ThenBy(w => w.Name)
                .Select(w => new WatchlistSummaryDto(
                    w.Id,
                    w.Name,
                    w.IsDefault,
                    w.Items.Count,
                    w.CreatedAt,
                    w.UpdatedAt
                ))
                .ToListAsync();

            return watchlists;
        }

        public async Task<WatchlistDto> GetWatchlistAsync(int userId, Guid watchlistId)
        {
            var watchlist = await _context.Watchlists
                .Include(w => w.Items)
                .FirstOrDefaultAsync(w => w.Id == watchlistId && w.UserId == userId);

            if (watchlist == null)
                throw new ArgumentException("Watchlist not found");

            var coins = new List<WatchlistCoinDto>();
            
            if (watchlist.Items.Any())
            {
                var symbols = watchlist.Items.Select(i => i.CoinSymbol).ToList();
                var allCoinData = await _coinGeckoService.GetMarketDataAsync();

                coins = watchlist.Items.Select(item =>
                {
                    var coinInfo = allCoinData.FirstOrDefault(c => 
                        c.Symbol.Equals(item.CoinSymbol, StringComparison.OrdinalIgnoreCase));
                    
                    return new WatchlistCoinDto(
                        item.CoinSymbol,
                        coinInfo?.Name ?? item.CoinSymbol,
                        "", // Image URL - not available in current Crypto model
                        coinInfo?.CurrentPrice ?? 0,
                        coinInfo?.PriceChange24h ?? 0,
                        coinInfo?.PriceChangePercentage24h ?? 0,
                        item.AddedAt
                    );
                }).ToList();
            }

            return new WatchlistDto(
                watchlist.Id,
                watchlist.Name,
                watchlist.IsDefault,
                watchlist.Items.Count,
                watchlist.CreatedAt,
                watchlist.UpdatedAt,
                coins
            );
        }

        public async Task<WatchlistDto> RenameWatchlistAsync(int userId, Guid watchlistId, RenameWatchlistDto dto)
        {
            var watchlist = await _context.Watchlists
                .FirstOrDefaultAsync(w => w.Id == watchlistId && w.UserId == userId);

            if (watchlist == null)
                throw new ArgumentException("Watchlist not found");

            watchlist.Name = dto.Name;
            watchlist.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetWatchlistAsync(userId, watchlistId);
        }

        public async Task<bool> DeleteWatchlistAsync(int userId, Guid watchlistId)
        {
            var watchlist = await _context.Watchlists
                .FirstOrDefaultAsync(w => w.Id == watchlistId && w.UserId == userId);

            if (watchlist == null)
                return false;

            // Don't allow deleting the last watchlist
            var watchlistCount = await _context.Watchlists.CountAsync(w => w.UserId == userId);
            if (watchlistCount <= 1)
                throw new InvalidOperationException("Cannot delete the last watchlist");

            _context.Watchlists.Remove(watchlist);
            await _context.SaveChangesAsync();

            // If deleted watchlist was default, make another one default
            if (watchlist.IsDefault)
            {
                var firstWatchlist = await _context.Watchlists
                    .FirstOrDefaultAsync(w => w.UserId == userId);
                
                if (firstWatchlist != null)
                {
                    firstWatchlist.IsDefault = true;
                    await _context.SaveChangesAsync();
                }
            }

            return true;
        }

        public async Task<bool> AddCoinToDefaultWatchlistAsync(int userId, string coinSymbol)
        {
            await EnsureDefaultWatchlistExistsAsync(userId);

            var defaultWatchlist = await _context.Watchlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.IsDefault);

            if (defaultWatchlist == null)
                return false;

            return await AddCoinToWatchlistAsync(userId, defaultWatchlist.Id, coinSymbol);
        }

        public async Task<bool> AddCoinToWatchlistAsync(int userId, Guid watchlistId, string coinSymbol)
        {
            var watchlist = await _context.Watchlists
                .Include(w => w.Items)
                .FirstOrDefaultAsync(w => w.Id == watchlistId && w.UserId == userId);

            if (watchlist == null)
                return false;

            // Check if coin already exists in watchlist
            if (watchlist.Items.Any(i => i.CoinSymbol.Equals(coinSymbol, StringComparison.OrdinalIgnoreCase)))
                return false;

            var watchlistItem = new WatchlistItem
            {
                Id = Guid.NewGuid(),
                WatchlistId = watchlistId,
                CoinSymbol = coinSymbol.ToUpper(),
                AddedAt = DateTime.UtcNow
            };

            _context.WatchlistItems.Add(watchlistItem);
            watchlist.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveCoinFromWatchlistAsync(int userId, Guid watchlistId, string coinSymbol)
        {
            var watchlistItem = await _context.WatchlistItems
                .Include(wi => wi.Watchlist)
                .FirstOrDefaultAsync(wi => 
                    wi.WatchlistId == watchlistId && 
                    wi.Watchlist.UserId == userId &&
                    wi.CoinSymbol.Equals(coinSymbol, StringComparison.OrdinalIgnoreCase));

            if (watchlistItem == null)
                return false;

            _context.WatchlistItems.Remove(watchlistItem);
            watchlistItem.Watchlist.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<WatchlistRealtimeUpdateDto> GetWatchlistRealtimeUpdatesAsync(Guid watchlistId)
        {
            var watchlist = await _context.Watchlists
                .Include(w => w.Items)
                .FirstOrDefaultAsync(w => w.Id == watchlistId);

            if (watchlist == null)
                throw new ArgumentException("Watchlist not found");

            var symbols = watchlist.Items.Select(i => i.CoinSymbol).ToList();
            if (!symbols.Any())
            {
                return new WatchlistRealtimeUpdateDto(watchlistId, new List<CoinPriceUpdateDto>());
            }

            var allCoinData = await _coinGeckoService.GetMarketDataAsync();
            var updates = symbols.Select(symbol =>
            {
                var coinInfo = allCoinData.FirstOrDefault(c => 
                    c.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
                
                return new CoinPriceUpdateDto(
                    symbol,
                    coinInfo?.CurrentPrice ?? 0,
                    coinInfo?.PriceChange24h ?? 0,
                    coinInfo?.PriceChangePercentage24h ?? 0,
                    DateTime.UtcNow
                );
            }).ToList();

            return new WatchlistRealtimeUpdateDto(watchlistId, updates);
        }

        public async Task<WatchlistQuotaDto> GetWatchlistQuotaAsync(int userId)
        {
            var currentCount = await _context.Watchlists.CountAsync(w => w.UserId == userId);
            
            // TODO: Get user subscription tier from User entity
            // For now, assume basic tier
            var subscriptionTier = "Basic";
            var maxAllowed = subscriptionTier switch
            {
                "Basic" => 3,
                "Plus" => 10,
                "Pro" => 50,
                _ => 1
            };

            return new WatchlistQuotaDto(
                currentCount,
                maxAllowed,
                subscriptionTier,
                currentCount < maxAllowed
            );
        }

        public async Task<WatchlistDto> GetDefaultWatchlistAsync(int userId)
        {
            await EnsureDefaultWatchlistExistsAsync(userId);

            var defaultWatchlist = await _context.Watchlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.IsDefault);

            if (defaultWatchlist == null)
                throw new InvalidOperationException("Default watchlist not found");

            return await GetWatchlistAsync(userId, defaultWatchlist.Id);
        }

        public async Task<bool> EnsureDefaultWatchlistExistsAsync(int userId)
        {
            var hasDefault = await _context.Watchlists
                .AnyAsync(w => w.UserId == userId && w.IsDefault);

            if (!hasDefault)
            {
                var hasAnyWatchlist = await _context.Watchlists
                    .AnyAsync(w => w.UserId == userId);

                if (hasAnyWatchlist)
                {
                    // Make first watchlist default
                    var firstWatchlist = await _context.Watchlists
                        .FirstOrDefaultAsync(w => w.UserId == userId);
                    
                    if (firstWatchlist != null)
                    {
                        firstWatchlist.IsDefault = true;
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // Create default watchlist
                    var defaultWatchlist = new Watchlist
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Name = "My Watchlist",
                        IsDefault = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Watchlists.Add(defaultWatchlist);
                    await _context.SaveChangesAsync();
                }
            }

            return true;
        }

        public async Task<bool> CanCreateMoreWatchlistsAsync(int userId)
        {
            var quota = await GetWatchlistQuotaAsync(userId);
            return quota.CanCreateMore;
        }

        private async Task UnsetDefaultWatchlistsAsync(int userId)
        {
            var defaultWatchlists = await _context.Watchlists
                .Where(w => w.UserId == userId && w.IsDefault)
                .ToListAsync();

            foreach (var watchlist in defaultWatchlists)
            {
                watchlist.IsDefault = false;
            }

            if (defaultWatchlists.Any())
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
