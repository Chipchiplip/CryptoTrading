using CryptoTrading.Data;
using CryptoTrading.Models;
using CryptoTrading.Models.DTOs;
using CryptoTrading.Services;
using CryptoTrading.Services.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CryptoTrading.Tests
{
    public class TradingServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ICoinGeckoService> _coinGeckoServiceMock;
        private readonly Mock<ICryptoCacheService> _cacheServiceMock;
        private readonly Mock<ILogger<TradingService>> _loggerMock;
        private readonly TradingService _tradingService;

        public TradingServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            
            _context = new ApplicationDbContext(options);
            _coinGeckoServiceMock = new Mock<ICoinGeckoService>();
            _cacheServiceMock = new Mock<ICryptoCacheService>();
            _loggerMock = new Mock<ILogger<TradingService>>();

            _tradingService = new TradingService(
                _context,
                _coinGeckoServiceMock.Object,
                _cacheServiceMock.Object,
                _loggerMock.Object
            );

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            var btc = new Cryptocurrency
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                CoinGeckoId = "bitcoin",
                IconUrl = "https://example.com/btc.png"
            };

            var eth = new Cryptocurrency
            {
                Id = 2,
                Symbol = "ETH",
                Name = "Ethereum",
                CoinGeckoId = "ethereum",
                IconUrl = "https://example.com/eth.png"
            };

            _context.Cryptocurrencies.AddRange(btc, eth);

            var testUser = new User
            {
                Id = 1,
                Email = "test@example.com",
                PasswordHash = "hash",
                FullName = "Test User",
                Role = "User",
                Level = "Beginner",
                IsActive = true
            };

            _context.Users.Add(testUser);

            // Add wallet with balance
            var usdWallet = new Wallet
            {
                Id = 1,
                UserId = 1,
                AssetType = "FIAT",
                CurrencyCode = "USD",
                CryptocurrencyId = null
            };

            _context.Wallets.Add(usdWallet);

            // Add initial USD balance
            _context.WalletMovements.Add(new WalletMovement
            {
                Id = 1,
                WalletId = 1,
                RefType = "DEPOSIT",
                RefId = null,
                Amount = 100000m, // $100k
                Note = "Initial deposit",
                CreatedAt = DateTime.UtcNow
            });

            _context.SaveChanges();
        }

        [Fact]
        public async Task PlaceOrderAsync_MarketBuyOrder_ShouldExecuteSuccessfully()
        {
            // Arrange
            var marketData = new List<Crypto>
            {
                new Crypto
                {
                    Id = "bitcoin",
                    Symbol = "btc",
                    Name = "Bitcoin",
                    CurrentPrice = 50000m
                }
            };
            
            _coinGeckoServiceMock.Setup(x => x.GetMarketDataAsync(It.IsAny<bool>()))
                .ReturnsAsync(marketData);
            
            _cacheServiceMock.Setup(x => x.TryGetCryptoData(out It.Ref<List<Crypto>?>.IsAny))
                .Returns((out List<Crypto>? data) => 
                {
                    data = marketData;
                    return true;
                });

            var request = new PlaceOrderRequest
            {
                Symbol = "BTC/USD",
                Side = "BUY",
                Type = "MARKET",
                Quantity = 0.1m,
                Price = null
            };

            // Act
            var result = await _tradingService.PlaceOrderAsync(1, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BUY", result.Side);
            Assert.Equal("MARKET", result.Type);
            Assert.Equal(0.1m, result.Quantity);
            Assert.True(result.Filled > 0); // Should be filled immediately
        }

        [Fact]
        public async Task PlaceOrderAsync_InsufficientBalance_ShouldThrowException()
        {
            // Arrange
            _coinGeckoServiceMock.Setup(x => x.GetMarketDataAsync(It.IsAny<bool>()))
                .ReturnsAsync(new List<Crypto>
                {
                    new Crypto
                    {
                        Id = "bitcoin",
                        Symbol = "btc",
                        Name = "Bitcoin",
                        CurrentPrice = 50000m
                    }
                });

            var request = new PlaceOrderRequest
            {
                Symbol = "BTC/USD",
                Side = "BUY",
                Type = "MARKET",
                Quantity = 100m, // Too much - would cost $5M
                Price = null
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _tradingService.PlaceOrderAsync(1, request)
            );
        }

        [Fact]
        public async Task PlaceOrderAsync_LimitOrder_ShouldCreatePendingOrder()
        {
            // Arrange
            var marketData = new List<Crypto>
            {
                new Crypto
                {
                    Id = "bitcoin",
                    Symbol = "btc",
                    Name = "Bitcoin",
                    CurrentPrice = 50000m
                }
            };
            
            _coinGeckoServiceMock.Setup(x => x.GetMarketDataAsync(It.IsAny<bool>()))
                .ReturnsAsync(marketData);
            
            _cacheServiceMock.Setup(x => x.TryGetCryptoData(out It.Ref<List<Crypto>?>.IsAny))
                .Returns((out List<Crypto>? data) => 
                {
                    data = marketData;
                    return true;
                });
            
            var request = new PlaceOrderRequest
            {
                Symbol = "BTC/USD",
                Side = "BUY",
                Type = "LIMIT",
                Quantity = 0.1m,
                Price = 45000m
            };

            // Act
            var result = await _tradingService.PlaceOrderAsync(1, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("LIMIT", result.Type);
            Assert.Equal(45000m, result.Price);
            Assert.Equal("NEW", result.Status);
            Assert.Equal(0m, result.Filled); // Not filled yet
        }

        [Fact]
        public async Task CancelOrderAsync_ValidOrder_ShouldCancelSuccessfully()
        {
            // Arrange - create a limit order first
            var marketData = new List<Crypto>
            {
                new Crypto
                {
                    Id = "bitcoin",
                    Symbol = "btc",
                    Name = "Bitcoin",
                    CurrentPrice = 50000m
                }
            };
            
            _coinGeckoServiceMock.Setup(x => x.GetMarketDataAsync(It.IsAny<bool>()))
                .ReturnsAsync(marketData);
            
            _cacheServiceMock.Setup(x => x.TryGetCryptoData(out It.Ref<List<Crypto>?>.IsAny))
                .Returns((out List<Crypto>? data) => 
                {
                    data = marketData;
                    return true;
                });
            
            var request = new PlaceOrderRequest
            {
                Symbol = "BTC/USD",
                Side = "BUY",
                Type = "LIMIT",
                Quantity = 0.1m,
                Price = 45000m
            };

            var placedOrder = await _tradingService.PlaceOrderAsync(1, request);
            var orderId = ulong.Parse(placedOrder.Id);

            // Act
            var result = await _tradingService.CancelOrderAsync(1, orderId);

            // Assert
            Assert.Equal("CANCELED", result.Status);

            // Verify balance was released
            var availableBalance = await CalculateAvailableBalance(1, "FIAT", "USD");
            Assert.Equal(100000m, availableBalance); // Should be back to original
        }

        [Fact]
        public async Task MatchOrdersAsync_OppositeOrders_ShouldMatch()
        {
            // Arrange - create a buy and sell order at matching prices
            var marketData = new List<Crypto>
            {
                new Crypto
                {
                    Id = "bitcoin",
                    Symbol = "btc",
                    Name = "Bitcoin",
                    CurrentPrice = 50000m
                }
            };
            
            _coinGeckoServiceMock.Setup(x => x.GetMarketDataAsync(It.IsAny<bool>()))
                .ReturnsAsync(marketData);
            
            _cacheServiceMock.Setup(x => x.TryGetCryptoData(out It.Ref<List<Crypto>?>.IsAny))
                .Returns((out List<Crypto>? data) => 
                {
                    data = marketData;
                    return true;
                });
            
            var buyRequest = new PlaceOrderRequest
            {
                Symbol = "BTC/USD",
                Side = "BUY",
                Type = "LIMIT",
                Quantity = 0.1m,
                Price = 50000m
            };

            var sellRequest = new PlaceOrderRequest
            {
                Symbol = "BTC/USD",
                Side = "SELL",
                Type = "LIMIT",
                Quantity = 0.1m,
                Price = 50000m
            };

            // Create another user and wallet for sell side
            var seller = new User
            {
                Id = 2,
                Email = "seller@example.com",
                PasswordHash = "hash",
                FullName = "Seller",
                Role = "User",
                Level = "Beginner",
                IsActive = true
            };
            _context.Users.Add(seller);

            var btcWallet = new Wallet
            {
                Id = 2,
                UserId = 2,
                AssetType = "COIN",
                CurrencyCode = null,
                CryptocurrencyId = 1 // BTC
            };
            _context.Wallets.Add(btcWallet);

            _context.WalletMovements.Add(new WalletMovement
            {
                WalletId = 2,
                RefType = "DEPOSIT",
                Amount = 1m, // 1 BTC
                Note = "Initial deposit",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Place orders
            var buyOrderResult = await _tradingService.PlaceOrderAsync(1, buyRequest);
            var sellOrderResult = await _tradingService.PlaceOrderAsync(2, sellRequest);

            // Act - Try immediate matching (happens in PlaceOrderAsync) or run matching engine
            var matchCount = await _tradingService.MatchOrdersAsync();

            // Assert
            // Note: Orders might match immediately in PlaceOrderAsync or need explicit matching
            // Check that at least one order was filled or matching was attempted
            var orders = await _context.Orders.ToListAsync();
            var filledOrders = orders.Where(o => o.Status == "FILLED").ToList();
            
            // Either orders matched immediately, or matching engine found matches
            Assert.True(filledOrders.Count > 0 || matchCount > 0, 
                $"Expected at least one filled order or match. Filled: {filledOrders.Count}, Matches: {matchCount}");
        }

        [Fact]
        public async Task GetOrderBookAsync_ShouldReturnAggregatedLevels()
        {
            // Arrange - create multiple orders at different price levels
            var marketData = new List<Crypto>
            {
                new Crypto
                {
                    Id = "bitcoin",
                    Symbol = "btc",
                    Name = "Bitcoin",
                    CurrentPrice = 50000m
                }
            };
            
            _coinGeckoServiceMock.Setup(x => x.GetMarketDataAsync(It.IsAny<bool>()))
                .ReturnsAsync(marketData);
            
            _cacheServiceMock.Setup(x => x.TryGetCryptoData(out It.Ref<List<Crypto>?>.IsAny))
                .Returns((out List<Crypto>? data) => 
                {
                    data = marketData;
                    return true;
                });
            
            var orders = new[]
            {
                new PlaceOrderRequest { Symbol = "BTC/USD", Side = "BUY", Type = "LIMIT", Quantity = 0.1m, Price = 49000m },
                new PlaceOrderRequest { Symbol = "BTC/USD", Side = "BUY", Type = "LIMIT", Quantity = 0.2m, Price = 49000m }, // Same price
                new PlaceOrderRequest { Symbol = "BTC/USD", Side = "BUY", Type = "LIMIT", Quantity = 0.1m, Price = 48000m },
                new PlaceOrderRequest { Symbol = "BTC/USD", Side = "SELL", Type = "LIMIT", Quantity = 0.1m, Price = 51000m },
                new PlaceOrderRequest { Symbol = "BTC/USD", Side = "SELL", Type = "LIMIT", Quantity = 0.2m, Price = 52000m },
            };

            // Setup seller wallets
            for (int i = 0; i < 2; i++)
            {
                var seller = new User
                {
                    Id = 10 + i,
                    Email = $"seller{i}@example.com",
                    PasswordHash = "hash",
                    FullName = $"Seller {i}",
                    Role = "User",
                    Level = "Beginner",
                    IsActive = true
                };
                _context.Users.Add(seller);

                var btcWallet = new Wallet
                {
                    Id = (ulong)(10 + i),
                    UserId = 10 + i,
                    AssetType = "COIN",
                    CurrencyCode = null,
                    CryptocurrencyId = 1
                };
                _context.Wallets.Add(btcWallet);

                _context.WalletMovements.Add(new WalletMovement
                {
                    WalletId = (ulong)(10 + i),
                    RefType = "DEPOSIT",
                    Amount = 1m,
                    Note = "Initial deposit",
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();

            // Place orders
            for (int i = 0; i < orders.Length; i++)
            {
                var userId = orders[i].Side == "BUY" ? 1 : 10 + (i % 2);
                await _tradingService.PlaceOrderAsync(userId, orders[i]);
            }

            // Act
            var orderBook = await _tradingService.GetOrderBookAsync("BTC/USD");

            // Assert
            Assert.NotNull(orderBook);
            Assert.True(orderBook.Bids.Count > 0);
            Assert.True(orderBook.Asks.Count > 0);

            // Check that orders at same price are aggregated
            var buyAt49k = orderBook.Bids.FirstOrDefault(b => b.Price == 49000m);
            Assert.NotNull(buyAt49k);
            Assert.Equal(0.3m, buyAt49k.Quantity); // 0.1 + 0.2
        }

        private async Task<decimal> CalculateAvailableBalance(int userId, string assetType, string? currencyCode)
        {
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId && 
                                         w.AssetType == assetType && 
                                         w.CurrencyCode == currencyCode);

            if (wallet == null) return 0;

            var totalBalance = await _context.WalletMovements
                .Where(m => m.WalletId == wallet.Id)
                .SumAsync(m => m.Amount);

            var lockedBalance = await _context.OrderHolds
                .Where(h => h.WalletId == wallet.Id && h.ReleasedAt == null)
                .SumAsync(h => h.Amount);

            return totalBalance - lockedBalance;
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}

