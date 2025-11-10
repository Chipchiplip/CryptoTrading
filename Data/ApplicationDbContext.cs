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

        // ========== AUTH ==========
        public DbSet<User> Users { get; set; }
        public DbSet<UserWatchlist> UserWatchlists { get; set; }
        public DbSet<LoginActivity> LoginActivity { get; set; }

        // ========== MARKET ==========
        public DbSet<Cryptocurrency> Cryptocurrencies { get; set; }
        public DbSet<CryptoPrice> CryptoPrices { get; set; }
        public DbSet<MarketStat> MarketStats { get; set; }

        // ========== TRADING ==========
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletMovement> WalletMovements { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderHold> OrderHolds { get; set; }
        public DbSet<Trade> Trades { get; set; }
        public DbSet<Level> Levels { get; set; }
        public DbSet<Role> Roles { get; set; }

        // Payment
        public DbSet<DepositTransaction> DepositTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================
            // USER ENTITY CONFIGURATION
            // ==========================
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Role);
                entity.HasIndex(e => e.Level);
                entity.HasIndex(e => e.IsActive);

                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).IsRequired().HasColumnType("LONGTEXT");
                entity.Property(e => e.FullName).HasColumnType("LONGTEXT");
                entity.Property(e => e.AvatarUrl).HasMaxLength(500);
                entity.Property(e => e.Bio).HasMaxLength(200);
                entity.Property(e => e.Role).HasMaxLength(50).HasDefaultValue("User");
                entity.Property(e => e.Level).HasMaxLength(50).HasDefaultValue("Beginner");
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.Property(e => e.EmailConfirmationToken).HasColumnType("LONGTEXT");
                entity.Property(e => e.RefreshToken).HasColumnType("LONGTEXT");
                entity.Property(e => e.PasswordResetToken).HasColumnType("LONGTEXT");
                entity.Property(e => e.TwoFactorSecret).HasColumnType("LONGTEXT");

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            });

            // ==========================
            // LOGIN ACTIVITY CONFIG
            // ==========================
            modelBuilder.Entity<LoginActivity>(entity =>
            {
                entity.ToTable("LoginActivity");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Ip).HasMaxLength(64);
                entity.Property(e => e.UserAgent).HasMaxLength(255);
                entity.Property(e => e.Success).HasDefaultValue(true);

                entity.HasIndex(e => new { e.UserId, e.CreatedAt });

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================
            // USER WATCHLIST CONFIG
            // ==========================
            modelBuilder.Entity<UserWatchlist>(entity =>
            {
                entity.ToTable("UserWatchlist");
                entity.HasKey(e => new { e.UserId, e.CryptocurrencyId });
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Cryptocurrency)
                    .WithMany()
                    .HasForeignKey(e => e.CryptocurrencyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================
            // CRYPTOCURRENCY CONFIG
            // ==========================
            modelBuilder.Entity<Cryptocurrency>(entity =>
            {
                entity.ToTable("Cryptocurrencies");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasIndex(e => e.Symbol).IsUnique();
                entity.HasIndex(e => e.CoinGeckoId).IsUnique();
            });

            // ==========================
            // CRYPTO PRICE CONFIG
            // ==========================
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

            // ==========================
            // MARKET STAT CONFIG
            // ==========================
            modelBuilder.Entity<MarketStat>(entity =>
            {
                entity.ToTable("MarketStats");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasIndex(e => e.CollectedAtUtc);
            });

            // ==========================
            // WALLET CONFIG
            // ==========================
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

            // ==========================
            // WALLET MOVEMENTS CONFIG
            // ==========================
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

            // ==========================
            // ORDER CONFIG
            // ==========================
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

            // ==========================
            // ORDER HOLD CONFIG
            // ==========================
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

            // ==========================
            // TRADE CONFIG
            // ==========================
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