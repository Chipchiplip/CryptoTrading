using CryptoTrading.Data;
using CryptoTrading.Models;
using CryptoTrading.Services;
using CryptoTrading.Services.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CryptoTrading.Tests
{
    public class PortfolioServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ICoinGeckoService> _coinGeckoServiceMock;
        private readonly Mock<ILogger<PortfolioService>> _loggerMock;
        private readonly PortfolioService _portfolioService;

        public PortfolioServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new ApplicationDbContext(options);
            _coinGeckoServiceMock = new Mock<ICoinGeckoService>();
            _loggerMock = new Mock<ILogger<PortfolioService>>();

            _portfolioService = new PortfolioService(
                _context,
                _coinGeckoServiceMock.Object,
                _loggerMock.Object
            );

            SeedTestData();
        }

        private void SeedTestData()
        {
            var btc = new Cryptocurrency
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                CoinGeckoId = "bitcoin"
            };

            _context.Cryptocurrencies.Add(btc);

            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                PasswordHash = "hash",
                FullName = "Test User",
                Role = "User",
                Level = "Beginner",
                IsActive = true
            };

            _context.Users.Add(user);

            // Create wallets
            var usdWallet = new Wallet { Id = 1, UserId = 1, AssetType = "FIAT", CurrencyCode = "USD" };
            var btcWallet = new Wallet { Id = 2, UserId = 1, AssetType = "COIN", CryptocurrencyId = 1 };

            _context.Wallets.AddRange(usdWallet, btcWallet);

            // Add wallet movements
            _context.WalletMovements.Add(new WalletMovement
            {
                WalletId = 1,
                RefType = "DEPOSIT",
                Amount = 50000m,
                Note = "USD deposit",
                CreatedAt = DateTime.UtcNow
            });

            _context.WalletMovements.Add(new WalletMovement
            {
                WalletId = 2,
                RefType = "TRADE",
                Amount = 0.5m,
                Note = "BTC from trade",
                CreatedAt = DateTime.UtcNow
            });

            // Add orders and trades for cost basis
            var order = new Order
            {
                Id = 1,
                UserId = 1,
                CryptocurrencyId = 1,
                Side = "BUY",
                Type = "MARKET",
                Status = "FILLED",
                PriceUsd = null,
                QuantityCoin = 0.5m,
                FilledQty = 0.5m,
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);

            var trade = new Trade
            {
                Id = 1,
                OrderId = 1,
                CryptocurrencyId = 1,
                PriceUsd = 45000m, // Bought at $45k
                QuantityCoin = 0.5m,
                FeeUsd = 22.5m,
                CreatedAt = DateTime.UtcNow
            };

            _context.Trades.Add(trade);

            _context.SaveChanges();
        }

        [Fact]
        public async Task GetPortfolioOverviewAsync_ShouldCalculateCorrectly()
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
                        CurrentPrice = 50000m // Current price is $50k
                    }
                });

            // Act
            var portfolio = await _portfolioService.GetPortfolioOverviewAsync(1);

            // Assert
            Assert.NotNull(portfolio);
            Assert.True(portfolio.TotalValue > 0);
            Assert.True(portfolio.Holdings.Count > 0);

            // Check BTC holding
            var btcHolding = portfolio.Holdings.FirstOrDefault(h => h.Symbol == "BTC");
            Assert.NotNull(btcHolding);
            Assert.Equal(0.5m, btcHolding.Amount);
            Assert.Equal(50000m, btcHolding.CurrentPrice);
            Assert.Equal(25000m, btcHolding.Value); // 0.5 * $50k

            // Cost basis should be ~$45k + fee
            Assert.True(btcHolding.AvgPrice > 45000m);
            Assert.True(btcHolding.AvgPrice < 45100m);

            // PnL should be positive (bought at 45k, now worth 50k)
            Assert.True(btcHolding.Pnl > 0);
        }

        [Fact]
        public async Task CalculateRealizedPnLAsync_WithSellTrades_ShouldCalculateFIFO()
        {
            // Arrange - add a SELL trade
            var sellOrder = new Order
            {
                Id = 2,
                UserId = 1,
                CryptocurrencyId = 1,
                Side = "SELL",
                Type = "MARKET",
                Status = "FILLED",
                PriceUsd = null,
                QuantityCoin = 0.2m,
                FilledQty = 0.2m,
                CreatedAt = DateTime.UtcNow.AddMinutes(5)
            };

            _context.Orders.Add(sellOrder);

            var sellTrade = new Trade
            {
                Id = 2,
                OrderId = 2,
                CryptocurrencyId = 1,
                PriceUsd = 48000m, // Sold at $48k
                QuantityCoin = 0.2m,
                FeeUsd = 9.6m,
                CreatedAt = DateTime.UtcNow.AddMinutes(5)
            };

            _context.Trades.Add(sellTrade);
            await _context.SaveChangesAsync();

            // Act
            var realizedPnL = await _portfolioService.CalculateRealizedPnLAsync(1);

            // Assert
            // Bought 0.2 at ~$45,000 + fee = ~$9,004.5
            // Sold 0.2 at $48,000 - fee = $9,600 - $9.6 = $9,590.4
            // PnL should be positive (~$586)
            Assert.True(realizedPnL > 0);
            Assert.True(realizedPnL > 500m); // At least $500 profit
        }

        [Fact]
        public async Task GetAverageCostBasisAsync_ShouldUseFIFO()
        {
            // Arrange - add more BUY trades at different prices
            var order2 = new Order
            {
                Id = 3,
                UserId = 1,
                CryptocurrencyId = 1,
                Side = "BUY",
                Type = "MARKET",
                Status = "FILLED",
                PriceUsd = null,
                QuantityCoin = 0.3m,
                FilledQty = 0.3m,
                CreatedAt = DateTime.UtcNow.AddMinutes(10)
            };

            _context.Orders.Add(order2);

            var trade2 = new Trade
            {
                Id = 3,
                OrderId = 3,
                CryptocurrencyId = 1,
                PriceUsd = 46000m, // Second buy at $46k
                QuantityCoin = 0.3m,
                FeeUsd = 13.8m,
                CreatedAt = DateTime.UtcNow.AddMinutes(10)
            };

            _context.Trades.Add(trade2);
            await _context.SaveChangesAsync();

            // Act
            var avgCostBasis = await _portfolioService.GetAverageCostBasisAsync(
                1, "BTC", DateTime.UtcNow.AddMinutes(15));

            // Assert
            // Average of $45,000 (0.5 BTC) and $46,000 (0.3 BTC)
            // = (45000*0.5 + 46000*0.3 + fees) / 0.8
            // ≈ $45,545
            Assert.True(avgCostBasis > 45000m);
            Assert.True(avgCostBasis < 46000m);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}

