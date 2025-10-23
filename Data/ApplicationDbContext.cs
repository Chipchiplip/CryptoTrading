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

        public DbSet<User> Users { get; set; }
        public DbSet<Watchlist> Watchlists { get; set; }
        public DbSet<WatchlistItem> WatchlistItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                // Let application handle CreatedAt default value for better compatibility
            });

            modelBuilder.Entity<Watchlist>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UserId).IsRequired();
                entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique();
                entity.HasIndex(e => new { e.UserId, e.IsDefault });
                
                // One user can have multiple watchlists
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WatchlistItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CoinSymbol).IsRequired().HasMaxLength(10);
                entity.HasIndex(e => new { e.WatchlistId, e.CoinSymbol }).IsUnique();
                
                // One watchlist can have multiple items
                entity.HasOne(e => e.Watchlist)
                    .WithMany(w => w.Items)
                    .HasForeignKey(e => e.WatchlistId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}