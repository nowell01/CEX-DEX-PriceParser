using CEX_DEX_Parser.Services;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static CEX_DEX_Parser.Services.Storage.StorageInterfaces;

namespace CEX_DEX_Parser.Controllers
{
    [ApiController]
    [Route("api/telegram")]
    public class TelegramWebhookController : ControllerBase
    {
        private const string SecretHeader = "X-Telegram-Bot-Api-Secret-Token";

        private readonly IChatIdStore _store;
        private readonly ITelegramNotifier _telegram;
        private readonly ILogger<TelegramWebhookController> _logger;
        private readonly string? _webhookSecret;

        public TelegramWebhookController(
            IChatIdStore store,
            ITelegramNotifier telegram,
            ILogger<TelegramWebhookController> logger,
            IConfiguration config)
        {
            _store = store;
            _telegram = telegram;
            _logger = logger;
            _webhookSecret = config["Telegram:WebhookSecret"];
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleUpdate([FromBody] Update update, CancellationToken ct)
        {
            // Only Telegram knows the secret set via SetWebhook(secretToken: ...)
            if (string.IsNullOrEmpty(_webhookSecret) ||
                !Request.Headers.TryGetValue(SecretHeader, out var secret) ||
                secret != _webhookSecret)
            {
                return Unauthorized();
            }

            var text = update.Message?.Text?.Trim();
            if (update.Type != UpdateType.Message || string.IsNullOrEmpty(text))
                return Ok();

            // Matches "/start", "/start payload" (deep link), "/start@YourBot" (groups)
            var command = text.Split(' ', 2)[0].Split('@')[0];
            if (command != "/start")
                return Ok();

            var chatId = update.Message!.Chat.Id.ToString();

            try
            {
                var isNew = await _store.AddAsync(chatId, ct);

                var message = isNew
                    ? "Welcome to the <b>CryptoScan Alert Bot</b>!\n\n" +
                      "You have successfully subscribed to alerts.\n" +
                      "Once the arbitrage detector notices a spread opportunity, you'll receive an alert!"
                    : "You're already subscribed to CryptoScan alerts.";

                await _telegram.SendWelcomeMessageAsync(chatId, message, ct);

                if (isNew)
                    _logger.LogInformation("New subscriber: {ChatId}", chatId);
            }
            catch (Exception ex)
            {
                // Still return 200 so Telegram doesn't retry the same update forever
                _logger.LogError(ex, "Failed to process /start for {ChatId}", chatId);
            }

            return Ok();
        }
    }
}