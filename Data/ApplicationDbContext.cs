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
        public DbSet<UserWatchlist> UserWatchlists { get; set; }

        // Market
        public DbSet<Cryptocurrency> Cryptocurrencies { get; set; }
        public DbSet<CryptoPrice> CryptoPrices { get; set; }
        public DbSet<MarketStat> MarketStats { get; set; }

        // Trading
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletMovement> WalletMovements { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderHold> OrderHolds { get; set; }
        public DbSet<Trade> Trades { get; set; }

        // Payment
        public DbSet<DepositTransaction> DepositTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // User entity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).IsRequired().HasColumnType("LONGTEXT");
                entity.Property(e => e.FullName).HasColumnType("LONGTEXT");
                entity.Property(e => e.EmailConfirmationToken).HasColumnType("LONGTEXT");
                entity.Property(e => e.RefreshToken).HasColumnType("LONGTEXT");
                entity.Property(e => e.PasswordResetToken).HasColumnType("LONGTEXT");
                entity.Property(e => e.TwoFactorSecret).HasColumnType("LONGTEXT");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            });

            // UserWatchlist - many-to-many relationship
            modelBuilder.Entity<UserWatchlist>(entity =>
            {
                entity.ToTable("UserWatchlist");
                entity.HasKey(e => new { e.UserId, e.CryptocurrencyId });
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.CryptocurrencyId).IsRequired();
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Cryptocurrency)
                    .WithMany()
                    .HasForeignKey(e => e.CryptocurrencyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Cryptocurrency entity configuration
            modelBuilder.Entity<Cryptocurrency>(entity =>
            {
                entity.ToTable("Cryptocurrencies");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasIndex(e => e.Symbol).IsUnique();
                entity.HasIndex(e => e.CoinGeckoId).IsUnique();
            });

            // CryptoPrice entity configuration
            modelBuilder.Entity<CryptoPrice>(entity =>
            {
                entity.ToTable("CryptoPrices");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasIndex(e => new { e.CryptocurrencyId, e.CollectedAtUtc });
                
                entity.HasOne(e => e.Cryptocurrency)
                    .WithMany(c => c.Prices)
                    .HasForeignKey(e => e.CryptocurrencyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // MarketStat entity configuration
            modelBuilder.Entity<MarketStat>(entity =>
            {
                entity.ToTable("MarketStats");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasIndex(e => e.CollectedAtUtc);
            });

            // Wallet entity configuration
            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.ToTable("Wallets");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasIndex(e => new { e.UserId, e.AssetType, e.CurrencyCode, e.CryptocurrencyId }).IsUnique();
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Cryptocurrency)
                    .WithMany()
                    .HasForeignKey(e => e.CryptocurrencyId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // WalletMovement entity configuration
            modelBuilder.Entity<WalletMovement>(entity =>
            {
                entity.ToTable("WalletMovements");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasIndex(e => new { e.WalletId, e.CreatedAt });
                
                entity.HasOne(e => e.Wallet)
                    .WithMany()
                    .HasForeignKey(e => e.WalletId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Order entity configuration
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasIndex(e => new { e.UserId, e.CreatedAt });
                entity.HasIndex(e => new { e.CryptocurrencyId, e.Status });
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Cryptocurrency)
                    .WithMany()
                    .HasForeignKey(e => e.CryptocurrencyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // OrderHold entity configuration
            modelBuilder.Entity<OrderHold>(entity =>
            {
                entity.ToTable("OrderHolds");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Wallet)
                    .WithMany()
                    .HasForeignKey(e => e.WalletId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Trade entity configuration
            modelBuilder.Entity<Trade>(entity =>
            {
                entity.ToTable("Trades");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasIndex(e => new { e.OrderId, e.CreatedAt });
                
                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Cryptocurrency)
                    .WithMany()
                    .HasForeignKey(e => e.CryptocurrencyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // DepositTransaction entity configuration
            modelBuilder.Entity<DepositTransaction>(entity =>
            {
                entity.ToTable("DepositTransactions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasIndex(e => new { e.UserId, e.CreatedAt });
                entity.HasIndex(e => e.OrderId).IsUnique();
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}