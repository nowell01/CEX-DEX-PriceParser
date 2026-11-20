using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace CEX_DEX_Parser.Services
{
    public class TelegramWebhookRegistrar : IHostedService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TelegramWebhookRegistrar> _logger;

        public TelegramWebhookRegistrar(IConfiguration config, ILogger<TelegramWebhookRegistrar> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            var url = _config["Telegram:WebhookUrl"];
            var secret = _config["Telegram:WebhookSecret"];
            var token = _config["Telegram:BotToken"];

            // Only production has a WebhookUrl, so local runs never hijack the live webhook
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("Telegram webhook not configured - skipping registration.");
                return;
            }

            try
            {
                var bot = new TelegramBotClient(token);
                await bot.SetWebhook(
                    url: url,
                    secretToken: secret,
                    allowedUpdates: new[] { UpdateType.Message },
                    cancellationToken: ct);

                _logger.LogInformation("Telegram webhook registered: {Url}", url);
            }
            catch (Exception ex)
            {
                // Don't crash the app over this; the old webhook stays as-is
                _logger.LogError(ex, "Failed to register Telegram webhook");
            }
        }

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}