using CEX_DEX_Parser.Data.Entities;
using CEX_DEX_Parser.Models;
using Microsoft.EntityFrameworkCore;

namespace CEX_DEX_Parser.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AlertLog> AlertLogs => Set<AlertLog>();
        public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
        public DbSet<ExchangePriceRow> ExchangePrices => Set<ExchangePriceRow>();
        public DbSet<TelegramSubscriber> TelegramSubscribers => Set<TelegramSubscriber>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            // alert_logs
            b.Entity<AlertLog>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasMaxLength(36);
                e.Property(x => x.Symbol).HasMaxLength(20).IsRequired();
                e.Property(x => x.HighExchange).HasMaxLength(32).IsRequired();
                e.Property(x => x.LowExchange).HasMaxLength(32).IsRequired();
                // decimals: no HasPrecision -> unbounded numeric (exact for PEPE/SHIB and BTC)
                e.HasIndex(x => x.TriggeredAt);
                e.HasIndex(x => new { x.Symbol, x.TriggeredAt });
            });

            // price_snapshots
            b.Entity<PriceSnapshot>(e =>
            {
                e.Property(x => x.Symbol).HasMaxLength(20).IsRequired();
                e.Property(x => x.HighestExchange).HasMaxLength(32).IsRequired();
                e.Property(x => x.LowestExchange).HasMaxLength(32).IsRequired();
                e.HasIndex(x => new { x.Symbol, x.CapturedAt });

                e.HasMany(x => x.Prices)
                 .WithOne(p => p.Snapshot)
                 .HasForeignKey(p => p.SnapshotId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // exchange_prices
            b.Entity<ExchangePriceRow>(e =>
            {
                e.ToTable("exchange_prices");
                e.Property(x => x.Exchange).HasMaxLength(32).IsRequired();
                e.Property(x => x.Symbol).HasMaxLength(20).IsRequired();
                e.HasIndex(x => new { x.Symbol, x.Exchange, x.Timestamp });
            });

            // telegram_subscribers
            b.Entity<TelegramSubscriber>(e =>
            {
                e.HasKey(x => x.ChatId);
                e.Property(x => x.ChatId).HasMaxLength(32);
            });
        }
    }
}