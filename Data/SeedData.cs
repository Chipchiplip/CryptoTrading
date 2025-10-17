using Microsoft.EntityFrameworkCore;
using CryptoTrading.Models;

namespace CryptoTrading.Data;

public static class SeedData
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Seed Cryptocurrencies
        if (!await context.Cryptocurrencies.AnyAsync())
        {
            var cryptocurrencies = new[]
            {
                new Cryptocurrency { Id = 1, CoinGeckoId = "bitcoin", Symbol = "BTC", Name = "Bitcoin", MarketCapRank = 1, IsActive = true },
                new Cryptocurrency { Id = 2, CoinGeckoId = "ethereum", Symbol = "ETH", Name = "Ethereum", MarketCapRank = 2, IsActive = true },
                new Cryptocurrency { Id = 3, CoinGeckoId = "binancecoin", Symbol = "BNB", Name = "BNB", MarketCapRank = 3, IsActive = true },
                new Cryptocurrency { Id = 4, CoinGeckoId = "solana", Symbol = "SOL", Name = "Solana", MarketCapRank = 4, IsActive = true },
                new Cryptocurrency { Id = 5, CoinGeckoId = "ripple", Symbol = "XRP", Name = "XRP", MarketCapRank = 5, IsActive = true },
                new Cryptocurrency { Id = 6, CoinGeckoId = "usd-coin", Symbol = "USDC", Name = "USD Coin", MarketCapRank = 6, IsActive = true },
                new Cryptocurrency { Id = 7, CoinGeckoId = "staked-ether", Symbol = "STETH", Name = "Lido Staked Ether", MarketCapRank = 7, IsActive = true },
                new Cryptocurrency { Id = 8, CoinGeckoId = "cardano", Symbol = "ADA", Name = "Cardano", MarketCapRank = 8, IsActive = true },
                new Cryptocurrency { Id = 9, CoinGeckoId = "avalanche-2", Symbol = "AVAX", Name = "Avalanche", MarketCapRank = 9, IsActive = true },
                new Cryptocurrency { Id = 10, CoinGeckoId = "dogecoin", Symbol = "DOGE", Name = "Dogecoin", MarketCapRank = 10, IsActive = true }
            };

            context.Cryptocurrencies.AddRange(cryptocurrencies);
            await context.SaveChangesAsync();
        }

        // Seed Demo User
        if (!await context.Users.AnyAsync())
        {
            var demoUser = new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "demo@cryptotrading.com",
                PasswordHash = "$2a$11$example.hash.for.demo.user.password", // This should be properly hashed
                FirstName = "Demo",
                LastName = "User",
                IsEmailVerified = true,
                SubscriptionTier = 1 // Plus tier
            };

            context.Users.Add(demoUser);
            await context.SaveChangesAsync();

            // Seed demo user's default watchlist
            var defaultWatchlist = new UserWatchlist
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                UserId = demoUser.Id,
                Name = "My Watchlist",
                IsDefault = true
            };

            context.UserWatchlists.Add(defaultWatchlist);
            await context.SaveChangesAsync();

            // Add some cryptocurrencies to the watchlist
            var watchlistItems = new[]
            {
                new WatchlistItem { WatchlistId = defaultWatchlist.Id, CryptocurrencyId = 1, SortOrder = 1 }, // BTC
                new WatchlistItem { WatchlistId = defaultWatchlist.Id, CryptocurrencyId = 2, SortOrder = 2 }, // ETH
                new WatchlistItem { WatchlistId = defaultWatchlist.Id, CryptocurrencyId = 4, SortOrder = 3 }, // SOL
                new WatchlistItem { WatchlistId = defaultWatchlist.Id, CryptocurrencyId = 8, SortOrder = 4 }  // ADA
            };

            context.WatchlistItems.AddRange(watchlistItems);
            await context.SaveChangesAsync();

            // Seed demo balances
            var balances = new[]
            {
                new Balance { UserId = demoUser.Id, CryptocurrencyId = 6, Amount = 10000m }, // 10,000 USDC
                new Balance { UserId = demoUser.Id, CryptocurrencyId = 1, Amount = 0.5m },   // 0.5 BTC
                new Balance { UserId = demoUser.Id, CryptocurrencyId = 2, Amount = 5m }      // 5 ETH
            };

            context.Balances.AddRange(balances);
            await context.SaveChangesAsync();
        }
    }
}
