using Microsoft.EntityFrameworkCore;
using CryptoTrading.Models;

namespace CryptoTrading.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Cryptocurrency> Cryptocurrencies { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<UserWatchlist> UserWatchlists { get; set; }
    public DbSet<WatchlistItem> WatchlistItems { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<Balance> Balances { get; set; }
    public DbSet<Bot> Bots { get; set; }
    public DbSet<CryptoPrice> CryptoPrices { get; set; }
    public DbSet<Trade> Trades { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<PaymentHistory> PaymentHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.RowVersion).IsRowVersion();
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany(e => e.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Cryptocurrency
        modelBuilder.Entity<Cryptocurrency>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Symbol).IsUnique();
            entity.HasIndex(e => e.CoinGeckoId).IsUnique();
            entity.Property(e => e.RowVersion).IsRowVersion();
        });

        // Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Orders)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Cryptocurrency)
                  .WithMany(e => e.Orders)
                  .HasForeignKey(e => e.CryptocurrencyId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.RowVersion).IsRowVersion();
        });

        // UserWatchlist
        modelBuilder.Entity<UserWatchlist>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Watchlists)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.RowVersion).IsRowVersion();
        });

        // WatchlistItem
        modelBuilder.Entity<WatchlistItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Watchlist)
                  .WithMany(e => e.Items)
                  .HasForeignKey(e => e.WatchlistId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Cryptocurrency)
                  .WithMany(e => e.WatchlistItems)
                  .HasForeignKey(e => e.CryptocurrencyId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.WatchlistId, e.CryptocurrencyId }).IsUnique();
            entity.Property(e => e.RowVersion).IsRowVersion();
        });

        // Position
        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Positions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Cryptocurrency)
                  .WithMany(e => e.Positions)
                  .HasForeignKey(e => e.CryptocurrencyId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.UserId, e.CryptocurrencyId }).IsUnique();
            entity.Property(e => e.RowVersion).IsRowVersion();
        });

        // Balance
        modelBuilder.Entity<Balance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Balances)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Cryptocurrency)
                  .WithMany(e => e.Balances)
                  .HasForeignKey(e => e.CryptocurrencyId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.UserId, e.CryptocurrencyId }).IsUnique();
            entity.Property(e => e.RowVersion).IsRowVersion();
        });

        // Bot
        modelBuilder.Entity<Bot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Bots)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.RowVersion).IsRowVersion();
        });

        // CryptoPrice
        modelBuilder.Entity<CryptoPrice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Cryptocurrency)
                  .WithMany(e => e.Prices)
                  .HasForeignKey(e => e.CryptocurrencyId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.CryptocurrencyId, e.Timestamp });
        });

        // Trade
        modelBuilder.Entity<Trade>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Trades)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Order)
                  .WithMany(e => e.Trades)
                  .HasForeignKey(e => e.OrderId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Cryptocurrency)
                  .WithMany(e => e.Trades)
                  .HasForeignKey(e => e.CryptocurrencyId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Subscription
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Subscriptions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.RowVersion).IsRowVersion();
        });

        // PaymentHistory
        modelBuilder.Entity<PaymentHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany(e => e.PaymentHistories)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.RowVersion).IsRowVersion();
        });
    }
}
