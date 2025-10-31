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
            // With UserWatchlist schema, we only support a single implicit watchlist per user
            // Return the default watchlist
            return await GetDefaultWatchlistAsync(userId);
        }

        public async Task<List<WatchlistSummaryDto>> GetAllWatchlistsAsync(int userId)
        {
            // With UserWatchlist schema, we only have one implicit watchlist
            var defaultWatchlist = await GetDefaultWatchlistAsync(userId);
            
            return new List<WatchlistSummaryDto>
            {
                new WatchlistSummaryDto(
                    defaultWatchlist.Id,
                    defaultWatchlist.Name,
                    defaultWatchlist.IsDefault,
                    defaultWatchlist.CoinCount,
                    defaultWatchlist.CreatedAt,
                    defaultWatchlist.UpdatedAt
                )
            };
        }

        public async Task<WatchlistDto> GetWatchlistAsync(int userId, Guid watchlistId)
        {
            // Since we only have UserWatchlist, return the default watchlist
            return await GetDefaultWatchlistAsync(userId);
        }

        public async Task<WatchlistDto> RenameWatchlistAsync(int userId, Guid watchlistId, RenameWatchlistDto dto)
        {
            // With UserWatchlist, we can't rename since there's no Watchlist entity
            // Just return the default watchlist
            return await GetDefaultWatchlistAsync(userId);
        }

        public async Task<bool> DeleteWatchlistAsync(int userId, Guid watchlistId)
        {
            // With UserWatchlist, we can't delete the watchlist itself since it's implicit
            // But we can clear all coins from the watchlist
            var userWatchlists = await _context.UserWatchlists
                .Where(uw => uw.UserId == userId)
                .ToListAsync();

            _context.UserWatchlists.RemoveRange(userWatchlists);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> AddCoinToDefaultWatchlistAsync(int userId, string coinSymbol)
        {
            // Find cryptocurrency by symbol
            var crypto = await _context.Cryptocurrencies
                .FirstOrDefaultAsync(c => c.Symbol.ToUpper() == coinSymbol.ToUpper());

            if (crypto == null)
                return false;

            // Check if already in watchlist
            var exists = await _context.UserWatchlists
                .AnyAsync(uw => uw.UserId == userId && uw.CryptocurrencyId == crypto.Id);

            if (exists)
                return false;

            // Add to UserWatchlist
            var userWatchlist = new UserWatchlist
            {
                UserId = userId,
                CryptocurrencyId = crypto.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserWatchlists.Add(userWatchlist);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddCoinToWatchlistAsync(int userId, Guid watchlistId, string coinSymbol)
        {
            // Since we only have UserWatchlist (many-to-many), treat any watchlistId as the default
            // Just add to UserWatchlist
            return await AddCoinToDefaultWatchlistAsync(userId, coinSymbol);
        }

        public async Task<bool> RemoveCoinFromWatchlistAsync(int userId, Guid watchlistId, string coinSymbol)
        {
            // Find cryptocurrency by symbol
            var crypto = await _context.Cryptocurrencies
                .FirstOrDefaultAsync(c => c.Symbol.ToUpper() == coinSymbol.ToUpper());

            if (crypto == null)
                return false;

            // Remove from UserWatchlist
            var userWatchlist = await _context.UserWatchlists
                .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.CryptocurrencyId == crypto.Id);

            if (userWatchlist == null)
                return false;

            _context.UserWatchlists.Remove(userWatchlist);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<WatchlistRealtimeUpdateDto> GetWatchlistRealtimeUpdatesAsync(Guid watchlistId)
        {
            // Since we don't have actual watchlist IDs, we'll get updates for the default watchlist
            // We can determine user from context, but for now we'll use a simplified approach
            // Get all UserWatchlist entries and create updates
            var userWatchlistEntries = await _context.UserWatchlists
                .Include(uw => uw.Cryptocurrency)
                .ToListAsync();

            if (!userWatchlistEntries.Any())
            {
                return new WatchlistRealtimeUpdateDto(watchlistId, new List<CoinPriceUpdateDto>());
            }

            var symbols = userWatchlistEntries
                .Select(uw => uw.Cryptocurrency.Symbol.ToUpper())
                .Distinct()
                .ToList();

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
            // Count coins in UserWatchlist instead of watchlists
            var currentCount = await _context.UserWatchlists.CountAsync(uw => uw.UserId == userId);
            
            // TODO: Get user subscription tier from User entity
            // For now, assume basic tier
            var subscriptionTier = "Basic";
            // For coins in watchlist, allow more
            var maxAllowed = subscriptionTier switch
            {
                "Basic" => 50,
                "Plus" => 100,
                "Pro" => 500,
                _ => 25
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
            // Get all UserWatchlist entries for this user
            var userWatchlistEntries = await _context.UserWatchlists
                .Where(uw => uw.UserId == userId)
                .Include(uw => uw.Cryptocurrency)
                .ToListAsync();

            var coinSymbols = userWatchlistEntries
                .Select(uw => uw.Cryptocurrency.Symbol.ToUpper())
                .Distinct()
                .ToList();

            var coins = new List<WatchlistCoinDto>();
            
            if (coinSymbols.Any())
            {
                var allCoinData = await _coinGeckoService.GetMarketDataAsync();
                
                foreach (var entry in userWatchlistEntries)
                {
                    var coinInfo = allCoinData.FirstOrDefault(c => 
                        c.Symbol.Equals(entry.Cryptocurrency.Symbol, StringComparison.OrdinalIgnoreCase));
                    
                    coins.Add(new WatchlistCoinDto(
                        entry.Cryptocurrency.Symbol.ToUpper(),
                        coinInfo?.Name ?? entry.Cryptocurrency.Name,
                        entry.Cryptocurrency.IconUrl ?? "",
                        coinInfo?.CurrentPrice ?? 0,
                        coinInfo?.PriceChange24h ?? 0,
                        coinInfo?.PriceChangePercentage24h ?? 0,
                        entry.CreatedAt
                    ));
                }
            }

            // Return a virtual watchlist DTO (since we don't have a Watchlist entity)
            return new WatchlistDto(
                Guid.Empty, // No actual watchlist ID in UserWatchlist
                "My Watchlist",
                true, // Always default
                coins.Count,
                DateTime.UtcNow,
                DateTime.UtcNow,
                coins
            );
        }

        public async Task<bool> EnsureDefaultWatchlistExistsAsync(int userId)
        {
            // With UserWatchlist, we don't need to create a watchlist entity
            // The watchlist is implicit - just return true
            return true;
        }

        public async Task<bool> CanCreateMoreWatchlistsAsync(int userId)
        {
            var quota = await GetWatchlistQuotaAsync(userId);
            return quota.CanCreateMore;
        }

        private async Task UnsetDefaultWatchlistsAsync(int userId)
        {
            // With UserWatchlist schema, we don't have multiple watchlists
            // This method is no longer needed
            await Task.CompletedTask;
        }
    }
}
