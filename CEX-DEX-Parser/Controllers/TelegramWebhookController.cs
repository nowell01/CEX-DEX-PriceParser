using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using CEX_DEX_Parser.Services;

namespace CEX_DEX_Parser.Controllers
{
    [ApiController]
    [Route("api/telegram")]
    public class TelegramWebhookController : ControllerBase
    {
        private readonly ChatIdStore _store;
        private readonly ILogger<TelegramWebhookController> _logger;

        public TelegramWebhookController(ChatIdStore store, ILogger<TelegramWebhookController> logger)
        {
            _store = store;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public IActionResult Update([FromBody] Update update)
        {
            if (update.Type == UpdateType.Message &&
                update.Message?.Text?.Trim() == "/start")
            {
                var chatId = update.Message.Chat.Id.ToString();
                _store.Add(chatId);
                _logger.LogInformation("New subscriber: {ChatId}", chatId);
            }

            return Ok();
        }
    }
}
