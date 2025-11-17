using CryptoTrading.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Collections.Generic;
using System.Linq;

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

        // ========== BOT TRADING ==========
        public DbSet<BotStrategyDefinition> BotStrategyDefinitions { get; set; }
        public DbSet<TradingBot> TradingBots { get; set; }
        public DbSet<TradingBotParameter> TradingBotParameters { get; set; }
        public DbSet<TradingBotRuntimeSnapshot> TradingBotRuntimeSnapshots { get; set; }
        public DbSet<TradingBotOrder> TradingBotOrders { get; set; }
        public DbSet<TradingBotLog> TradingBotLogs { get; set; }
        
        // ========== AUDIT & CONFIGURATION ==========
        public DbSet<AuditEvent> AuditEvents { get; set; }
        public DbSet<FeeLedger> FeeLedger { get; set; }
        public DbSet<TradingConfiguration> TradingConfigurations { get; set; }
        public DbSet<BotRiskConfiguration> BotRiskConfigurations { get; set; }
        public DbSet<ReconciliationResult> ReconciliationResults { get; set; }
        public DbSet<ClientOrderIdempotency> ClientOrderIdempotency { get; set; }

        // ========== RISK MANAGEMENT ==========
        public DbSet<KillSwitchEvent> KillSwitchEvents { get; set; }
        public DbSet<BotRiskState> BotRiskStates { get; set; }
        public DbSet<UserCapitalLimits> UserCapitalLimits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var guidConverter = new GuidToStringConverter();
            var nullableGuidConverter = new ValueConverter<Guid?, string?>(
                v => v.HasValue ? v.Value.ToString() : null,
                v => string.IsNullOrEmpty(v) ? (Guid?)null : Guid.Parse(v));

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

                entity.Property(e => e.EmailConfirmationToken).HasColumnType("longtext");
                entity.Property(e => e.RefreshToken).HasColumnType("longtext");
                entity.Property(e => e.PasswordResetToken).HasColumnType("longtext");
                entity.Property(e => e.TwoFactorSecret).HasColumnType("longtext");

                entity.Property(e => e.EmailConfirmed).HasColumnType("tinyint(1)");
                entity.Property(e => e.TwoFactorEnabled).HasColumnType("tinyint(1)");
                entity.Property(e => e.IsActive).HasColumnType("tinyint(1)").HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.Property(e => e.LastLoginAt).HasColumnType("datetime(6)");
                entity.Property(e => e.RefreshTokenExpiryTime).HasColumnType("datetime(6)");
                entity.Property(e => e.PasswordResetTokenExpiry).HasColumnType("datetime(6)");
                entity.Property(e => e.EmailConfirmationTokenExpiry).HasColumnType("datetime(6)");
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
                entity.Property(e => e.Success).HasColumnType("tinyint(1)").HasDefaultValue(true);

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
                entity.Property(e => e.PriceUsd).HasPrecision(28, 8);
                entity.Property(e => e.MarketCap).HasPrecision(28, 2);
                entity.Property(e => e.Volume24h).HasPrecision(28, 2);
                entity.Property(e => e.PercentChange1h).HasPrecision(10, 4);
                entity.Property(e => e.PercentChange24h).HasPrecision(10, 4);
                entity.Property(e => e.PercentChange7d).HasPrecision(10, 4);
                entity.Property(e => e.CirculatingSupply).HasPrecision(28, 2);
                entity.Property(e => e.TotalSupply).HasPrecision(28, 2);
                entity.Property(e => e.CollectedAtUtc).HasColumnType("datetime(6)");

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
                entity.Property(e => e.TotalMarketCap).HasPrecision(28, 2);
                entity.Property(e => e.TotalVolume).HasPrecision(28, 2);
                entity.Property(e => e.MarketCapChangePercentage24h).HasPrecision(10, 4);
                entity.Property(e => e.BtcDominance).HasPrecision(10, 4);
                entity.Property(e => e.EthDominance).HasPrecision(10, 4);
                entity.Property(e => e.CollectedAtUtc).HasColumnType("datetime(6)");
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
                entity.Property(e => e.IsDefault).HasColumnType("tinyint(1)").HasDefaultValue(false);
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                    .ValueGeneratedOnAddOrUpdate();

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Wallets)
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
                entity.Property(e => e.Direction).HasMaxLength(8).IsRequired();
                entity.Property(e => e.Amount).HasPrecision(38, 18);
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                entity.HasOne(e => e.Wallet)
                    .WithMany(w => w.Movements)
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
                entity.Property(e => e.PriceUsd).HasPrecision(30, 10);
                entity.Property(e => e.QuantityCoin).HasPrecision(38, 18);
                entity.Property(e => e.FilledQty).HasPrecision(38, 18);
                entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
                entity.Property(e => e.UpdatedAt).HasColumnType("datetime(6)");
                entity.HasIndex(e => new { e.UserId, e.CreatedAt });
                entity.HasIndex(e => new { e.CryptocurrencyId, e.Status });

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Orders)
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
                entity.Property(e => e.PriceUsd).HasPrecision(30, 10);
                entity.Property(e => e.QuantityCoin).HasPrecision(38, 18);
                entity.Property(e => e.FeeUsd).HasPrecision(30, 10);
                entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
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

            modelBuilder.Entity<DepositTransaction>(entity =>
            {
                entity.ToTable("DepositTransactions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.Property(e => e.CompletedAt).HasColumnType("datetime(6)");
                entity.Property(e => e.Provider).HasMaxLength(20).HasDefaultValue("VNPAY");
                entity.Property(e => e.StripeSessionId).HasMaxLength(128);
                entity.Property(e => e.StripePaymentIntentId).HasMaxLength(128);
                entity.Property(e => e.PaymentMethod).HasMaxLength(64);

                entity.HasIndex(e => new { e.UserId, e.CreatedAt });
                entity.HasIndex(e => e.OrderId).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            // ==========================
            // BOT STRATEGY DEFINITION CONFIG
            // ==========================
            modelBuilder.Entity<BotStrategyDefinition>(entity =>
            {
                entity.ToTable("BotStrategyDefinitions");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.StrategyKey, e.Version }).IsUnique();
                entity.HasIndex(e => e.IsActive);

                entity.Property(e => e.Id)
                    .HasColumnType("char(36)")
                    .HasConversion(
                        v => v.ToString(),
                        v => Guid.Parse(v));
                entity.Property(e => e.ParametersSchema)
                    .HasColumnType("json")
                    .HasConversion(
                        v => v ?? string.Empty,
                        v => string.IsNullOrEmpty(v) ? null : v)
                    .IsRequired(false);
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            });

            // ==========================
            // TRADING BOT CONFIG
            // ==========================
            modelBuilder.Entity<TradingBot>(entity =>
            {
                entity.ToTable("TradingBots");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnType("char(36)")
                    .HasConversion(
                        v => v.ToString(),
                        v => Guid.Parse(v));
                entity.Property(e => e.StrategyDefinitionId)
                    .HasColumnType("char(36)")
                    .HasConversion(
                        v => v.ToString(),
                        v => Guid.Parse(v));
                entity.Property(e => e.PositionSizing)
                    .HasColumnType("json")
                    .HasConversion(
                        v => v ?? string.Empty,
                        v => string.IsNullOrEmpty(v) ? null : v)
                    .IsRequired(false);
                entity.Property(e => e.Parameters)
                    .HasColumnType("json")
                    .HasConversion(
                        v => v ?? string.Empty,
                        v => string.IsNullOrEmpty(v) ? null : v)
                    .IsRequired(false);
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.Property(e => e.UpdatedAt).HasColumnType("datetime(6)");
                entity.Property(e => e.NextRunAt).HasColumnType("datetime(6)");

                entity.HasIndex(e => new { e.UserId, e.Status });
                entity.HasIndex(e => e.NextRunAt);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.StrategyDefinition)
                    .WithMany()
                    .HasForeignKey(e => e.StrategyDefinitionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TradingBotRuntimeSnapshot>(entity =>
            {
                entity.ToTable("TradingBotRuntimeSnapshots");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnType("bigint unsigned")
                    .ValueGeneratedOnAdd();
                entity.Property(e => e.TradingBotId)
                    .HasColumnType("char(36)")
                    .HasConversion(
                        v => v.ToString(),
                        v => Guid.Parse(v));
                entity.Property(e => e.RuntimeState)
                    .HasColumnType("json")
                    .HasConversion(
                        v => v ?? "{}",
                        v => string.IsNullOrEmpty(v) ? "{}" : v)
                    .IsRequired();
                entity.Property(e => e.OpenPositionSummary)
                    .HasColumnType("json")
                    .HasConversion(
                        v => v ?? string.Empty,
                        v => string.IsNullOrEmpty(v) ? null : v)
                    .IsRequired(false);
                entity.Property(e => e.CapturedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.Property(e => e.NextTickAt).HasColumnType("datetime(6)");
                entity.Property(e => e.Version)
                    .IsConcurrencyToken()
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.HasIndex(e => new { e.TradingBotId, e.CapturedAt });

                entity.HasOne(e => e.TradingBot)
                    .WithMany()
                    .HasForeignKey(e => e.TradingBotId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TradingBotOrder>(entity =>
            {
                entity.ToTable("TradingBotOrders");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnType("bigint unsigned")
                    .ValueGeneratedOnAdd();
                entity.Property(e => e.TradingBotId)
                    .HasColumnType("char(36)")
                    .HasConversion(
                        v => v.ToString(),
                        v => Guid.Parse(v));
                entity.Property(e => e.OrderId).HasColumnType("bigint unsigned");
                entity.Property(e => e.Intent).HasMaxLength(50);
                entity.Property(e => e.SignalId).HasMaxLength(100);
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.HasIndex(e => e.OrderId).IsUnique();
                entity.HasIndex(e => new { e.TradingBotId, e.CreatedAt });

                entity.HasOne(e => e.TradingBot)
                    .WithMany()
                    .HasForeignKey(e => e.TradingBotId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Order)
                    .WithOne()
                    .HasForeignKey<TradingBotOrder>(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TradingBotLog>(entity =>
            {
                entity.ToTable("TradingBotLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnType("bigint unsigned")
                    .ValueGeneratedOnAdd();
                entity.Property(e => e.TradingBotId)
                    .HasColumnType("char(36)")
                    .HasConversion(
                        v => v.ToString(),
                        v => Guid.Parse(v));
                entity.Property(e => e.Payload)
                    .HasColumnType("json")
                    .HasConversion(
                        v => v ?? string.Empty,
                        v => string.IsNullOrEmpty(v) ? null : v)
                    .IsRequired(false);
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.HasIndex(e => new { e.TradingBotId, e.CreatedAt });
                entity.HasIndex(e => e.Level);

                entity.HasOne(e => e.TradingBot)
                    .WithMany()
                    .HasForeignKey(e => e.TradingBotId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TradingBotParameter>(entity =>
            {
                entity.ToTable("TradingBotParameters");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TradingBotId)
                    .HasColumnType("char(36)")
                    .HasConversion(
                        v => v.ToString(),
                        v => Guid.Parse(v));
                entity.Property(e => e.ParameterKey).HasMaxLength(100);
                entity.Property(e => e.ValueType).HasMaxLength(20);
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.HasIndex(e => new { e.TradingBotId, e.ParameterKey });

                entity.HasOne(e => e.TradingBot)
                    .WithMany()
                    .HasForeignKey(e => e.TradingBotId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================
            // AUDIT EVENT CONFIG
            // ==========================
            modelBuilder.Entity<AuditEvent>(entity =>
            {
                entity.ToTable("AuditEvents");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.BotId)
                    .HasColumnType("char(36)")
                    .HasConversion(nullableGuidConverter);
                entity.Property(e => e.BeforeState)
                    .HasColumnType("json")
                    .HasConversion(
                        v => v ?? string.Empty,
                        v => string.IsNullOrEmpty(v) ? null : v)
                    .IsRequired(false);
                entity.Property(e => e.AfterState)
                    .HasColumnType("json")
                    .HasConversion(
                        v => v ?? string.Empty,
                        v => string.IsNullOrEmpty(v) ? null : v)
                    .IsRequired(false);
                entity.Property(e => e.Metadata).HasColumnType("text");
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.HasIndex(e => new { e.UserId, e.CreatedAt });
                entity.HasIndex(e => e.CorrelationId);
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
            });

            // ==========================
            // FEE LEDGER CONFIG
            // ==========================
            modelBuilder.Entity<FeeLedger>(entity =>
            {
                entity.ToTable("FeeLedger");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasIndex(e => new { e.UserId, e.CreatedAt });
                entity.HasIndex(e => e.TradeId);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================
            // TRADING CONFIGURATION CONFIG
            // ==========================
            modelBuilder.Entity<TradingConfiguration>(entity =>
            {
                entity.ToTable("TradingConfigurations");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ConfigKey, e.Environment }).IsUnique();
            });

            // ==========================
            // BOT RISK CONFIGURATION CONFIG
            // ==========================
            modelBuilder.Entity<BotRiskConfiguration>(entity =>
            {
                entity.ToTable("BotRiskConfigurations");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.BotId });
                entity.Property(e => e.BotId)
                    .HasColumnType("char(36)")
                    .HasConversion(nullableGuidConverter);
                entity.Property(e => e.MaxAllowedCapital).HasColumnType("decimal(30,10)");
                entity.Property(e => e.MaxSlippage).HasColumnType("decimal(10,4)");
                entity.Property(e => e.MaxDailyLoss).HasColumnType("decimal(10,4)");
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                    .ValueGeneratedOnAddOrUpdate();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Bot)
                    .WithMany()
                    .HasForeignKey(e => e.BotId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ==========================
            // RECONCILIATION RESULT CONFIG
            // ==========================
            modelBuilder.Entity<ReconciliationResult>(entity =>
            {
                entity.ToTable("ReconciliationResults");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Mismatches)
                    .HasColumnType("json")
                    .HasConversion(
                        v => v ?? string.Empty,
                        v => string.IsNullOrEmpty(v) ? null : v)
                    .IsRequired(false);
                entity.Property(e => e.ErrorMessage).HasColumnType("text");
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.HasIndex(e => new { e.EntityType, e.ReconciliationTime });
            });

            // ==========================
            // CLIENT ORDER IDEMPOTENCY CONFIG
            // ==========================
            modelBuilder.Entity<ClientOrderIdempotency>(entity =>
            {
                entity.ToTable("ClientOrderIdempotency");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ClientOrderId).IsUnique();
                entity.HasIndex(e => new { e.UserId, e.CreatedAt });

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================
            // BOT RISK STATE CONFIG
            // ==========================
            modelBuilder.Entity<BotRiskState>(entity =>
            {
                entity.ToTable("BotRiskStates");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.BotId);
                entity.Property(e => e.BotId)
                    .IsRequired()
                    .HasColumnType("char(36)")
                    .HasConversion(guidConverter);
                entity.Property(e => e.DailyLoss).HasColumnType("decimal(18,8)");
                entity.Property(e => e.TotalDrawdown).HasColumnType("decimal(18,8)");
                entity.Property(e => e.DailyLossResetAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.Property(e => e.LastOrderAt)
                    .HasColumnType("datetime(6)");
                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                    .ValueGeneratedOnAddOrUpdate();

                entity.HasOne(e => e.Bot)
                    .WithMany()
                    .HasForeignKey(e => e.BotId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================
            // KILL SWITCH EVENT CONFIG
            // ==========================
            modelBuilder.Entity<KillSwitchEvent>(entity =>
            {
                entity.ToTable("KillSwitchEvents");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.BotId);
                entity.HasIndex(e => e.TriggerTime);
                entity.Property(e => e.BotId)
                    .IsRequired()
                    .HasColumnType("char(36)")
                    .HasConversion(guidConverter);
                entity.Property(e => e.TriggerTime)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.Property(e => e.TotalLoss).HasColumnType("decimal(18,8)");

                entity.HasOne(e => e.Bot)
                    .WithMany()
                    .HasForeignKey(e => e.BotId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================
            // USER CAPITAL LIMITS CONFIG
            // ==========================
            modelBuilder.Entity<UserCapitalLimits>(entity =>
            {
                entity.ToTable("UserCapitalLimits");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();

                entity.Property(e => e.MaxTotalExposure)
                    .HasColumnType("decimal(18,8)");
                entity.Property(e => e.MaxCapitalPerBot)
                    .HasColumnType("decimal(18,8)");
                entity.Property(e => e.MaxDailyLoss)
                    .HasColumnType("decimal(18,8)");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6)");
            });
        }
    }
}
