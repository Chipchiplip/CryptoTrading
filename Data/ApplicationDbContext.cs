using CryptoTrading.Models;
using Microsoft.EntityFrameworkCore;

namespace CryptoTrading.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Auth
        public DbSet<User> Users { get; set; }

        // Market
        public DbSet<Cryptocurrency> Cryptocurrencies { get; set; }
        public DbSet<CryptoPrice> CryptoPrices { get; set; }
        public DbSet<MarketStat> MarketStats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // User entity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
            });

            // Cryptocurrency entity configuration
            modelBuilder.Entity<Cryptocurrency>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Symbol).IsUnique();
                entity.HasIndex(e => e.CoinGeckoId).IsUnique();
            });

            // CryptoPrice entity configuration
            modelBuilder.Entity<CryptoPrice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.CryptocurrencyId, e.CollectedAtUtc });
                
                entity.HasOne(e => e.Cryptocurrency)
                    .WithMany(c => c.Prices)
                    .HasForeignKey(e => e.CryptocurrencyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // MarketStat entity configuration
            modelBuilder.Entity<MarketStat>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CollectedAtUtc);
            });
        }
    }
}