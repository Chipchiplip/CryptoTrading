using CryptoTrading.Interfaces;
using CryptoTrading.Models;

namespace CryptoTrading.Services
{
    public interface ICryptoDataSyncService
    {
        Task SyncCryptocurrenciesAsync();
        Task SyncPricesAsync();
        Task SyncMarketStatsAsync();
    }

    public class CryptoDataSyncService : ICryptoDataSyncService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICoinGeckoService _coinGeckoService;
        private readonly ILogger<CryptoDataSyncService> _logger;

        public CryptoDataSyncService(
            IUnitOfWork unitOfWork,
            ICoinGeckoService coinGeckoService,
            ILogger<CryptoDataSyncService> logger)
        {
            _unitOfWork = unitOfWork;
            _coinGeckoService = coinGeckoService;
            _logger = logger;
        }

        public async Task SyncCryptocurrenciesAsync()
        {
            try
            {
                _logger.LogInformation("Starting cryptocurrency sync...");
                
                var cryptos = await _coinGeckoService.GetMarketDataAsync();
                var savedCount = 0;
                var updatedCount = 0;
                
                // Get all existing symbols to track in-memory duplicates
                var existingSymbols = new HashSet<string>();
                
                foreach (var crypto in cryptos)
                {
                    try
                    {
                        // Check if cryptocurrency exists by CoinGeckoId
                        var existing = await _unitOfWork.Cryptocurrencies.GetByCoinGeckoIdAsync(crypto.Id);
                        
                        if (existing == null)
                        {
                            // Create new cryptocurrency with unique symbol
                            var symbol = crypto.Symbol.ToUpper();
                            
                            // Check if symbol already exists in database OR in current batch
                            var symbolExists = await _unitOfWork.Cryptocurrencies.GetBySymbolAsync(symbol);
                            if (symbolExists != null || existingSymbols.Contains(symbol))
                            {
                                // Make symbol unique by appending CoinGeckoId
                                symbol = $"{symbol}-{crypto.Id}";
                                _logger.LogDebug("Symbol conflict detected, using unique symbol: {Symbol}", symbol);
                            }
                            
                            existingSymbols.Add(symbol);
                            
                            var newCrypto = new Cryptocurrency
                            {
                                CoinGeckoId = crypto.Id,
                                Symbol = symbol,
                                Name = crypto.Name,
                                IconUrl = null,
                                MarketCapRank = null,
                                IsActive = true,
                                CreatedAtUtc = DateTime.UtcNow,
                                UpdatedAtUtc = DateTime.UtcNow
                            };
                            
                            await _unitOfWork.Cryptocurrencies.AddAsync(newCrypto);
                            
                            // Save immediately to avoid in-memory conflicts
                            await _unitOfWork.SaveChangesAsync();
                            savedCount++;
                            
                            _logger.LogDebug("Added new cryptocurrency: {Symbol} ({CoinGeckoId})", newCrypto.Symbol, crypto.Id);
                        }
                        else
                        {
                            // Update existing cryptocurrency
                            existing.Name = crypto.Name;
                            existing.UpdatedAtUtc = DateTime.UtcNow;
                            existing.IsActive = true;
                            
                            _unitOfWork.Cryptocurrencies.Update(existing);
                            updatedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to sync cryptocurrency: {CoinGeckoId}", crypto.Id);
                        // Continue with next crypto instead of failing entire sync
                    }
                }
                
                // Save any remaining updates
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Cryptocurrency sync completed. Added: {SavedCount}, Updated: {UpdatedCount}", savedCount, updatedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing cryptocurrencies");
                throw;
            }
        }

        public async Task SyncPricesAsync()
        {
            try
            {
                _logger.LogInformation("Starting price sync...");
                
                var cryptos = await _coinGeckoService.GetMarketDataAsync();
                var savedCount = 0;
                
                foreach (var crypto in cryptos)
                {
                    // Find cryptocurrency in database
                    var dbCrypto = await _unitOfWork.Cryptocurrencies.GetByCoinGeckoIdAsync(crypto.Id);
                    
                    if (dbCrypto != null && crypto.CurrentPrice.HasValue)
                    {
                        // Create new price record
                        var price = new CryptoPrice
                        {
                            CryptocurrencyId = dbCrypto.Id,
                            PriceUsd = crypto.CurrentPrice.Value,
                            MarketCap = crypto.MarketCap,
                            Volume24h = crypto.TotalVolume,
                            PercentChange1h = crypto.PriceChangePercentage1h,
                            PercentChange24h = crypto.PriceChangePercentage24h,
                            PercentChange7d = crypto.PriceChangePercentage7d,
                            CirculatingSupply = crypto.CirculatingSupply,
                            TotalSupply = null,
                            CollectedAtUtc = DateTime.UtcNow
                        };
                        
                        await _unitOfWork.CryptoPrices.AddAsync(price);
                        savedCount++;
                    }
                }
                
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Price sync completed. Saved {Count} price records", savedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing prices");
                throw;
            }
        }

        public async Task SyncMarketStatsAsync()
        {
            try
            {
                _logger.LogInformation("Starting market stats sync...");
                
                var stats = await _coinGeckoService.GetMarketStatsAsync();
                
                var marketStat = new MarketStat
                {
                    TotalMarketCap = stats.TotalMarketCap,
                    TotalVolume = stats.TotalVolume,
                    ActiveCryptocurrencies = stats.ActiveCryptocurrencies,
                    MarketCapChangePercentage24h = stats.MarketCapChangePercentage24h,
                    BtcDominance = stats.BtcDominance,
                    EthDominance = stats.EthDominance,
                    CollectedAtUtc = DateTime.UtcNow
                };
                
                await _unitOfWork.MarketStats.AddAsync(marketStat);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Market stats sync completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing market stats");
                throw;
            }
        }
    }
}

