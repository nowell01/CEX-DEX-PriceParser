using CEX_DEX_Parser.Data;
using CEX_DEX_Parser.Models;
using Microsoft.EntityFrameworkCore;
using static CEX_DEX_Parser.Services.Storage.StorageInterfaces;

namespace CEX_DEX_Parser.Services.Storage
{
    public class DbPriceHistoryStore : IPriceHistoryStore
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public DbPriceHistoryStore(IDbContextFactory<AppDbContext> factory) => _factory = factory;

        public Task AddAsync(PriceComparison comparison) => AddManyAsync(new[] { comparison });

        public async Task AddManyAsync(IEnumerable<PriceComparison> comparisons)
        {
            var now = DateTime.UtcNow;
            var snapshots = comparisons
                .Where(c => c.Prices.Count > 0)
                .Select(c => PriceMapper.ToSnapshot(c, now))
                .ToList();

            if (snapshots.Count == 0) return;

            await using var db = await _factory.CreateDbContextAsync();
            db.PriceSnapshots.AddRange(snapshots);
            await db.SaveChangesAsync();
        }

        public async Task<List<PriceComparison>> GetHistoryAsync(string symbol, DateTime fromUtc, DateTime toUtc)
        {
            fromUtc = PriceMapper.Utc(fromUtc);
            toUtc = PriceMapper.Utc(toUtc);

            await using var db = await _factory.CreateDbContextAsync();
            return await db.PriceSnapshots
                .AsNoTracking()
                .Where(s => s.Symbol == symbol && s.CapturedAt >= fromUtc && s.CapturedAt < toUtc)
                .OrderBy(s => s.CapturedAt)
                .Select(s => new PriceComparison
                {
                    Symbol = s.Symbol,
                    Spread = s.Spread,
                    SpreadPercent = s.SpreadPercent,
                    HighestExchange = s.HighestExchange,
                    LowestExchange = s.LowestExchange,
                    Prices = s.Prices.Select(p => new ExchangePrice
                    {
                        Exchange = p.Exchange,
                        Symbol = p.Symbol,
                        Price = p.Price,
                        Volume24h = p.Volume24h,
                        Timestamp = p.Timestamp
                    }).ToList()
                })
                .ToListAsync();
        }
    }
}