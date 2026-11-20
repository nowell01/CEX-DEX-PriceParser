using CEX_DEX_Parser.Data;
using Microsoft.EntityFrameworkCore;
using static CEX_DEX_Parser.Services.Storage.StorageInterfaces;

namespace CEX_DEX_Parser.Services.Storage
{
    public class DbChatIdStore : IChatIdStore
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public DbChatIdStore(IDbContextFactory<AppDbContext> factory) => _factory = factory;

        public async Task AddAsync(string chatId)
        {
            if (string.IsNullOrWhiteSpace(chatId)) return;

            await using var db = await _factory.CreateDbContextAsync();
            // Atomic insert: a repeated /start is ignored, no read-then-write race
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO telegram_subscribers (chat_id, subscribed_at)
                VALUES ({chatId}, {DateTime.UtcNow})
                ON CONFLICT (chat_id) DO NOTHING
                """);
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