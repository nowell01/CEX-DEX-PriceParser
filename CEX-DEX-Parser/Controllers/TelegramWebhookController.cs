using CEX_DEX_Parser.Services.Storage;
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
        private readonly IChatIdStore _store;
        private readonly ILogger<TelegramWebhookController> _logger;

        public TelegramWebhookController(IChatIdStore store, ILogger<TelegramWebhookController> logger)
        {
            _store = store;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Update([FromBody] Update update)
        {
            if (update.Type == UpdateType.Message &&
                update.Message?.Text?.Trim() == "/start")
            {
                var chatId = update.Message.Chat.Id.ToString();

                try
                {
                    await _store.AddAsync(chatId);
                    _logger.LogInformation("New subscriber: {ChatId}", chatId);
                }
                catch (Exception ex)
                {
                    // Still return 200 so Telegram doesn't retry the same update forever
                    _logger.LogError(ex, "Failed to save subscriber {ChatId}", chatId);
                }
            }

            return Ok();
        }
    }
}