//using Telegram.Bot;
//using Telegram.Bot.Types.Enums;

//public interface ITelegramNotifier
//{
//    Task SendAsync(string message, string key = "", CancellationToken ct = default);
//}

//public class TelegramNotifier : ITelegramNotifier
//{
//    private readonly ITelegramBotClient _bot;
//    private readonly string _chatId;
//    private readonly ILogger<TelegramNotifier> _logger;
//    private readonly Dictionary<string, DateTime> _lastSent = new();
//    private readonly TimeSpan _cooldown = TimeSpan.FromMinutes(5);

//    public TelegramNotifier(IConfiguration config, ILogger<TelegramNotifier> logger)
//    {
//        _logger = logger;
//        _chatId = config["Telegram:ChatId"]!;
//        _bot = new TelegramBotClient(config["Telegram:BotToken"]!);
//    }

//    public async Task SendAsync(string message, string key = "", CancellationToken ct = default)
//    {
//        try
//        {
//            if (!string.IsNullOrEmpty(key))
//            {
//                if (_lastSent.TryGetValue(key, out var last) && DateTime.UtcNow - last < _cooldown)
//                {
//                    _logger.LogInformation("Telegram alert suppressed by cooldown for key: {Key}", key);
//                    return;
//                }

//                _lastSent[key] = DateTime.UtcNow;
//            }

//            await _bot.SendMessage(
//                chatId: _chatId,
//                text: message,
//                parseMode: ParseMode.Html,
//                cancellationToken: ct);
//        }
//        catch (Exception ex)
//        {
//            // Telegram failure must never crash your 30s monitor loop
//            _logger.LogWarning(ex, "Telegram alert failed to send");
//        }
//    }
//}