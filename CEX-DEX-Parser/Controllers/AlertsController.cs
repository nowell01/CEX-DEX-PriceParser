using CEX_DEX_Parser.Models;
using CEX_DEX_Parser.Services;
using Microsoft.AspNetCore.Mvc;

namespace CEX_DEX_Parser.Controllers
{
    [ApiController]
    [Route("api/alerts")]
    public class AlertsController : ControllerBase
    {
        private readonly AlertService _alertService;

        public AlertsController(AlertService alertService)
        {
            _alertService = alertService;
        }

        [HttpGet("config")]
        public async Task<ActionResult<List<AlertConfig>>> GetConfigs()
        {
            var configs = await _alertService.GetConfigsAsync();
            return Ok(configs);
        }

        [HttpPost("config")]
        public async Task<IActionResult> SaveConfig([FromBody] AlertConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.Symbol))
                return BadRequest("Symbol is required.");

            if (config.ThresholdPercent <= 0)
                return BadRequest("ThresholdPercent must be greater than 0.");

            config.Symbol = config.Symbol.ToUpper().Trim();
            await _alertService.SaveConfigAsync(config);
            return Ok(config);
        }

        [HttpDelete("config/{symbol}")]
        public async Task<IActionResult> DeleteConfig(string symbol)
        {
            var decoded = Uri.UnescapeDataString(symbol).ToUpper();
            await _alertService.DeleteConfigAsync(decoded);
            return NoContent();
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<AlertLog>>> GetHistory()
        {
            var history = await _alertService.GetHistoryAsync();
            return Ok(history.OrderByDescending(a => a.TriggeredAt));
        }
    }
}
