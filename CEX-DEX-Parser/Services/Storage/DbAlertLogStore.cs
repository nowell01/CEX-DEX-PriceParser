using CEX_DEX_Parser.Data;
using CEX_DEX_Parser.Models;
using Microsoft.EntityFrameworkCore;
using static CEX_DEX_Parser.Services.Storage.StorageInterfaces;

namespace CEX_DEX_Parser.Services.Storage
{
    public class DbAlertLogStore : IAlertLogStore
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public DbAlertLogStore(IDbContextFactory<AppDbContext> factory) => _factory = factory;

        public async Task AddAsync(AlertLog alert)
        {
            alert.TriggeredAt = PriceMapper.Utc(alert.TriggeredAt);

            await using var db = await _factory.CreateDbContextAsync();
            db.AlertLogs.Add(alert);
            await db.SaveChangesAsync();
        }

        public async Task<List<AlertLog>> GetRecentAsync(int take = 100)
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.AlertLogs
                .AsNoTracking()
                .OrderByDescending(a => a.TriggeredAt)
                .Take(take)
                .ToListAsync();
        }
        public async Task<List<AlertLog>> GetSinceAsync(DateTime fromUtc)
        {
            fromUtc = PriceMapper.Utc(fromUtc);

            await using var db = await _factory.CreateDbContextAsync();
            return await db.AlertLogs
                .AsNoTracking()
                .Where(a => a.TriggeredAt >= fromUtc)
                .OrderBy(a => a.TriggeredAt)
                .ToListAsync();
        }
    }
}