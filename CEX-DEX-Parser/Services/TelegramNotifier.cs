using CEX_DEX_Parser.Services.Storage;
using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using static CEX_DEX_Parser.Services.Storage.StorageInterfaces;

namespace CEX_DEX_Parser.Services
{
    public interface ITelegramNotifier
    {
        Task SendAsync(string message, string key = "", CancellationToken ct = default);
    }

    public class TelegramNotifier : ITelegramNotifier
    {
        private readonly ITelegramBotClient _bot;
        private readonly IChatIdStore _store;
        private readonly ILogger<TelegramNotifier> _logger;
        private readonly ConcurrentDictionary<string, DateTime> _lastSent = new();
        private readonly TimeSpan _cooldown = TimeSpan.FromMinutes(4);

        public TelegramNotifier(IConfiguration config, IChatIdStore store, ILogger<TelegramNotifier> logger)
        {
            _logger = logger;
            _store = store;
            _bot = new TelegramBotClient(config["Telegram:BotToken"]
                ?? throw new InvalidOperationException("Telegram:BotToken is not configured."));
        }

        public async Task SendAsync(string message, string key = "", CancellationToken ct = default)
        {
            try
            {
                if (!string.IsNullOrEmpty(key))
                {
                    var now = DateTime.UtcNow;
                    if (_lastSent.TryGetValue(key, out var last) && now - last < _cooldown)
                    {
                        _logger.LogInformation("Telegram alert suppressed by cooldown for key: {Key}", key);
                        return;
                    }
                    _lastSent[key] = now;
                }

                // Broadcast to all subscribers who sent /start
                var chatIds = await _store.GetAllAsync();

                if (chatIds.Count == 0)
                {
                    _logger.LogWarning("No Telegram subscribers yet - no messages sent.");
                    return;
                }

                foreach (var chatId in chatIds)
                {
                    try
                    {
                        await _bot.SendMessage(
                            chatId: chatId,
                            text: message,
                            parseMode: ParseMode.Html,
                            cancellationToken: ct);
                    }
                    catch (Exception ex)
                    {
                        // One bad chat ID shouldn't stop others from receiving
                        _logger.LogWarning(ex, "Failed to send to chat {ChatId}", chatId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telegram broadcast failed");
            }
        }
    }
}