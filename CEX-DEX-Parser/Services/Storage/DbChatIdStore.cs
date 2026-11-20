using CEX_DEX_Parser.Data;
using Microsoft.EntityFrameworkCore;
using static CEX_DEX_Parser.Services.Storage.StorageInterfaces;

namespace CEX_DEX_Parser.Services.Storage
{
    public class DbChatIdStore : IChatIdStore
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public DbChatIdStore(IDbContextFactory<AppDbContext> factory) => _factory = factory;

        // Returns true if this chat was newly subscribed, false if it already existed
        public async Task<bool> AddAsync(string chatId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(chatId)) return false;

            await using var db = await _factory.CreateDbContextAsync(ct);
            // Atomic insert: a repeated /start is ignored, no read-then-write race.
            // ExecuteSql returns rows affected: 1 = inserted, 0 = ON CONFLICT skipped it.
            var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO telegram_subscribers (chat_id, subscribed_at)
                VALUES ({chatId}, {DateTime.UtcNow})
                ON CONFLICT (chat_id) DO NOTHING
                """, ct);

            return inserted == 1;
        }

        public async Task<List<string>> GetAllAsync()
        {
            await using var db = await _factory.CreateDbContextAsync();
            return await db.TelegramSubscribers
                .AsNoTracking()
                .Select(s => s.ChatId)
                .ToListAsync();
        }
    }
}