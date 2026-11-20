using System.Text.Json;
using CEX_DEX_Parser.Data.Entities;
using CEX_DEX_Parser.Models;
using CEX_DEX_Parser.Services.Storage;
using Microsoft.EntityFrameworkCore;

namespace CEX_DEX_Parser.Data
{
    /// <summary>
    /// One-time import of the old JSON files into Postgres.
    /// Safe to run twice: each table is skipped if it already has rows.
    /// </summary>
    public static class JsonImporter
    {
        private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

        public static async Task RunAsync(AppDbContext db, string dir, ILogger log)
        {
            log.LogInformation("JSON import from {Dir}", dir);

            // 1. Alerts
            if (await db.AlertLogs.AnyAsync())
            {
                log.LogInformation("alert_logs not empty, skipping");
            }
            else
            {
                var alerts = (await ReadListAsync<AlertLog>(dir, "alerts-log.json"))
                    .DistinctBy(a => a.Id)
                    .ToList();
                foreach (var a in alerts) a.TriggeredAt = PriceMapper.Utc(a.TriggeredAt);

                db.AlertLogs.AddRange(alerts);
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
                log.LogInformation("Imported {Count} alerts", alerts.Count);
            }

            // 2. Price history (batched to keep memory and change tracker small)
            if (await db.PriceSnapshots.AnyAsync())
            {
                log.LogInformation("price_snapshots not empty, skipping");
            }
            else
            {
                var history = (await ReadListAsync<PriceComparison>(dir, "price-history.json"))
                    .Where(h => h.Prices.Count > 0)
                    .ToList();

                db.ChangeTracker.AutoDetectChangesEnabled = false;
                var done = 0;
                foreach (var batch in history.Chunk(500))
                {
                    db.PriceSnapshots.AddRange(batch.Select(c => PriceMapper.ToSnapshot(c)));
                    await db.SaveChangesAsync();
                    db.ChangeTracker.Clear();
                    done += batch.Length;
                    log.LogInformation("Price history {Done}/{Total}", done, history.Count);
                }
                db.ChangeTracker.AutoDetectChangesEnabled = true;
            }

            // 3. Telegram subscribers (old store = JSON array of strings; "{}" = empty)
            if (await db.TelegramSubscribers.AnyAsync())
            {
                log.LogInformation("telegram_subscribers not empty, skipping");
            }
            else
            {
                var ids = ReadChatIds(Path.Combine(dir, "telegram-subscribers.json"));
                db.TelegramSubscribers.AddRange(ids.Select(id => new TelegramSubscriber { ChatId = id }));
                await db.SaveChangesAsync();
                log.LogInformation("Imported {Count} subscribers", ids.Count);
            }

            log.LogInformation("JSON import finished");
        }

        private static async Task<List<T>> ReadListAsync<T>(string dir, string file)
        {
            var path = Path.Combine(dir, file);
            if (!File.Exists(path)) return new List<T>();

            await using var s = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<List<T>>(s, Opts) ?? new List<T>();
        }

        private static HashSet<string> ReadChatIds(string path)
        {
            var ids = new HashSet<string>();
            if (!File.Exists(path)) return ids;

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return ids;

            foreach (var e in doc.RootElement.EnumerateArray())
            {
                var id = e.ValueKind == JsonValueKind.String ? e.GetString() : e.GetRawText();
                if (!string.IsNullOrWhiteSpace(id)) ids.Add(id);
            }
            return ids;
        }
    }
}